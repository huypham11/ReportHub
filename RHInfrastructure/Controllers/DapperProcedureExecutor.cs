using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RHInfrastructure.Controllers;

/// <summary>
/// Gọi proc theo tham số VỊ TRÍ (không theo tên) - EXEC {proc} @p0, @p1, @p2...
/// SQL Server tự khớp @p0, @p1... vào đúng tham số proc theo THỨ TỰ khai báo, bất kể tên
/// biến trong proc là gì. Vẫn tham số hoá đầy đủ qua DynamicParameters - không SQL injection.
/// </summary>
public class DapperProcedureExecutor
{
    private readonly string _connectionString;
    public DapperProcedureExecutor(string connectionString) => _connectionString = connectionString;

    public async Task<List<Dictionary<string, object?>>> ExecuteAsync(string procedureName, object?[] values)
    {
        using var conn = new SqlConnection(_connectionString);

        var dynParams = new DynamicParameters();
        var placeholders = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var name = "p" + i;
            dynParams.Add(name, values[i]);
            placeholders[i] = "@" + name;
        }

        var sql = values.Length > 0
            ? $"EXEC {procedureName} {string.Join(", ", placeholders)}"
            : $"EXEC {procedureName}";

        using var reader = await conn.ExecuteReaderAsync(sql, dynParams, commandType: CommandType.Text);
        return ReadRows(reader);
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
