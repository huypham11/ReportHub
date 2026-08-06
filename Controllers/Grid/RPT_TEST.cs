using RHDomain.Controllers;

public partial class TestController
{
    public override void ConfigureGrid(FieldBuilder builder)
    {
        builder.Grid("Amount", "Số tiền", FieldType.Number).Format("#,#0");
        builder.Grid("Discount", "Giảm giá", FieldType.Number).Format("#,##0");
        builder.Grid("Total", "Thành tiền", FieldType.Number).Format("#,##0");
        builder.Grid("Tax", "Thuế", FieldType.Number).Format("#,##0");
    }
}
