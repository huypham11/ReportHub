using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RHInfrastructure.Controllers;

public class DapperProcedureExecutor
{
    private readonly string _connectionString;
    public DapperProcedureExecutor(string connectionString) => _connectionString = connectionString;

    /// <summary>
    /// Chạy 1 SCRIPT SQL nhiều câu lệnh (DECLARE, nhiều SELECT, EXEC proc...) do report tự viết
    /// nguyên khối - giống hệt cách command event="Processing" của FBO dùng 1 khối CDATA. Tham
    /// số đặt tên THEO ĐÚNG TÊN FIELD Filter (VD field "FromDate" -> viết @FromDate trong script),
    /// không phải @p0/@p1 vị trí như các hàm khác - tự nhiên hơn khi script dài, nhiều tham số.
    /// Đọc HẾT các bảng kết quả (script có thể SELECT/EXEC nhiều lần), bảng đánh số từ 0 theo
    /// đúng thứ tự xuất hiện trong script.
    /// </summary>
    public async Task<List<List<Dictionary<string, object?>>>> ExecuteScriptMultipleAsync(
        string sqlScript, Dictionary<string, object?> namedParams)
    {
        using var conn = new SqlConnection(_connectionString);

        var dynParams = new DynamicParameters();
        foreach (var (name, value) in namedParams)
            dynParams.Add(name, value);

        using var reader = await conn.ExecuteReaderAsync(sqlScript, dynParams, commandType: CommandType.Text);

        var tables = new List<List<Dictionary<string, object?>>>();
        do
        {
            tables.Add(ReadRows(reader));
        } while (reader.NextResult());

        return tables;
    }

    /// <summary>
    /// Chạy thẳng 1 câu SQL do report tự viết (không cần tạo stored procedure) - vẫn tham số
    /// hoá qua @p0, @p1... để tránh SQL injection. Dùng khi chỉ cần 1 query đơn giản (VD: lấy
    /// giá trị hệ thống cho default value), không đáng để tạo hẳn 1 proc riêng.
    /// </summary>
    public async Task<List<Dictionary<string, object?>>> ExecuteSqlAsync(string sql, object?[] values)
    {
        using var conn = new SqlConnection(_connectionString);

        var dynParams = new DynamicParameters();
        for (var i = 0; i < values.Length; i++)
            dynParams.Add("p" + i, values[i]);

        using var reader = await conn.ExecuteReaderAsync(sql, dynParams, commandType: CommandType.Text);
        return ReadRows(reader);
    }

    private static List<Dictionary<string, object?>> ReadRows(IDataReader reader)
    {
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value is DBNull ? null : value;
            }
            rows.Add(row);
        }
        return rows;
    }
}
