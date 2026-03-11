using Advantage.Data.Provider;
using System.Data;

namespace DashLine;

public class AdsService
{
    private readonly string _dataPath;
    private readonly string _serverType;
    private readonly ILogger<AdsService> _logger;

    public AdsService(IConfiguration config, ILogger<AdsService> logger)
    {
        _dataPath = config["DataPath"] ?? @"C:\Users\AVXUser\BMS\DATA";
        _serverType = config["ServerType"] ?? "LOCAL";
        _logger = logger;
    }

    private string GetConnectionString(string tableType = "CDX")
    {
        var st = _serverType.Equals("REMOTE", StringComparison.OrdinalIgnoreCase) ? "ADS_REMOTE_SERVER" : "ADS_LOCAL_SERVER";
        return $"Data Source={_dataPath};ServerType={st};TableType=ADS_CDX;CharType=OEM;TrimTrailingSpaces=TRUE;";
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

    private List<(string yldGrp, string bType, string sizeId, double lvalLim, double hvalLim, string tolId, string uomId, double expYld)>? _expqtyCache;

    private void LoadExpqtyCache()
    {
        _expqtyCache = new();
        try
        {
            using var conn = new AdsConnection(GetConnectionString("CDX"));
            conn.Open();

            using var cmd = new AdsCommand("SELECT YLD_GRP, B_TYPE, SIZE_ID, LVAL_LIM, HVAL_LIM, TOL_ID, UOM_ID, EXP_YLD FROM \"c_expqty.DBF\"", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var yg = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString()!.TrimEnd();
                var bt = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString()!.TrimEnd();
                var sz = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString()!.TrimEnd();
                var ll = reader.IsDBNull(3) ? 0.0 : Convert.ToDouble(reader.GetValue(3));
                var hl = reader.IsDBNull(4) ? 0.0 : Convert.ToDouble(reader.GetValue(4));
                var ti = reader.IsDBNull(5) ? "" : reader.GetValue(5).ToString()!.TrimEnd();
                var ui = reader.IsDBNull(6) ? "" : reader.GetValue(6).ToString()!.TrimEnd();
                var ey = reader.IsDBNull(7) ? 0.0 : Convert.ToDouble(reader.GetValue(7));
                _expqtyCache.Add((yg, bt, sz, ll, hl, ti, ui, ey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading c_expqty cache");
        }
    }

    public List<object> GetExpqtyEntries(string bType)
    {
        if (_expqtyCache == null) LoadExpqtyCache();
        return _expqtyCache!
            .Where(x => x.bType.Equals(bType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.sizeId)
            .ThenBy(x => x.uomId)
            .Select(x => (object)new { yldGrp = x.yldGrp, btype = x.bType, sizeId = x.sizeId, lvalLim = x.lvalLim, hvalLim = x.hvalLim, tolId = x.tolId, uomId = x.uomId, expYld = x.expYld })
            .ToList();
    }

    private List<(string bType, string plineId, string sizeId, double origAmt, string origUom, string convUom, double thQty, double ydQty, double lVal, double hVal)>? _thqtyCache;

    private void LoadThqtyCache()
    {
        _thqtyCache = new();
        try
        {
            using var conn = new AdsConnection(GetConnectionString("CDX"));
            conn.Open();

            using var cmd = new AdsCommand("SELECT B_TYPE, PLINE_ID, SIZE_ID, ORIG_AMT, ORIG_UOM, CONV_UOM, TH_QTY, YD_QTY, L_VAL, H_VAL FROM \"c_thqty.DBF\"", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var bt = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString()!.TrimEnd();
                var pl = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString()!.TrimEnd();
                var sz = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString()!.TrimEnd();
                var oa = reader.IsDBNull(3) ? 0.0 : Convert.ToDouble(reader.GetValue(3));
                var ou = reader.IsDBNull(4) ? "" : reader.GetValue(4).ToString()!.TrimEnd();
                var cu = reader.IsDBNull(5) ? "" : reader.GetValue(5).ToString()!.TrimEnd();
                var tq = reader.IsDBNull(6) ? 0.0 : Convert.ToDouble(reader.GetValue(6));
                var yq = reader.IsDBNull(7) ? 0.0 : Convert.ToDouble(reader.GetValue(7));
                var lv = reader.IsDBNull(8) ? 0.0 : Convert.ToDouble(reader.GetValue(8));
                var hv = reader.IsDBNull(9) ? 0.0 : Convert.ToDouble(reader.GetValue(9));
                _thqtyCache.Add((bt, pl, sz, oa, ou, cu, tq, yq, lv, hv));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading c_thqty cache");
        }
    }

    public List<object> GetThqtyEntries(string bType, string plineId)
    {
        if (_thqtyCache == null) LoadThqtyCache();
        return _thqtyCache!
            .Where(x => x.bType.Equals(bType, StringComparison.OrdinalIgnoreCase)
                     && x.plineId.Equals(plineId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.sizeId)
            .ThenBy(x => x.origUom)
            .Select(x => (object)new { sizeId = x.sizeId, origAmt = x.origAmt, origUom = x.origUom, convUom = x.convUom, thQty = x.thQty, ydQty = x.ydQty, lVal = x.lVal, hVal = x.hVal })
            .ToList();
    }

    public List<object> GetDlineEntries(string ptypeId, string plineId)
    {
        var results = new List<object>();
        try
        {
            using var conn = new AdsConnection(GetConnectionString());
            conn.Open();

            using var cmd = new AdsCommand(
                "SELECT B_PURP, B_ID, ESN_ID, B_STAT, B_PRIOR, B_DPROM, CP_BQTYP, CP_BQTYS, CP_BQTYW, CPPROC_ID, CP_PCCODE " +
                "FROM \"D_LINE.DBF\" " +
                "WHERE PTYPE_ID = :ptype AND PLINE_ID = :pline AND B_STAT NOT IN ('C','D')", conn);
            cmd.Parameters.Add(":ptype", ptypeId);
            cmd.Parameters.Add(":pline", plineId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new
                {
                    purp = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString()!.TrimEnd(),
                    batchId = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString()!.TrimEnd(),
                    esn = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString()!.TrimEnd(),
                    stat = reader.IsDBNull(3) ? "" : reader.GetValue(3).ToString()!.TrimEnd(),
                    prior = reader.IsDBNull(4) ? "" : reader.GetValue(4).ToString()!.TrimEnd(),
                    promised = reader.IsDBNull(5) ? "" : reader.GetValue(5)?.ToString() ?? "",
                    qtyP = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6)),
                    qtyS = reader.IsDBNull(7) ? 0 : Convert.ToInt32(reader.GetValue(7)),
                    qtyW = reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader.GetValue(8)),
                    proc = reader.IsDBNull(9) ? "" : reader.GetValue(9).ToString()!.TrimEnd(),
                    pccode = reader.IsDBNull(10) ? "" : reader.GetValue(10).ToString()!.TrimEnd()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading d_line");
        }
        return results;
    }

    public string GetDebugConnectionString() => GetConnectionString();

    public string GetDebugConnectionInfo()
    {
        return $"ServerType={_serverType}, DataPath={_dataPath}";
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
