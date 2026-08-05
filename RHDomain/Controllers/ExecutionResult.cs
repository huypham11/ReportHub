namespace RHDomain.Controllers;

public class ExecutionResult
{
    public bool Found { get; set; } = true;
    public bool Success { get; set; }
    public List<(string Field, string Message)> Errors { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public static ExecutionResult NotFound() => new() { Found = false, Success = false };

    public static ExecutionResult ValidationFailed(List<(string Field, string Message)> errors)
        => new() { Success = false, Errors = errors };

    public static ExecutionResult Ok(List<Dictionary<string, object?>> rows)
        => new() { Success = true, Rows = rows };
}
