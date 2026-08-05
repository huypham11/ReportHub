namespace RHDomain.Controllers;

public class ViewRowItem
{
    public string Field { get; set; } = "";

    // Thang 12 cột (kiểu Bootstrap/Ant Design) - không dùng px cố định như FBO.
    // Không khai báo -> mặc định 12 (full-width, xuống hàng riêng).
    public int Span { get; set; } = 12;
}

/// <summary>
/// Khai báo field hiển thị ở đâu trên màn hình Filter - tương đương &lt;views&gt; của FBO,
/// nhưng dùng lưới 12 cột thay vì mảng độ rộng px + chuỗi mã hoá occupancy. Field nào không
/// được nhắc tới ở đây vẫn hiển thị bình thường (full-width, theo đúng thứ tự khai báo ở
/// ConfigureFilter) - ConfigureView là optional, chỉ cần khi muốn tuỳ biến bố cục.
/// </summary>
public class ViewBuilder
{
    public List<List<ViewRowItem>> Rows { get; } = new();

    /// <summary>
    /// Mỗi item dạng "TenField" (mặc định Span=12) hoặc "TenField:Span" (VD "FromDate:6").
    /// </summary>
    public void Row(params string[] items)
    {
        Rows.Add(items.Select(ParseItem).ToList());
    }

    private static ViewRowItem ParseItem(string item)
    {
        var parts = item.Split(':', 2);
        var span = parts.Length > 1 && int.TryParse(parts[1], out var s) ? s : 12;
        return new ViewRowItem { Field = parts[0], Span = span };
    }
}
