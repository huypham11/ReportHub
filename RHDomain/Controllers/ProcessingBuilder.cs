namespace RHDomain.Controllers;

/// <summary>
/// Nơi khai báo Processing (lấy data) và Checking (validate thêm) - tách RIÊNG khỏi
/// FieldBuilder vì không liên quan gì đến việc khai field, dù cùng nằm trong file
/// Filter/{code}.cs (khai qua ConfigureProcessing, giống command event="Processing"
/// của FBO gắn ở Filter XML).
/// </summary>
public class ProcessingBuilder
{
    public Func<ExecutionContext, Task>? ProcessingHook { get; private set; }
    public List<Func<ExecutionContext, Task>> CheckingHooks { get; } = new();

    public void OnChecking(Func<ExecutionContext, Task> hook) => CheckingHooks.Add(hook);

    // BẮT BUỘC gọi trước khi dùng Sql(...) - chỉ định rõ bảng kết quả nào (đánh số từ 0
    // theo đúng thứ tự SELECT/EXEC xuất hiện trong script) đổ vào Grid, kể cả khi script
    // chỉ SELECT 1 lần (TableId(0)). Không cho mặc định ngầm để tránh hiểu lầm rồi sau
    // này ai đó thêm 1 SELECT nữa vào script mà quên sửa Filter.cs. Gọi trước hay sau
    // Sql(...) trong ConfigureProcessing đều được (Sql đọc TableIndex lúc Processing
    // THỰC SỰ chạy, không phải lúc ConfigureProcessing chạy).
    public int? TableIndex { get; private set; }

    public void TableId(int index) => TableIndex = index;

    /// <summary>
    /// Cách DUY NHẤT để khai Processing - viết nguyên 1 script SQL (DECLARE, nhiều SELECT,
    /// EXEC proc...), giống hệt command event="Processing" của FBO dùng 1 khối CDATA. Tham
    /// số đặt tên THEO ĐÚNG TÊN FIELD Filter đã khai (VD field "FromDate" -> viết @FromDate
    /// trong script) - tự lấy từ giá trị người dùng nhập, không cần P.Field/P.Value gì cả.
    /// BẮT BUỘC gọi processing.TableId(...) trước đó, kể cả khi script chỉ SELECT 1 lần
    /// (TableId(0)).
    /// </summary>
    public void Sql(string sqlScript)
    {
        ProcessingHook = async ctx =>
        {
            if (TableIndex is not int index)
                throw new InvalidOperationException(
                    "ConfigureProcessing gọi Sql(...) nhưng chưa khai processing.TableId(...) - " +
                    "bắt buộc chỉ định rõ bảng kết quả nào đổ vào Grid (đánh số từ 0), kể cả khi " +
                    "script chỉ SELECT 1 lần thì vẫn phải gọi TableId(0).");

            var tables = await ctx.RunScript(sqlScript);

            if (index < 0 || index >= tables.Count)
                throw new InvalidOperationException(
                    $"TableId({index}): script trả về {tables.Count} bảng, không có bảng số {index}.");

            ctx.Rows = tables[index];
        };
    }
}
