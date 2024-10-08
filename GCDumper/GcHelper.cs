﻿using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

// https://github.com/cklutz/EtwForceGC
namespace GCDumper;

public static class GcHelper
{
    public static void ForceGc(string prefix, int processId, TextWriter tw, int completeTimeout = -1)
    {
        tw.WriteLine("{0}: proc {1}: starting", prefix, processId);

        const int startTimeout = 15000;
        completeTimeout = completeTimeout <= 0 ? 60000 : completeTimeout;
        var interrupted = false;

        using var startedEvent = new ManualResetEventSlim();
        using var session = new TraceEventSession("TriggerGC-" + processId);
        session.Source.NeedLoadedDotNetRuntimes();
        session.Source.AddCallbackOnProcessStart((TraceProcess proc) =>
        {
            proc.AddCallbackOnDotNetRuntimeLoad((TraceLoadedDotNetRuntime runtime) =>
            {
                runtime.GCStart += (TraceProcess p, TraceGC gc) =>
                {
                    if (p.ProcessID == processId && gc.Reason.ToString().Contains("Induced"))
                    {
                        var xprefix = $"{prefix}: proc {p.ProcessID}: ";
                        tw.WriteLine("{0} GC #{1} {2} start at {3:N2}ms",
                            xprefix,
                            gc.Number,
                            gc.Reason,
                            gc.PauseStartRelativeMSec);
                    }
                };
                runtime.GCEnd += (TraceProcess p, TraceGC gc) =>
                {
                    if (p.ProcessID == processId && gc.Reason.ToString().Contains("Induced"))
                    {
                        var xprefix = $"{prefix}: proc {p.ProcessID}: ";
                        tw.WriteLine("{0} GC #{1} {2} end, paused for {3:N2}ms",
                            xprefix,
                            gc.Number,
                            gc.Reason,
                            gc.PauseDurationMSec);

                        WriteFormattedTraceGC(tw, xprefix, gc);
                        WriteFormattedHeapStats(tw, xprefix, gc.HeapStats);

                        session.Source.StopProcessing();
                    }
                };
            });
        });

        var options = new TraceEventProviderOptions
        {
            ProcessIDFilter = new List<int> { processId }
        };

        session.EnableProvider(
            ClrTraceEventParser.ProviderGuid,
            TraceEventLevel.Informational,
            (ulong)(ClrTraceEventParser.Keywords.GC | ClrTraceEventParser.Keywords.GCHeapCollect),
            options);

        // Sample: direct invocation of GCHeapCollect with a specific Process ID.
        // Not used here, since we also need GC-events to listen to.
        //session.CaptureState(ClrTraceEventParser.ProviderGuid,
        //  (long)(ClrTraceEventParser.Keywords.GCHeapCollect),
        //  filterType: unchecked((int)0x80000004),
        //  data: processId);

        var eventReader = Task.Run(() =>
        {
            if (!Console.IsInputRedirected)
            {
                // Ensure we are cleanly detaching from victim process; otherwise it will be left hanging dormant.
                Console.TreatControlCAsInput = false;
                Console.CancelKeyPress += (sender, args) =>
                {
                    args.Cancel = true;
                    // session.Stop(), not source.StopProcessing(), because the former is immediate, while the later is not.
                    interrupted = true;
                    session.Stop();
                };
            }

            startedEvent.Set();
            session.Source.Process();
        });

        if (!startedEvent.Wait(startTimeout))
        {
            tw.WriteLine("{0}: proc {1}: event reader did not start within {2:N2} seconds. Giving up.",
                prefix, processId, startTimeout / 1000);
            session.Stop();
            return;
        }

        if (!eventReader.Wait(completeTimeout))
        {
            tw.WriteLine("{0}: proc {1}: garbage collection did not finish within {2:N2} seconds. Giving up.",
                prefix, processId, completeTimeout / 1000);
            session.Stop();
        }
        else if (interrupted)
        {
            tw.WriteLine("{0}: proc {1}: interrupted.", prefix, processId);
        }
        else
        {
            tw.WriteLine("{0}: proc {1}: complete.", prefix, processId);
        }
    }

    private static void WriteFormattedTraceGC(TextWriter tw, string prefix, TraceGC gc)
    {
        // Fields are not really obsolete, but experimental.
#pragma warning disable CS0618
        double mbBefore = gc.HeapSizeBeforeMB;
        double mbAfter = gc.HeapSizeAfterMB;
#pragma warning restore CS0618

        tw.WriteLine("{0}    heap size before {1:N2} MB, after {2:N2} MB, {3:N2} % freed.",
            prefix, mbBefore, mbAfter,
            (100 - (((double)mbAfter / mbBefore) * 100)));
    }

    private static void WriteFormattedHeapStats(TextWriter tw, string prefix, GCHeapStats heapStats)
    {
        var data = new List<Tuple<string, string>>();
        data.Add(FormatWithValue(heapStats, h => h.Depth));
        data.Add(FormatWithValue(heapStats, h => h.FinalizationPromotedCount));
        data.Add(FormatWithValue(heapStats, h => h.FinalizationPromotedSize));
        data.Add(FormatWithValue(heapStats, h => h.GCHandleCount));
        data.Add(FormatWithValue(heapStats, h => h.GenerationSize0));
        data.Add(FormatWithValue(heapStats, h => h.GenerationSize1));
        data.Add(FormatWithValue(heapStats, h => h.GenerationSize2));
        data.Add(FormatWithValue(heapStats, h => h.GenerationSize3));
        data.Add(FormatWithValue(heapStats, h => h.PinnedObjectCount));
        data.Add(FormatWithValue(heapStats, h => h.SinkBlockCount));
        data.Add(FormatWithValue(heapStats, h => h.TotalHeapSize));
        data.Add(FormatWithValue(heapStats, h => h.TotalPromoted));
        data.Add(FormatWithValue(heapStats, h => h.TotalPromotedSize0));
        data.Add(FormatWithValue(heapStats, h => h.TotalPromotedSize1));
        data.Add(FormatWithValue(heapStats, h => h.TotalPromotedSize2));
        data.Add(FormatWithValue(heapStats, h => h.TotalPromotedSize3));

        int maxName = data.Max(d => d.Item1.Length);
        int maxValue = data.Max(d => d.Item2.Length);

        foreach (var entry in data)
        {
            tw.WriteLine("{0}    {1}   {2}", prefix, entry.Item1.PadRight(maxName), entry.Item2.PadLeft(maxValue));
        }
    }

    private static Tuple<string, string> FormatWithValue<T>(GCHeapStats s, Expression<Func<GCHeapStats, T>> exp)
    {
        if (!(exp.Body is MemberExpression body))
        {
            var ubody = (UnaryExpression)exp.Body;
            body = ubody.Operand as MemberExpression;
        }

        string name = body.Member.Name;
        name = Regex.Replace(name, @"(\B[A-Z0-9])", " $1").ToLowerInvariant().Replace("g c ", "GC ");

        return Tuple.Create(name, $"{exp.Compile().Invoke(s):N0}");
    }
}