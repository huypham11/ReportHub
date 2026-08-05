namespace RHDomain.Controllers;

/// <summary>
/// Đánh dấu 1 tham số của Process(...) là "lấy giá trị từ Filter theo tên field" -
/// phân biệt với giá trị literal viết thẳng (mọi thứ không phải FieldRef đều là literal,
/// kể cả string, vì không còn dùng string trần để ám chỉ tên field nữa).
/// </summary>
public sealed class FieldRef
{
    public string Name { get; }
    public FieldRef(string name) => Name = name;
}

public static class P
{
    public static FieldRef Field(string fieldName) => new(fieldName);

    // Chỉ để đọc code cho rõ nghĩa (đối xứng với P.Field) - về bản chất trả nguyên giá trị.
    public static object? Value(object? value) => value;
}
