using System;
using RHDomain.Controllers;

public partial class RPT_SALES_ORDERS : ControllerDefinitionBase
{
    public override void ConfigureFilter(FieldBuilder field)
    {
        // Demo field.Title - title RIÊNG cho modal điều kiện lọc, độc lập với
        // field.Title khai ở Grid.cs (banner màn kết quả).
        field.Title("Điều kiện lọc");

        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required().DefaultValue(ctx => new DateTime(2008, 1, 1));
        field.Filter("ToDate", "Đến ngày", FieldType.Date).Required().DefaultValue(ctx => new DateTime(2008, 6, 30));
    }

    public override void ConfigureProcessing(ProcessingBuilder processing)
    {
        // Script chỉ SELECT 1 lần -> vẫn phải khai TableId(0) (bắt buộc, không mặc định ngầm).
        processing.TableId(0);
        processing.Sql(@"
            SELECT
                h.SalesOrderID,
                h.OrderDate,
                ISNULL(c.CompanyName, c.FirstName + ' ' + c.LastName) AS CustomerName,
                h.SubTotal,
                h.TaxAmt,
                h.Freight,
                h.TotalDue
            FROM SalesLT.SalesOrderHeader h
            JOIN SalesLT.Customer c ON c.CustomerID = h.CustomerID
            WHERE h.OrderDate BETWEEN @FromDate AND @ToDate
            ORDER BY h.OrderDate");
    }

    public override void ConfigureView(ViewBuilder view)
    {
        view.Row("FromDate:3", "ToDate:3");
    }
}
