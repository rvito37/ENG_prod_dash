using DashLine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AdsService>();

builder.WebHost.UseUrls("http://127.0.0.1:5050");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/data/pline", (AdsService ads) =>
{
    try
    {
        var (columns, rows) = ads.ReadTable("C_PLINE");
        return Results.Ok(new { columns, rows });
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

var adsService = app.Services.GetRequiredService<AdsService>();
if (adsService.TestConnection())
    Console.WriteLine("ADS connection OK");
else
    Console.WriteLine("WARNING: ADS connection failed. Check DataPath in appsettings.json");

Console.WriteLine("Dashboard: http://127.0.0.1:5050");

try
{
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = "http://127.0.0.1:5050",
        UseShellExecute = true
    });
}
catch { }

app.Run();
