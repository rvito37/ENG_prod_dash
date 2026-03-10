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

    private List<(string plineId, string esnxxId, string bType, string btypeNme)>? _btypeCache;

    private void LoadBtypeCache()
    {
        _btypeCache = new();
        try
        {
            using var conn = new AdsConnection(GetConnectionString("CDX"));
            conn.Open();

            using var cmd = new AdsCommand("SELECT pline_id, esnxx_id, b_type, BTYPE_NME FROM \"c_btype.DBF\"", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var pl = reader.IsDBNull(0) ? "" : reader.GetString(0).TrimEnd();
                var es = reader.IsDBNull(1) ? "" : reader.GetString(1).TrimEnd();
                var bt = reader.IsDBNull(2) ? "" : reader.GetString(2).TrimEnd();
                var nm = reader.IsDBNull(3) ? "" : reader.GetString(3).TrimEnd();
                _btypeCache.Add((pl, es, bt, nm));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading c_btype cache");
        }
    }

    public List<object> GetBtypeEntries(string plineId)
    {
        if (_btypeCache == null) LoadBtypeCache();
        return _btypeCache!
            .Where(x => x.plineId.Equals(plineId, StringComparison.OrdinalIgnoreCase))
            .Select(x => (object)new { esnxx = x.esnxxId, btype = x.bType, name = x.btypeNme })
            .ToList();
    }

    private List<(string esnxxId, string esnxxNm, List<string> lines)>? _esnxxCache;

    private void LoadEsnxxCache()
    {
        _esnxxCache = new();
        try
        {
            using var conn = new AdsConnection(GetConnectionString("CDX"));
            conn.Open();

            using var cmd = new AdsCommand("SELECT ESNXX_ID, ESNXX_NM, ESNXXTL1, ESNXXTL2, ESNXXTL3, ESNXXTL4, ESNXXTL5, ESNXXTL6, ESNXXTL7 FROM \"c_esnxx.DBF\"", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var id = reader.IsDBNull(0) ? "" : reader.GetString(0).TrimEnd();
                var nm = reader.IsDBNull(1) ? "" : reader.GetString(1).TrimEnd();
                var lines = new List<string>();
                for (int i = 2; i <= 8; i++)
                {
                    var line = reader.IsDBNull(i) ? "" : reader.GetString(i).TrimEnd();
                    if (!string.IsNullOrEmpty(line)) lines.Add(line);
                }
                _esnxxCache.Add((id, nm, lines));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading c_esnxx cache");
        }
    }

    public List<object> GetEsnxxEntries(string esnxxId)
    {
        if (_esnxxCache == null) LoadEsnxxCache();
        return _esnxxCache!
            .Where(x => x.esnxxId.Equals(esnxxId, StringComparison.OrdinalIgnoreCase))
            .Select(x => (object)new { id = x.esnxxId, name = x.esnxxNm, lines = x.lines })
            .ToList();
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
