# Hướng dẫn tạo 1 báo cáo mới trong ReportHub

Tài liệu này dành cho việc **thêm 1 report mới**. Không cần đụng tới `RHApi`,
`RHApplication`, `RHDomain`, `RHInfrastructure` — chỉ tạo file `.cs` trong thư mục
`Controllers/` ở gốc solution. Muốn hiểu sâu hơn về kiến trúc/lý do thiết kế, xem
[core-engine-summary.md](core-engine-summary.md).

## Bước 0 — Đặt tên (Code) cho report

Chọn 1 mã report, ví dụ `RPT_XXX`. Mã này **chính là tên file** (không đuôi `.cs`) —
không khai báo `Code` ở đâu trong code cả, Loader tự suy ra từ tên file.

## Bước 1 — Tạo file Filter (bắt buộc)

Tạo `Controllers/Filter/RPT_XXX.cs`:

```csharp
using RHDomain.Controllers;

public partial class RPT_XXX : ControllerDefinitionBase
{
    public RPT_XXX() => Name = "Tên report hiển thị cho người dùng";

    public override void ConfigureFilter(FieldBuilder field)
    {
        field.Filter("FromDate", "Từ ngày", FieldType.Date).Required();
        field.Filter("ToDate", "Đến ngày", FieldType.Date).Required();
    }

    // ConfigureProcessing: nơi thực sự lấy data, dùng ProcessingBuilder RIÊNG (không
    // phải FieldBuilder - Processing không liên quan gì đến khai field), cùng file
    // Filter/{code}.cs, giống command event="Processing" của FBO.
    public override void ConfigureProcessing(ProcessingBuilder processing)
    {
        processing.TableId(0);
        processing.Sql("EXEC dbo.usp_Xxx @FromDate, @ToDate");
    }
}
```

- Tên class **phải trùng tên file** (`RPT_XXX`), kế thừa `ControllerDefinitionBase`.
- `field.Filter(name, label, type)` khai 1 field lọc. `FieldType` hiện có:
  `Date`, `Int`, `String`, `Number`.
- `.Required()` — bắt buộc nhập.
- `.DefaultValue(...)` — có 2 overload:
  - `DefaultValue(ctx => ...)` — tính thuần C#, không chạm DB (VD:
    `ctx.CurrentUser.BranchId`, `new DateTime(...)`).
  - `DefaultValue(async ctx => ...)` — cần lấy giá trị hệ thống từ SQL Server, dùng
    `ctx.QueryScalar(...)` bên trong.
- `.AsLookup("OTHER_CODE")` — đánh dấu field này là combobox, load dữ liệu từ 1
  report khác theo `Code` (thiết kế đã có, FE dùng để load nguồn lookup).

### Gọi data ở `ConfigureProcessing` — CHỈ 1 CÁCH DUY NHẤT

Processing chỉ có đúng 2 method: `TableId(n)` và `Sql(script)`, cả 2 đều bắt buộc
dùng chung (không có cách nào khác, không có lối tắt gọi proc riêng, không có
`OnProcessing` tự viết) — để mọi report Processing đều 1 kiểu, không phải mỗi
report một cách viết.

```csharp
public override void ConfigureProcessing(ProcessingBuilder processing)
{
    processing.TableId(0); // script bên dưới SELECT/EXEC bao nhiêu lần cũng được,
                            // chỉ định rõ bảng số mấy (đếm từ 0) đổ vào Grid
    processing.Sql(@"
        SELECT * FROM SomeTable
        WHERE CreatedDate >= @FromDate AND CreatedDate <= @ToDate");
}
```

- **`processing.TableId(n)`** — BẮT BUỘC gọi (trước hay sau `Sql(...)` đều được),
  kể cả khi script chỉ `SELECT`/`EXEC` đúng 1 lần thì vẫn phải `TableId(0)`. Không
  cho mặc định ngầm để tránh về sau ai đó thêm 1 `SELECT` vào script mà quên sửa
  Filter.cs rồi Grid tự nhiên nhận nhầm bảng. Chọn số bảng không tồn tại sẽ ném
  lỗi rõ ràng ngay khi chạy (`TableId(n): script trả về X bảng, không có bảng số n`).
- **`processing.Sql(script)`** — viết NGUYÊN 1 script SQL, có thể nhiều câu lệnh
  (`DECLARE`, nhiều `SELECT` để tính biến trung gian, cuối cùng `EXEC` 1 proc hoặc
  `SELECT` thẳng ra data) — giống hệt cách 1 khối CDATA trong
  `command event="Processing"` của FBO. Gọi proc thì viết thẳng `EXEC dbo.usp_Xxx
  @FromDate, @ToDate` bên trong script, không cần API riêng.
  - **Tham số đặt tên THEO ĐÚNG TÊN FIELD Filter đã khai** (field `"FromDate"` ->
    viết `@FromDate` trong script) — tự động lấy giá trị người dùng nhập, không
    cần `P.Field`/`P.Value` hay truyền tay gì cả.
  - Script trả về NHIỀU bảng (nhiều `SELECT`/`EXEC`) hoàn toàn bình thường — chỉ
    bảng đã chọn qua `TableId(...)` mới đổ vào Grid, các bảng khác (VD dùng để
    tính header, biến trung gian) bị bỏ qua.

**Validate thêm trước khi Processing chạy** (tuỳ chọn, cũng khai ở `ConfigureProcessing`):

```csharp
processing.OnChecking(async ctx =>
{
    var from = ctx.GetParam<DateTime>("FromDate");
    var to = ctx.GetParam<DateTime>("ToDate");
    if (from > to) ctx.AddError("ToDate", "Đến ngày phải sau Từ ngày");
});
```

## Bước 2 — Tạo file Grid (tuỳ chọn — chỉ cần khi report có kết quả dạng bảng)

Tạo `Controllers/Grid/RPT_XXX.cs`. Class là **`partial` của cùng class Filter**,
Roslyn sẽ ghép 2 file lại khi compile:

```csharp
using RHDomain.Controllers;

public partial class RPT_XXX
{
    public override void ConfigureGrid(FieldBuilder field)
    {
        field.Grid("Amount", "Số tiền", FieldType.Number).Format("#,##0").Width(140);
        field.Grid("CustomerName", "Khách hàng", FieldType.String).Width(220).Align(GridAlign.Center);
    }
}
```

- `field.Grid(name, label, type)` khai 1 cột hiển thị. `.Format(...)` tuỳ chọn
  (VD `"#,##0"` cho số tiền). `.Width(px)` tuỳ chọn — không gọi thì FE tự set độ
  rộng mặc định (co giãn theo nội dung). `.Align(GridAlign.Left/Center/Right)`
  tuỳ chọn — căn lề DỮ LIỆU trong cột (không gọi thì tự căn phải cho
  `Number`/`Int`, căn trái cho các kiểu còn lại). Header cột luôn căn giữa,
  không phụ thuộc `.Align(...)`.
- Cột không cần khớp 100% với field trong Filter — đây là cột hiển thị của
  `Rows` mà Processing trả về.

## Bước 3 — Tuỳ biến bố cục màn hình Filter (tuỳ chọn)

Mặc định, không khai `ConfigureView` thì mỗi field full-width (span 12), xuống
hàng theo đúng thứ tự khai ở `ConfigureFilter`. Muốn xếp nhiều field 1 hàng, thêm
vào file Filter:

```csharp
public override void ConfigureView(ViewBuilder view)
{
    view.Row("FromDate:6", "ToDate:6"); // 2 field, mỗi field nửa hàng (thang 12 cột)
    view.Row("Note:12");                // field còn lại full-width, hàng riêng
}
```

Cú pháp mỗi item: `"TenField"` (span mặc định 12) hoặc `"TenField:Span"`.

## Bước 4 — Lưu file, hot reload tự nhận

Không cần build lại solution. `FileSystemWatcher` tự phát hiện file mới/sửa đổi
trong `Controllers/` và compile lại bằng Roslyn ngay lập tức (kể cả khi app đang
chạy). Nếu muốn ép reload thủ công: `POST /api/controllers/reload`.

## Bước 5 — Kiểm tra qua API

| Method | Endpoint | Ý nghĩa |
|---|---|---|
| GET | `/api/controllers/{code}/filter` | Danh sách field lọc + default value đã tính |
| GET | `/api/controllers/{code}/view` | Bố cục màn hình Filter |
| GET | `/api/controllers/{code}/grid` | Danh sách cột hiển thị |
| POST | `/api/controllers/{code}/execute` | Chạy report, body `{ "parameters": { "FromDate": "...", ... } }` |
| GET | `/api/controllers/errors` | Danh sách report đang lỗi compile/hot-reload |
| POST | `/api/controllers/reload` | Ép compile lại toàn bộ `Controllers/` |

Ví dụ với `curl`:

```bash
curl http://localhost:PORT/api/controllers/RPT_XXX/filter
curl -X POST http://localhost:PORT/api/controllers/RPT_XXX/execute \
  -H "Content-Type: application/json" \
  -d '{"parameters":{"FromDate":"2026-01-01","ToDate":"2026-01-31"}}'
```

**Luôn kiểm tra `GET /api/controllers/errors` trước** nếu `/filter` hay `/execute`
trả `409 Conflict` — lỗi compile sẽ hiện chi tiết ở đây thay vì âm thầm 404.

## Lỗi thường gặp

- **Tên class không khớp tên file** → Loader không map được `Code`, hoặc lỗi
  compile khi ghép 2 partial class Filter/Grid.
- **Quên `Required()`** cho field bắt buộc nhưng Processing lại giả định luôn có
  giá trị → nên thêm `OnChecking` validate hoặc dùng `Required()`.
- **Gọi `ctx.QueryScalar` cho query trả nhiều dòng/cột** → cố ý ném exception, xem
  lại câu SQL/proc thay vì bọc try-catch nuốt lỗi.
- **Field trong `view.Row(...)` không khớp tên field khai ở `ConfigureFilter`** →
  FE sẽ không tìm thấy field tương ứng để render.
- **Quên gọi `processing.TableId(...)` trước khi dùng `Sql(...)`** → ném lỗi rõ
  ràng ngay khi chạy, kể cả khi script chỉ có 1 câu lệnh vẫn phải `TableId(0)`.
- **Tham số trong `Sql(...)` không khớp tên field Filter** (VD field tên `FromDate`
  nhưng script viết `@fromdate` sai chính tả hoặc `@Date1` khác tên) → SQL Server
  báo lỗi thiếu tham số ngay khi chạy.

## Giới hạn hiện tại (chưa hỗ trợ)

- Chưa có phân quyền/`CurrentUser` thật — `BranchId` đang hard-code = 1 trong
  `DynamicControllersController`.
- Lookup field (`AsLookup`) mới có phần khai báo, FE/luồng load dữ liệu lookup
  thật chưa code.
- Chưa có khái niệm `Category`/tab để nhóm field khi Filter có nhiều field.
