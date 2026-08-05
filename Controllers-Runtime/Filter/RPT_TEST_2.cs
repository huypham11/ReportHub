using RHDomain.Controllers;

public partial class TestController2 : ControllerDefinitionBase
{
    public TestController2() => Name = "Report test 2";

    public override void ConfigureFilter(FieldBuilder builder)
    {
        builder.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
    }
}
