using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/hello", () => 
    Results.Json(new ApplicationInfo() { Year = DateTime.Now.Year, Name = "dotnet6" })
);
app.Run("http://localhost:8080");


[JsonSerializable(typeof(ApplicationInfo))]
internal partial class MyContext : JsonSerializerContext
{
}

public record ApplicationInfo
{
    public int Year { get; set; }
    public string? Name { get; set; }
}