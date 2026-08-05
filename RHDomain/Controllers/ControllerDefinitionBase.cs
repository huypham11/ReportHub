namespace RHDomain.Controllers;

public abstract class ControllerDefinitionBase
{
    // Code KHÔNG khai báo ở đây - Loader tự suy ra từ tên file (Filter/{code}.cs, Grid/{code}.cs).
    public string Name { get; protected set; } = "";

    // ConfigureFilter là nơi bắt buộc khai báo field lọc + Processing (gọi proc lấy data),
    // giống cơ chế command event="Processing" của Filter XML trong FBO.
    public virtual void ConfigureFilter(FieldBuilder builder) { }

    // ConfigureGrid chỉ khai báo cột hiển thị, không có logic thực thi - optional.
    public virtual void ConfigureGrid(FieldBuilder builder) { }

    // Bố cục field trên màn hình Filter - optional, giống <views> nằm CHUNG file Filter XML
    // với <fields> trong FBO (không tách riêng file). Không override -> field tự full-width
    // theo đúng thứ tự khai báo ở ConfigureFilter.
    public virtual void ConfigureView(ViewBuilder view) { }
}
