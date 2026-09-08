using Dapper;
using Microsoft.Data.SqlClient;
using RHDomain.Menu;

namespace RHInfrastructure.Menu;

/// <summary>
/// Đọc cây menu điều hướng từ bảng dbo.RH_MenuItem (xem sql/001_create_rh_menu_item.sql) -
/// cho phép đổi cấu trúc menu (thêm/sửa/xoá mục, đổi thứ tự) chỉ bằng cách sửa data trong
/// bảng, không cần build lại FE/BE.
/// </summary>
public class MenuService
{
    private readonly string _connectionString;
    public MenuService(string connectionString) => _connectionString = connectionString;

    private class MenuRow
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Label { get; set; } = "";
        public string? ReportCode { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task<List<MenuNode>> GetTreeAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        var rows = (await conn.QueryAsync<MenuRow>(
            "SELECT Id, ParentId, Label, ReportCode, SortOrder FROM dbo.RH_MenuItem ORDER BY ParentId, SortOrder"))
            .ToList();

        var nodeById = rows.ToDictionary(r => r.Id, r => new MenuNode
        {
            Id = r.Id,
            Label = r.Label,
            ReportCode = r.ReportCode
        });

        var roots = new List<MenuNode>();
        foreach (var row in rows)
        {
            var node = nodeById[row.Id];
            if (row.ParentId is int parentId && nodeById.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }
}
