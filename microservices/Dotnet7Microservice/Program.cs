var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddControllers();

var app = builder.Build();


app.UseAuthorization();
app.MapGet("/hello", () =>
    Results.Json(new ApplicationInfo() { Year = DateTime.Now.Year, Name = "dotnet7" })
);
app.Run("http://localhost:8080");

public record ApplicationInfo
{
    public int Year { get; init; }
    public string? Name { get; init; }
}