using System.Diagnostics;
using System.Text.Json.Serialization;
using Dotnet8Microservice.Controllers;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Ozkanpakdil.OpenTelemetry.Exporter.Json.Console;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.NumberHandling = JsonNumberHandling.WriteAsString;
#pragma warning disable SYSLIB0049
        opt.JsonSerializerOptions.AddContext<SerializeOnlyContext>();
#pragma warning restore SYSLIB0049
    });
builder.Services.AddOpenTelemetry().WithTracing(builder =>
{
    builder
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter();
    
    if (Debugger.IsAttached)
    {
        // builder.AddConsoleExporter(options =>
        // {
        //     options.Targets = ConsoleExporterOutputTargets.Debug;
        // });
        builder.AddJsonConsoleExporter(options =>
        {
            options.Targets = ConsoleExporterOutputTargets.Debug;
        });
    }
});
var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run("http://localhost:8080");

Console.WriteLine("started...");