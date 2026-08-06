using System;
using System.Linq;
using RHDomain.Controllers;

public partial class TestController3 : ControllerDefinitionBase
{
    public TestController3() => Name = "Report test 3";

    public override void ConfigureFilter(FieldBuilder field)
    {
        var z = new DateTime(2026, 1, 1);
        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
        field.Filter("ToDate", "Đến ngày", FieldType.Date)
            .Required()
            .DefaultValue(ctx => z);
    }
}
