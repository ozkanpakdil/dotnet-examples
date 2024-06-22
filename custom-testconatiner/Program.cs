using System;
using System.Threading.Tasks;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

ConsoleLogger.Instance.DebugLogLevelEnabled = true;

var container = new ContainerBuilder()
    .WithImage("ozkanpakdil/mssql-ubuntu")
    .WithEnvironment("TERM", "xterm")
    .Build();

try
{
    await container.StartAsync();
    
    var logs = await container.GetLogsAsync();
    Console.WriteLine("Container logs:");
    Console.WriteLine(logs);

    var execResult = await container.ExecAsync(new[] { "/bin/bash", "-c", "ls" });
    Console.WriteLine($"Exec exit code: {execResult.ExitCode}");
    Console.WriteLine($"Exec output: {execResult.Stdout}");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
finally
{
    await container.DisposeAsync();
}