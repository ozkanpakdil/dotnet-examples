using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using GCDumper;
using Microsoft.Diagnostics.Runtime;

class Program
{
    [DllImport("Dbghelp.dll")]
    static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType,
        IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    const int MiniDumpWithFullMemory = 2;

    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine(
                "Usage: DumpTool.exe <ProcessName> [dumpbeforegc:true/false] [interval1:seconds] [interval2:seconds]");
            Console.WriteLine("  ProcessName: Name of the process to dump (without .exe)");
            Console.WriteLine("  dumpBeforeGc: 'true' to dump before GC, 'false' to skip (default: true)");
            Console.WriteLine("  memDump: 'true' to generate DMP files, 'false' to skip (default: false)");
            Console.WriteLine("  interval1: Seconds to wait before second dump (default: 10)");
            Console.WriteLine("  interval2: Seconds to wait before third dump (default: 20)");
            Console.WriteLine("  interval2: Seconds to wait before third dump (default: 20)");
            Console.WriteLine("Example: gcdumper run RedGate.Monitor.BaseMonitor interval2:30 interval1:15");
            return;
        }

        var processName = args[0];
        var dumpBeforeGc = true;
        var interval1 = 10;
        var interval2 = 20;
        var memDump = false;

        foreach (var arg in args.Skip(1))
        {
            var parts = arg.Split(':');
            if (parts.Length == 2)
            {
                switch (parts[0].ToLower())
                {
                    case "dumpbeforegc":
                        dumpBeforeGc = bool.Parse(parts[1]);
                        break;
                    case "interval1":
                        interval1 = int.Parse(parts[1]);
                        break;
                    case "interval2":
                        interval2 = int.Parse(parts[1]);
                        break;
                    case "memdump":
                        memDump = bool.Parse(parts[1]);
                        break;
                }
            }
        }

        Process process;
        try
        {
            process = GetProcess(processName);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine($"Found process '{processName}' with ID: {process.Id}");

        if (memDump)
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ??
                                      throw new InvalidOperationException());
            var availableSpace = drive.AvailableFreeSpace;

            var processMemory = process.WorkingSet64;
            var estimatedDumpSize = processMemory + (processMemory * 0.1); // add 10% for overhead

            if (estimatedDumpSize > availableSpace)
            {
                Console.WriteLine("Warning: Available disk space is insufficient to create memory dump.");
                Console.WriteLine($"Available space: {availableSpace / (1024 * 1024)} MB");
                Console.WriteLine($"Estimated dump size: {estimatedDumpSize / (1024 * 1024)} MB");
                Console.WriteLine("Disabling memory dump creation.");
                memDump = false; // set memDump to false to prevent dump creation
            }
        }

        if (dumpBeforeGc)
        {
            Console.WriteLine("Taking thread dump before GC");
            await CaptureThreadDump(process, "initial");
            if (memDump)
                await CreateDump((uint)process.Id, "dump_before_gc.dmp");
        }

        ForceGc(process.Id);

        if (memDump)
            await CreateDump((uint)process.Id, "dump_after_gc.dmp");

        if (interval1 > 0)
        {
            await Task.Delay(interval1 * 1000);
            await CaptureThreadDump(process, "second");
            if (memDump)
                await CreateDump((uint)process.Id, $"dump_after_{interval1}s.dmp");
        }

        if (interval2 > interval1)
        {
            await Task.Delay((interval2 - interval1) * 1000);
            await CaptureThreadDump(process, "third");
            if (memDump)
                await CreateDump((uint)process.Id, $"dump_after_{interval2}s.dmp");
        }

        Console.WriteLine("Process completed. Dumps have been created in the current directory.");

        GenerateZipForOutput();
    }

    private static void GenerateZipForOutput()
    {
        const string sourceDirectory = "./";

        Console.WriteLine("Creating zip file...");

        var zipFileName = $"dumps_{DateTime.Now:yyyyMMddHHmmss}.zip";
        var zipFilePath = Path.Combine(sourceDirectory, zipFileName);

        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        long totalFileSize = 0;
        var filesToZip = Directory.GetFiles(sourceDirectory)
            .Where(file => Path.GetExtension(file).Equals(".dmp", StringComparison.OrdinalIgnoreCase)
                           || file.Contains("thread_dump", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var fileCount = filesToZip.Count;

        for (var i = 0; i < fileCount; i++)
        {
            var file = filesToZip[i];
            var fileName = Path.GetFileName(file);
            var fileInfo = new FileInfo(file);
            totalFileSize += fileInfo.Length;

            archive.CreateEntryFromFile(file, fileName, CompressionLevel.Fastest);

            Console.Write($"\rAdding file {i + 1} of {fileCount} ({(i + 1) * 100 / fileCount}%)");
        }

        var zipFileSizeMb = new FileInfo(zipFilePath).Length / (1024.0 * 1024.0);
        var totalFileSizeMb = totalFileSize / (1024.0 * 1024.0);

        Console.WriteLine("\nZip file created successfully: " + zipFilePath);
        Console.WriteLine($"Zip file size: {zipFileSizeMb:F2} MB");
        Console.WriteLine($"Total files size(all dumps): {totalFileSizeMb:F2} MB");
        Console.WriteLine(
            $"Compression ratio: {(1 - (double)new FileInfo(zipFilePath).Length / totalFileSize) * 100:F2}%");
        Console.WriteLine("Please send the zip file to the support for further investigation.");
    }

    private static Process GetProcess(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        switch (processes.Length)
        {
            case 0:
                throw new InvalidOperationException($"Process '{processName}' not found.");
            case > 1:
                Console.WriteLine($"Warning: Multiple processes named '{processName}' found. Using the first one.");
                break;
        }

        return processes[0];
    }

    private static async Task CreateDump(uint processId, string fileName)
    {
        var handle = OpenProcess(0x001F0FFF, false, processId);
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to open process {processId}");
            return;
        }

        try
        {
            await using var fs = new FileStream(fileName, FileMode.Create);
            var success = MiniDumpWriteDump(handle, processId, fs.SafeFileHandle.DangerousGetHandle(),
                MiniDumpWithFullMemory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine(success
                ? $"Dump created at {Path.GetFullPath(fileName)}"
                : $"Failed to create dump. Error code: {Marshal.GetLastWin32Error()}");
        }
        finally
        {
            CloseHandle(handle);
        }

        await Task.CompletedTask;
    }

    private static void ForceGc(int processId)
    {
        try
        {
            GcHelper.ForceGc("forcegc", processId, Console.Out);
            Console.WriteLine($"Requested GC on process {processId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to force GC: {ex.Message}");
        }
    }

    private static async Task CaptureThreadDump(Process process, string suffix)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Thread Dump for process {process.ProcessName} (ID: {process.Id}) at {DateTime.Now}");
        sb.AppendLine($"Total Working Set: {process.WorkingSet64 / 1024 / 1024} MB");
        sb.AppendLine($"Total Private Memory: {process.PrivateMemorySize64 / 1024 / 1024} MB");
        sb.AppendLine();

        using (var dataTarget = DataTarget.AttachToProcess(process.Id, false))
        {
            var runtime = dataTarget.ClrVersions.FirstOrDefault()?.CreateRuntime();
            sb.AppendLine($"CLR version:{runtime?.ClrInfo.Version}");
            if (runtime != null)
            {
                foreach (var thread in runtime.Threads)
                {
                    sb.AppendLine($"Thread ID: {thread.OSThreadId}");
                    sb.AppendLine($"Managed Thread ID: {thread.ManagedThreadId}");
                    sb.AppendLine($"Is GC Thread: {thread.IsGc}");
                    sb.AppendLine($"Is Finalizer Thread: {thread.IsFinalizer}");

                    var threadCpuTime = GetThreadCpuTime(thread.OSThreadId);
                    sb.AppendLine($"Thread CPU Time: {threadCpuTime.TotalMilliseconds:F2} ms");

                    var stackTrace = GetThreadStackInfo(thread);

                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        sb.AppendLine("Stack Trace:");
                        sb.AppendLine(stackTrace);
                    }

                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("Failed to attach to CLR runtime.");
            }
        }

        var fileName = $"thread_dump_{suffix}.txt";
        await File.WriteAllTextAsync(fileName, sb.ToString());
        Console.WriteLine($"Thread dump created at {Path.GetFullPath(fileName)}");
    }

    private static TimeSpan GetThreadCpuTime(uint osThreadId)
    {
        var thread = GetThreadById(osThreadId);
        return thread?.TotalProcessorTime ?? TimeSpan.Zero;
    }

    private static ProcessThread? GetThreadById(uint osThreadId)
    {
        return Process.GetCurrentProcess().Threads.Cast<ProcessThread>()
            .FirstOrDefault(t => t.Id == osThreadId)!;
    }

    private static string GetThreadStackInfo(ClrThread thread)
    {
        var sb = new StringBuilder();

        foreach (var frame in thread.EnumerateStackTrace())
        {
            sb.AppendLine($"  {frame}");
        }

        return sb.ToString();
    }
}