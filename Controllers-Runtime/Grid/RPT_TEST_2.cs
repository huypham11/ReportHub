using RHDomain.Controllers;

public partial class TestController2
{
    public override void ConfigureGrid(FieldBuilder builder)
    {
        builder.Grid("Amount", "Số tiền", FieldType.Number).Format("#,##0");
    }
}
