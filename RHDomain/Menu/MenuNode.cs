namespace RHDomain.Menu;

/// <summary>
/// 1 node trong cây menu điều hướng - ReportCode NULL nghĩa là node category
/// (thư mục, chỉ để nhóm), khác NULL nghĩa là node lá, click vào để mở report.
/// </summary>
public class MenuNode
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public string? ReportCode { get; set; }
    public List<MenuNode> Children { get; set; } = new();
}
