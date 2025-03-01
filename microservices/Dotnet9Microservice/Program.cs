using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.TypeInfoResolverChain.Add(MyContext.Default); });

var app = builder.Build();
app.UseAuthorization();
app.MapGet("/hello", () => 
    Results.Json(new ApplicationInfo() { year = DateTime.Now.Year, name = "dotnet9" }, MyContext.Default.ApplicationInfo)
);
app.Run("http://localhost:8080");


[JsonSerializable(typeof(ApplicationInfo))]
internal partial class MyContext : JsonSerializerContext
{
}

public record ApplicationInfo
{
    public int year { get; set; }
    public string? name { get; set; }
}
