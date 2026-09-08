import type { FilterFieldDto } from "./types";

function formatFilterValue(f: FilterFieldDto, value: unknown): string {
  if (value === null || value === undefined || value === "") return "";

  if (f.type === "Date" && typeof value === "string") {
    // Giá trị lưu dạng "yyyy-MM-dd..." - cắt thẳng chuỗi, không qua Date để
    // tránh lệch ngày theo timezone (xem FilterForm.toDateValue).
    const m = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
    return m ? `${m[3]}/${m[2]}/${m[1]}` : value;
  }

  if (f.type === "Number" || f.type === "Int") {
    const n = Number(value);
    return Number.isNaN(n) ? String(value) : n.toLocaleString("vi-VN");
  }

  return String(value);
}

// Template mặc định khi report không khai field.Subtitle(...) ở ConfigureGrid -
// ghép "Label value" của mọi field Filter đã nhập, giữ nguyên hành vi cũ.
export function buildFilterSummary(
  fields: FilterFieldDto[],
  values: Record<string, unknown>
): string {
  return fields
    .map((f) => {
      const formatted = formatFilterValue(f, values[f.field]);
      return formatted ? `${f.label} ${formatted}` : null;
    })
    .filter((part): part is string => part !== null)
    .join("  ·  ");
}

// Template tự do khai ở Grid.cs qua field.Subtitle("Từ ngày {FromDate} ... linh tinh") -
// placeholder "{TenFieldFilter}" được thay bằng giá trị đã nhập (format theo đúng
// FieldType), giống cú pháp string interpolation của C# ($"...{Bien}...") nên quen
// tay với người viết report. Field không tồn tại/chưa nhập -> thay bằng chuỗi rỗng,
// phần text xung quanh vẫn giữ nguyên.
export function renderSubtitleTemplate(
  template: string,
  fields: FilterFieldDto[],
  values: Record<string, unknown>
): string {
  const fieldByName = new Map(fields.map((f) => [f.field, f]));

  return template.replace(/\{(\w+)\}/g, (match, name: string) => {
    const f = fieldByName.get(name);
    if (!f) return match; // tên sai chính tả -> giữ nguyên placeholder cho dễ phát hiện
    return formatFilterValue(f, values[f.field]);
  });
}
