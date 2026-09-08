using RHDomain.Controllers;

public partial class RPT_SALES_ORDERS
{
    public override void ConfigureGrid(FieldBuilder field)
    {
        // Demo field.Title - ghi đè tên hiển thị ở banner, không cần trùng label
        // khai trong menu (dbo.RH_MenuItem) nữa.
        field.Title("Báo cáo đơn hàng bán zz");

        // Demo tính năng: subheader tự do, trộn text thường với placeholder
        // "{TenFieldFilter}" - không gọi thì FE tự dùng template mặc định.
        field.Subtitle("Đơn hàng bán từ {FromDate}, xem đến hết {ToDate} nhé!");

        field.Grid("SalesOrderID", "Số đơn", FieldType.Int).Width(100);
        field.Grid("OrderDate", "Ngày đặt", FieldType.Date).Width(120).Align(GridAlign.Center);
        field.Grid("CustomerName", "Khách hàng", FieldType.String).Width(220).Align(GridAlign.Left);
        field.Grid("SubTotal", "Tiền hàng", FieldType.Number).Format("#,##0").Width(130);
        field.Grid("TaxAmt", "Thuế", FieldType.Number).Align(GridAlign.Right).Format("#,##0").Width(110);
        field.Grid("Freight", "Phí vận chuyển", FieldType.Number).Format("#,##0").Width(130);
        field.Grid("TotalDue", "Tổng cộng", FieldType.Number).Format("#,##0").Width(140);
    }
}
