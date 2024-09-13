using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GCDumper.Tests;

[TestClass]
[TestSubject(typeof(Program))]
public class ProgramTest
{
    [TestMethod]
    public void CaptureThreadDump()
    {
        var process = Program.GetProcess("dotnet");
        var captureThreadDump = Program.CaptureThreadDump(process, suffix: "suf");
        Assert.AreEqual(TaskStatus.WaitingForActivation, captureThreadDump.Status);
    }
}