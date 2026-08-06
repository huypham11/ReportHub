using System;
using System.Linq;
using RHDomain.Controllers;

public partial class TestController4 : ControllerDefinitionBase
{
    public TestController4() => Name = "Report test 4";

    public override void ConfigureFilter(FieldBuilder field)
    {
        var z = new DateTime(2026,01,01);
        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
        field.Filter("ToDate", "Đến ngày", FieldType.Date)
            .Required()
            .DefaultValue(ctx => z);
    }
}
