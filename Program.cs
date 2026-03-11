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

app.MapGet("/api/data/dline/{ptypeId}/{plineId}", (string ptypeId, string plineId, AdsService ads) =>
{
    try
    {
        var entries = ads.GetDlineEntries(ptypeId, plineId);
        return Results.Ok(entries);
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

app.MapGet("/api/data/leadt/{plineId}", (string plineId, AdsService ads) =>
{
    try
    {
        return Results.Ok(ads.GetLeadtEntries(plineId));
    }
    catch
    {
        return Results.Problem("Failed to load data", statusCode: 500);
    }
});

app.MapGet("/api/tables", (AdsService ads) =>
{
    try
    {
        return Results.Ok(ads.ListTables());
    }
    catch
    {
        return Results.Problem("Failed to list tables", statusCode: 500);
    }
});

app.MapGet("/api/table/{name}", (string name, AdsService ads) =>
{
    try
    {
        var (columns, rows) = ads.ReadTable(name);
        return Results.Ok(new { columns, rows });
    }
    catch
    {
        return Results.Problem("Failed to read table", statusCode: 500);
    }
});

app.MapGet("/api/debug/dline-structure", (AdsService ads) =>
{
    try
    {
        using var conn = new Advantage.Data.Provider.AdsConnection(ads.GetDebugConnectionString());
        conn.Open();
        using var cmd = new Advantage.Data.Provider.AdsCommand("SELECT TOP 1 * FROM \"D_LINE.DBF\"", conn);
        using var reader = cmd.ExecuteReader();
        var columns = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));
        var sample = new List<Dictionary<string, object?>>();
        if (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (val is string s) val = s.TrimEnd();
                row[columns[i]] = val;
            }
            sample.Add(row);
        }
        return Results.Ok(new { columns, sample });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { error = ex.Message, detail = ex.InnerException?.Message });
    }
});

try { Console.Clear(); } catch { }
var config = app.Services.GetRequiredService<IConfiguration>();
Console.WriteLine("DataPath: " + (config["DataPath"] ?? "(default)"));
Console.WriteLine("ServerType: " + (config["ServerType"] ?? "(default=LOCAL)"));
var adsService = app.Services.GetRequiredService<AdsService>();
Console.WriteLine("Connection: " + adsService.GetDebugConnectionInfo());
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
