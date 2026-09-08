namespace RHDomain.Controllers;

public class CurrentUserInfo
{
    public int BranchId { get; set; }
}

public class ExecutionContext
{
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public CurrentUserInfo CurrentUser { get; set; } = new();
    public List<(string Field, string Message)> Errors { get; } = new();

    // Kết quả mà Processing (khai báo trong Filter) để lại cho Grid đọc khi trả response.
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    // Do tầng Infrastructure gán vào lúc build context (Domain không biết Dapper/SqlClient là gì).
    public Func<string, object?[], Task<List<Dictionary<string, object?>>>>? SqlExecutor { get; set; }
    public Func<string, Dictionary<string, object?>, Task<List<List<Dictionary<string, object?>>>>>? ScriptExecutor { get; set; }

    public T? GetParam<T>(string name)
        => Parameters.TryGetValue(name, out var v) && v is not null ? (T)v : default;

    public void AddError(string field, string message) => Errors.Add((field, message));

    /// <summary>
    /// Chạy 1 SCRIPT SQL nhiều câu lệnh (DECLARE, nhiều SELECT, EXEC proc...) viết nguyên khối -
    /// cách DUY NHẤT để khai Processing, giống command event="Processing" của FBO. Tham số đặt
    /// tên THEO ĐÚNG TÊN FIELD Filter đã khai (VD field "FromDate" -> viết @FromDate trong script) -
    /// tự động lấy hết từ ctx.Parameters, không cần truyền tay. Đọc HẾT các bảng kết quả, bảng
    /// đánh số từ 0 theo đúng thứ tự script SELECT/EXEC - tự chọn 1 bảng gán vào ctx.Rows.
    /// </summary>
    public Task<List<List<Dictionary<string, object?>>>> RunScript(string sqlScript)
    {
        if (ScriptExecutor == null)
            throw new InvalidOperationException("ScriptExecutor chưa được cấu hình cho ExecutionContext.");
        return ScriptExecutor(sqlScript, Parameters);
    }

    /// <summary>
    /// Viết thẳng 1 câu SQL, không cần tạo stored procedure. Dùng @p0, @p1... trong câu SQL
    /// ứng với thứ tự values truyền vào (giống hệt cách CallProcedure dùng tham số vị trí).
    /// </summary>
    public Task<List<Dictionary<string, object?>>> QuerySql(string sql, params object?[] values)
    {
        if (SqlExecutor == null)
            throw new InvalidOperationException("SqlExecutor chưa được cấu hình cho ExecutionContext.");
        return SqlExecutor(sql, values);
    }

    /// <summary>
    /// Dùng khi chỉ cần lấy đúng 1 giá trị (VD: default value cho 1 field) - tự lấy ra giá trị
    /// duy nhất, không bắt người viết Filter.cs phải tự đào rows[0].Values.First(). Ném lỗi nếu
    /// kết quả không phải đúng 1 dòng/1 cột, để lộ rõ query sai ngay khi chạy, không âm thầm
    /// lấy nhầm dòng đầu tiên.
    /// </summary>
    public async Task<object?> QueryScalar(string sql, params object?[] values)
    {
        var rows = await QuerySql(sql, values);

        if (rows.Count != 1)
            throw new InvalidOperationException(
                $"QueryScalar yêu cầu đúng 1 dòng kết quả, nhưng query trả về {rows.Count} dòng.");

        var row = rows[0];
        if (row.Count != 1)
            throw new InvalidOperationException(
                $"QueryScalar yêu cầu đúng 1 cột kết quả, nhưng query trả về {row.Count} cột.");

        return row.Values.First();
    }
}
