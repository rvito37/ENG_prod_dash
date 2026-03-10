using Advantage.Data.Provider;
using System.Data;

namespace DashLine;

public class AdsService
{
    private readonly string _dataPath;
    private readonly ILogger<AdsService> _logger;

    public AdsService(IConfiguration config, ILogger<AdsService> logger)
    {
        _dataPath = config["DataPath"] ?? @"C:\Users\AVXUser\BMS\DATA";
        _logger = logger;
    }

    private string GetConnectionString(string tableType = "CDX")
    {
        return $"Data Source={_dataPath};ServerType=LOCAL;TableType={tableType};LockMode=COMPATIBLE;CharType=OEM;TrimTrailingSpaces=TRUE;";
    }

    public (List<string> columns, List<List<object?>> rows) ReadTable(string tableName)
    {
        var columns = new List<string>();
        var rows = new List<List<object?>>();

        try
        {
            using var conn = new AdsConnection(GetConnectionString());
            conn.Open();

            string sql = $"SELECT * FROM \"{tableName}.DBF\"";
            using var cmd = new AdsCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            while (reader.Read())
            {
                var row = new List<object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    if (val is string s)
                        val = s.TrimEnd();
                    row.Add(val);
                }
                rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading table {Table}", tableName);
            throw new InvalidOperationException("Data load error");
        }

        return (columns, rows);
    }

    public bool TestConnection()
    {
        try
        {
            using var conn = new AdsConnection(GetConnectionString());
            conn.Open();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ADS connection test failed");
            return false;
        }
    }
}
