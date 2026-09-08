namespace RHDomain.Controllers;

public abstract class ControllerDefinitionBase
{
    // Code KHÔNG khai báo ở đây - Loader tự suy ra từ tên file (Filter/{code}.cs, Grid/{code}.cs).
    // Tên hiển thị của report cũng KHÔNG khai ở đây - lấy từ label trong menu
    // (dbo.RH_MenuItem), có thể ghi đè riêng qua field.Title(...) ở ConfigureFilter/ConfigureGrid.

    // ConfigureFilter CHỈ khai báo field lọc (tên, label, kiểu, Required, DefaultValue...) -
    // không có logic thực thi. Tách biệt khỏi ConfigureProcessing vì đây là 2 việc khác nhau:
    // khai input vs. lấy data, dù cả 2 đều đặt cùng file Filter/{code}.cs.
    public virtual void ConfigureFilter(FieldBuilder builder) { }

    // Processing = nơi thực sự lấy data (gọi proc/SQL), gắn cùng file Filter/{code}.cs (giống
    // command event="Processing" của FBO) nhưng dùng ProcessingBuilder RIÊNG, không phải
    // FieldBuilder - Processing không liên quan gì đến việc khai field.
    public virtual void ConfigureProcessing(ProcessingBuilder processing) { }

    // ConfigureGrid chỉ khai báo cột hiển thị, không có logic thực thi - optional.
    public virtual void ConfigureGrid(FieldBuilder builder) { }

    // Bố cục field trên màn hình Filter - optional, giống <views> nằm CHUNG file Filter XML
    // với <fields> trong FBO (không tách riêng file). Không override -> field tự full-width
    // theo đúng thứ tự khai báo ở ConfigureFilter.
    public virtual void ConfigureView(ViewBuilder view) { }
}
