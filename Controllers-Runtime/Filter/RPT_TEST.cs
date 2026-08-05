using System;
using System.Linq;
using RHDomain.Controllers;

public partial class TestController : ControllerDefinitionBase
{
    public TestController() => Name = "Report test";

    public override void ConfigureFilter(FieldBuilder field)
    {
        var date1 = new DateTime(2026,1,1);
        field.Filter("FromDate", "Từ ngày", FieldType.Date)
               .Required()
               .DefaultValue(ctx => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));

        field.Filter("BranchId", "Chi nhánh", FieldType.Int)
               .DefaultValue(ctx => ctx.CurrentUser.BranchId);

        // Giá trị hệ thống lấy từ SQL Server (không phải người dùng tự nhập) - chỉ cần bấm
        // "Nhận" là dùng luôn giá trị này, khác với DefaultValue tính C# thuần ở trên.
        field.Filter("ToDate", "Đến ngày", FieldType.Date)
               .DefaultValue(ctx => ctx.QueryScalar("select top 1 OrderDate from SalesLT.SalesOrderHeader"));

        field.Filter("Test", "Test ngày", FieldType.Date)
               .DefaultValue(ctx => date1);

        field.Process("dbo.usp_Report_Test",
            P.Field("FromDate"),
            P.Field("BranchId"));
    }

    // Optional - không override thì field tự full-width theo đúng thứ tự ở ConfigureFilter.
    public override void ConfigureView(ViewBuilder view)
    {
        view.Row("FromDate:6", "ToDate:6"); // 2 field chung 1 hàng, mỗi cái nửa hàng
        view.Row("BranchId:12");            // full-width, xuống hàng riêng
        view.Row("Test:12");
    }
}
