using RHDomain.Controllers;

public partial class RPT_TEST_2
{
    public override void ConfigureGrid(FieldBuilder builder)
    {
        builder.Grid("Amount", "Số tiền", FieldType.Number).Format("#,##0");
    }
}
