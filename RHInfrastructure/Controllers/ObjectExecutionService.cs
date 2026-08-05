using System.Text.Json;
using RHDomain.Controllers;

namespace RHInfrastructure.Controllers;

/// <summary>
/// Điều phối toàn bộ vòng đời execute 1 controller: validate -> Processing (gọi proc) ->
/// lọc cột theo Grid schema -> trả kết quả. Generic hoàn toàn, không biết report cụ thể nào -
/// mọi hành vi riêng của report nằm trong Filter.cs/Grid.cs, không đụng tới class này.
/// </summary>
public class ObjectExecutionService
{
    private readonly ControllerLoader _loader;
    private readonly DapperProcedureExecutor _executor;

    public ObjectExecutionService(ControllerLoader loader, DapperProcedureExecutor executor)
    {
        _loader = loader;
        _executor = executor;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        string code,
        Dictionary<string, object?> rawParameters,
        CurrentUserInfo currentUser)
    {
        var compiled = _loader.Get(code);
        if (compiled == null) return ExecutionResult.NotFound();

        var ctx = new RHDomain.Controllers.ExecutionContext
        {
            Parameters = rawParameters,
            CurrentUser = currentUser,
            ProcedureExecutor = _executor.ExecuteAsync,
            SqlExecutor = _executor.ExecuteSqlAsync
        };

        // 1. Áp default value cho field nào FE không gửi lên (có thể phải query SQL Server)
        foreach (var f in compiled.Fields.Filters)
        {
            if (!ctx.Parameters.ContainsKey(f.Field) && f.DefaultValueFactory != null)
                ctx.Parameters[f.Field] = await f.DefaultValueFactory(ctx);
        }

        // 2. Convert JsonElement (do System.Text.Json deserialize Dictionary<string, object?>)
        //    về đúng kiểu CLR theo FieldType đã khai báo ở Filter - làm 1 lần ở Core,
        //    Filter.cs không phải tự parse kiểu.
        foreach (var f in compiled.Fields.Filters)
        {
            if (ctx.Parameters.TryGetValue(f.Field, out var raw) && raw is JsonElement je)
                ctx.Parameters[f.Field] = ConvertJsonElement(je, f.Type);
        }

        // 3. Validate required
        foreach (var f in compiled.Fields.Filters.Where(f => f.IsRequired))
        {
            if (!ctx.Parameters.TryGetValue(f.Field, out var v) || v is null)
                ctx.AddError(f.Field, $"{f.Label} là bắt buộc");
        }

        // 4. Custom validate do report tự khai báo
        foreach (var hook in compiled.Fields.CheckingHooks)
            await hook(ctx);

        if (ctx.Errors.Count > 0)
            return ExecutionResult.ValidationFailed(ctx.Errors);

        // 5. Processing - nơi DUY NHẤT chạm DB, do Filter.cs khai báo
        if (compiled.Fields.ProcessingHook != null)
            await compiled.Fields.ProcessingHook(ctx);

        // 6. Lọc cột theo đúng Grid schema, tránh lộ field thừa không khai báo
        var gridFields = compiled.Fields.Grids.Select(g => g.Field).ToHashSet();
        var rows = gridFields.Count == 0
            ? ctx.Rows
            : ctx.Rows
                .Select(r => r.Where(kv => gridFields.Contains(kv.Key))
                              .ToDictionary(kv => kv.Key, kv => kv.Value))
                .ToList();

        return ExecutionResult.Ok(rows);
    }

    private static object? ConvertJsonElement(JsonElement je, FieldType type)
    {
        if (je.ValueKind == JsonValueKind.Null) return null;
        return type switch
        {
            FieldType.Date => je.GetDateTime(),
            FieldType.Int => je.GetInt32(),
            FieldType.Number => je.GetDecimal(),
            _ => je.GetString()
        };
    }
}
