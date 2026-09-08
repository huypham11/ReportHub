using RHDomain.Controllers;

public partial class TestController
{
    public override void ConfigureGrid(FieldBuilder field)
    {
        field.Title("Đơn giản là thế thôi");
        field.Subtitle("tôi là vậy đấy");

        field.Grid("Amount", "Số tiền", FieldType.Number).Format("#,#0").Align(GridAlign.Left);
        field.Grid("Discount", "Giảm giá", FieldType.Number).Format("#,##0");
        field.Grid("Total", "Thành tiền", FieldType.Number).Format("#,##0");
        field.Grid("Tax", "Thuế", FieldType.Number).Format("#,##0");
    }
}
