using System;
using System.Linq;
using RHDomain.Controllers;

public partial class RPT_TEST_2 : ControllerDefinitionBase
{
    public RPT_TEST_2() => Name = "Report test 2";

    public override void ConfigureFilter(FieldBuilder field)
    {
        var z = new DateTime(2026, 1, 1);
        var date1 = new DateTime(2026, 1, 2);
        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
        field.Filter("ToDate", "Đến ngày", FieldType.Date)
            .Required()
            .DefaultValue(ctx => date1);
    }
}
