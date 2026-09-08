namespace RHDomain.Controllers;

public enum FieldType { Date, Int, String, Number }

public enum GridAlign { Left, Center, Right }

public class FilterFieldDef
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public FieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? LookupSource { get; set; }

    // Bất đồng bộ để cho phép default value lấy giá trị hệ thống từ SQL Server (ctx.CallProcedure),
    // không chỉ tính toán thuần C#.
    public Func<ExecutionContext, Task<object?>>? DefaultValueFactory { get; set; }
}

public class GridFieldDef
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public FieldType Type { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public GridAlign? Align { get; set; }
}

public class FilterFieldBuilder
{
    private readonly FilterFieldDef _def;
    public FilterFieldBuilder(FilterFieldDef def) => _def = def;

    public FilterFieldBuilder Required()
    {
        _def.IsRequired = true;
        return this;
    }

    public FilterFieldBuilder AsLookup(string sourceCode)
    {
        _def.LookupSource = sourceCode;
        return this;
    }

    // Default value tính thuần C# (không chạm DB), VD: new DateTime(...), ctx.CurrentUser.BranchId.
    public FilterFieldBuilder DefaultValue(Func<ExecutionContext, object?> factory)
    {
        _def.DefaultValueFactory = ctx => Task.FromResult(factory(ctx));
        return this;
    }

    // Default value cần lấy giá trị hệ thống từ SQL Server - dùng ctx.CallProcedure/ctx.QuerySql bên trong.
    public FilterFieldBuilder DefaultValue(Func<ExecutionContext, Task<object?>> factory)
    {
        _def.DefaultValueFactory = factory;
        return this;
    }
}

public class GridFieldBuilder
{
    private readonly GridFieldDef _def;
    public GridFieldBuilder(GridFieldDef def) => _def = def;

    public GridFieldBuilder Format(string? format)
    {
        _def.Format = format;
        return this;
    }

    // Độ rộng cột tính bằng px. Không gọi -> FE tự set độ rộng mặc định.
    public GridFieldBuilder Width(int px)
    {
        _def.Width = px;
        return this;
    }

    // Căn lề nội dung cột. Không gọi -> FE tự căn phải cho Number/Int, căn trái cho Date/String.
    public GridFieldBuilder Align(GridAlign align)
    {
        _def.Align = align;
        return this;
    }
}

/// <summary>
/// Nơi khai báo field - dùng chung cho cả ConfigureFilter và ConfigureGrid, đúng tinh thần
/// FBO: <field> là thẻ dùng chung cho cả Filter XML lẫn Grid XML, không phải khái niệm riêng
/// của "Controller" (khái niệm đó đã bỏ từ lâu, đặt tên theo Controller không còn hợp lý).
/// </summary>
public class FieldBuilder
{
    public List<FilterFieldDef> Filters { get; } = new();
    public List<GridFieldDef> Grids { get; } = new();

    // 2 property dưới đây KHÔNG khai qua FieldBuilder - chỉ là chỗ ControllerLoader gom
    // kết quả từ ProcessingBuilder (ConfigureProcessing) lại để CompiledController.Fields
    // giữ MỘT chỗ duy nhất cho ObjectExecutionService đọc. Muốn khai Processing/Checking,
    // xem ProcessingBuilder.cs + ControllerDefinitionBase.ConfigureProcessing.
    public List<Func<ExecutionContext, Task>> CheckingHooks { get; } = new();
    public Func<ExecutionContext, Task>? ProcessingHook { get; private set; }

    public void SetProcessingHook(Func<ExecutionContext, Task>? hook) => ProcessingHook = hook;

    // Template tự do cho dòng subheader (tóm tắt điều kiện lọc) ở màn Grid - trộn
    // text thường với placeholder "{TenFieldFilter}" (giống cú pháp string
    // interpolation của C#, VD $"Từ ngày {FromDate}"). null (không gọi Subtitle
    // ở ConfigureGrid) = FE tự dùng template mặc định "Label1 value1 · Label2 value2...".
    // Khai ở ConfigureGrid (không phải ConfigureFilter) vì đây là chuyện hiển thị
    // của màn kết quả, không phải input.
    public string? SubtitleTemplate { get; private set; }

    public void Subtitle(string template) => SubtitleTemplate = template;

    public void SetSubtitleTemplate(string? template) => SubtitleTemplate = template;

    // Tiêu đề của CHÍNH builder này - dùng chung 1 method .Title(...) cho cả
    // ConfigureFilter lẫn ConfigureGrid, nhưng Ý NGHĨA KHÁC NHAU tuỳ gọi ở đâu:
    //   - Gọi trong ConfigureFilter -> tiêu đề modal điều kiện lọc.
    //   - Gọi trong ConfigureGrid   -> tiêu đề banner màn kết quả.
    // ControllerLoader đọc riêng TitleOverride của filterBuilder/gridBuilder rồi
    // merge thành FilterTitle/GridTitle bên dưới - 2 field Filter/Grid không được
    // lẫn title của nhau.
    public string? TitleOverride { get; private set; }

    public void Title(string title) => TitleOverride = title;

    // Tiêu đề modal điều kiện lọc - null (ConfigureFilter không gọi Title) = FE tự
    // dùng label khai trong menu (dbo.RH_MenuItem).
    public string? FilterTitle { get; private set; }

    public void SetFilterTitle(string? title) => FilterTitle = title;

    // Tiêu đề banner màn Grid - null (ConfigureGrid không gọi Title) = FE tự dùng
    // label khai trong menu.
    public string? GridTitle { get; private set; }

    public void SetGridTitle(string? title) => GridTitle = title;

    public FilterFieldBuilder Filter(string name, string label, FieldType type)
    {
        var def = new FilterFieldDef { Field = name, Label = label, Type = type };
        Filters.Add(def);
        return new FilterFieldBuilder(def);
    }

    public GridFieldBuilder Grid(string name, string label, FieldType type)
    {
        var def = new GridFieldDef { Field = name, Label = label, Type = type };
        Grids.Add(def);
        return new GridFieldBuilder(def);
    }
}
