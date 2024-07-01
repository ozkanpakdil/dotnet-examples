using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;

ConsoleLogger.Instance.DebugLogLevelEnabled = true;

var customImage = new ImageFromDockerfileBuilder()
    .WithDockerfileDirectory(CommonDirectoryPath.GetCallerFileDirectory(), string.Empty)
    .WithDockerfile("Dockerfile.suse")
    .Build();

await customImage.CreateAsync()
    .ConfigureAwait(false);

var container = new ContainerBuilder()
    .WithImage(customImage)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
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