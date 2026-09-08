# ReportHub — Tóm tắt Core Engine (phiên làm việc này)

## Mục tiêu

Xây dựng **core API** cho phép khai báo 1 báo cáo mới bằng cách **chỉ tạo file `.cs`** trong
thư mục `Controllers/`, không cần build lại hay sửa code trong 4 project chính
(`RHApi`, `RHApplication`, `RHDomain`, `RHInfrastructure`).

## Kiến trúc cuối cùng

```
ReportHub/
├── Controllers/                  <- KHÔNG thuộc project nào, chỉ chứa khai báo report
│   ├── Filter/{code}.cs          <- field lọc + bố cục (View) + Processing (gọi proc/SQL)
│   └── Grid/{code}.cs            <- cột hiển thị kết quả (optional)
├── RHDomain/Controllers/         <- hợp đồng chung (POCO, không phụ thuộc Dapper/SQL)
│   ├── ControllerDefinitionBase.cs
│   ├── FieldBuilder.cs           <- khai báo field (dùng chung Filter + Grid)
│   ├── ViewBuilder.cs            <- khai báo bố cục (lưới 12 cột)
│   ├── ExecutionContext.cs       <- Parameters, Rows, CallProcedure(), QuerySql(), QueryScalar()
│   ├── ExecutionResult.cs
│   └── ProcParam.cs              <- P.Field(...) / P.Value(...)
├── RHInfrastructure/Controllers/ <- hạ tầng thật (Roslyn, Dapper, SQL Server)
│   ├── ControllerLoader.cs       <- compile runtime bằng Roslyn, hot reload, track lỗi
│   ├── CompiledController.cs
│   ├── DapperProcedureExecutor.cs
│   └── ObjectExecutionService.cs <- điều phối validate -> Processing -> lọc Grid -> trả kết quả
└── RHApi/Controllers/
    └── DynamicControllersController.cs
        GET  /api/controllers/{code}/filter
        GET  /api/controllers/{code}/grid
        GET  /api/controllers/{code}/view
        POST /api/controllers/{code}/execute
        POST /api/controllers/reload
        GET  /api/controllers/errors
```

## Các quyết định thiết kế quan trọng

1. **Compile runtime bằng Roslyn** (`Microsoft.CodeAnalysis.CSharp`), không cần rebuild solution.
   `FileSystemWatcher` theo dõi `Controllers/`, tự compile lại khi file đổi (bắt cả
   `Changed`/`Created`/`Renamed` — nhiều editor lưu file bằng cách rename đè, không phát sinh
   `Changed`).

2. **Tổ chức file theo LOẠI, không theo controller**: `Filter/{code}.cs` và `Grid/{code}.cs`,
   tên file (không đuôi) chính là `Code` — không cần khai báo lại trong code. 2 file dùng
   `partial class` cùng tên để ghép lại khi compile.

3. **View nằm chung file với Filter** (đúng cách FBO gộp `<fields>`+`<views>` trong 1 file Filter
   XML), không tách riêng. Bố cục dùng lưới **12 cột** (`view.Row("Field:6", "Field2:6")`)
   thay vì mảng độ rộng px + chuỗi mã hoá occupancy như FBO — đơn giản hơn nhiều nhờ CSS
   Grid/Flexbox của web, nhưng vẫn tuỳ biến được tỷ lệ từng field.

4. **Processing gắn ở Filter** (giống `command event="Processing"` của FBO) — Filter không chỉ
   mô tả input, còn là nơi kích hoạt lấy data. `builder.Process(proc, P.Field("X"), P.Value(3))`
   là lối tắt phổ biến nhất: gọi 1 proc, tham số theo **vị trí** (`EXEC proc @p0, @p1...`),
   không theo tên — cho phép trộn giá trị lấy từ Filter và giá trị literal cố định.

5. **Không bắt buộc phải có stored procedure** — `ctx.QuerySql(sql, ...)` cho phép viết thẳng
   câu SQL tham số hoá. `ctx.QueryScalar(sql, ...)` dùng khi chỉ cần đúng 1 giá trị (ném lỗi
   nếu query trả về khác 1 dòng/1 cột) — hay dùng cho `DefaultValue` cần lấy giá trị hệ thống.

6. **`DefaultValue` hỗ trợ cả đồng bộ lẫn bất đồng bộ** — field có thể lấy giá trị mặc định từ
   tính toán C# thuần hoặc từ query SQL Server thật, chỉ cần bấm "Nhận" là dùng luôn, không bắt
   người dùng tự nhập.

7. **Tất cả lỗi compile/hot-reload được track lại** (`ControllerLoader.LastErrors`), expose qua
   `GET /api/controllers/errors` và trả `409` kèm chi tiết lỗi ở mọi endpoint liên quan — tránh
   tình trạng lỗi âm thầm chỉ hiện trên console.

## Quy trình thêm 1 report mới (từ giờ, không cần đụng Core)

```csharp
// Filter/RPT_XXX.cs
public partial class XxxController : ControllerDefinitionBase
{
    public override void ConfigureFilter(FieldBuilder field)
    {
        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
    }

    public override void ConfigureProcessing(ProcessingBuilder processing)
    {
        processing.TableId(0);
        processing.Sql("EXEC dbo.usp_Xxx @FromDate");
    }

    public override void ConfigureView(ViewBuilder view)
    {
        view.Row("FromDate:12");
    }
}

// Grid/RPT_XXX.cs
public partial class XxxController
{
    public override void ConfigureGrid(FieldBuilder field)
    {
        field.Grid("ColumnName", "Nhãn", FieldType.Number).Format("#,##0");
    }
}
```

Lưu file → hot reload tự nhận → gọi ngay `/filter`, `/view`, `/grid`, `/execute`.

## Việc còn để ngỏ (chưa làm trong phiên này)

- Phân quyền / lấy `CurrentUser` thật (đang hard-code `BranchId = 1` trong
  `DynamicControllersController`).
- Lookup field (combobox load từ 1 controller khác) — đã bàn thiết kế, chưa code.
- FE React — chưa bắt đầu, mới có API.
- `Category`/tab cho Filter phức tạp (nhiều field) — mới dừng ở mức ý tưởng.
