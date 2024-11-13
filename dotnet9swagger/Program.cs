using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.TypeInfoResolverChain.Add(MyContext.Default); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();


var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();
app.UseOpenApi();
app.UseSwaggerUi();
app.Run("http://localhost:8080");


[JsonSerializable(typeof(ApplicationInfo))]
public partial class MyContext : JsonSerializerContext
{
}


[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public ApplicationInfo Get() => new()
    {
        Year = DateTime.Now.Year,
        Name = "dotnet9"
    };
}

public record ApplicationInfo
{
    public int Year { get; set; }
    public string Name { get; set; }
}