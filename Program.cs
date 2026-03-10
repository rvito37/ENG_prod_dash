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

app.MapGet("/api/data/btype/{plineId}", (string plineId, AdsService ads) =>
{
    try
    {
        var entries = ads.GetBtypeEntries(plineId);
        return Results.Ok(entries);
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

app.MapGet("/api/data/expqty/{bType}", (string bType, AdsService ads) =>
{
    try
    {
        var entries = ads.GetExpqtyEntries(bType);
        return Results.Ok(entries);
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

app.MapGet("/api/data/thqty/{bType}/{plineId}", (string bType, string plineId, AdsService ads) =>
{
    try
    {
        var entries = ads.GetThqtyEntries(bType, plineId);
        return Results.Ok(entries);
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

app.MapGet("/api/data/esnxx/{esnxxId}", (string esnxxId, AdsService ads) =>
{
    try
    {
        var entries = ads.GetEsnxxEntries(esnxxId);
        return Results.Ok(entries);
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

try { Console.Clear(); } catch { }
var adsService = app.Services.GetRequiredService<AdsService>();
if (adsService.TestConnection())
    Console.WriteLine("Dashboard is ready. Browser will open automatically.");
else
    Console.WriteLine("Connection error. Please check configuration.");

Console.WriteLine("Press Ctrl+C to stop.");

try
{
    // Try Chrome in app mode with small window on the left
    var chromePaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
    };
    var chrome = chromePaths.FirstOrDefault(File.Exists);
    if (chrome != null)
    {
        System.Diagnostics.Process.Start(chrome, "--app=http://127.0.0.1:5050 --window-size=1400,750 --window-position=50,50");
    }
    else
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "http://127.0.0.1:5050",
            UseShellExecute = true
        });
    }
}
catch { }

app.Run();
