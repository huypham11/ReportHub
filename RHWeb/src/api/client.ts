import type {
  BannerDto,
  ExecuteResult,
  FieldError,
  FilterFieldDto,
  GridColumnDto,
  MenuNodeDto,
  ViewDto,
} from "../types";

const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5123/api";

export class ApiConflictError extends Error {
  constructor(public code: string, public detail: string) {
    super(detail);
  }
}

export class ApiValidationError extends Error {
  constructor(public errors: FieldError[]) {
    super("Validation failed");
  }
}

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`);
  if (res.status === 409) {
    const body = await res.json();
    throw new ApiConflictError(body.code, body.error);
  }
  if (!res.ok) throw new Error(`GET ${path} thất bại: ${res.status}`);
  return res.json();
}

// Cây menu điều hướng - đọc từ bảng dbo.RH_MenuItem (xem sql/001_create_rh_menu_item.sql),
// đổi cấu trúc menu chỉ cần sửa data trong bảng, không cần build lại FE.
export function getMenu(): Promise<MenuNodeDto[]> {
  return getJson<MenuNodeDto[]>("/menu");
}

export function getFilter(code: string): Promise<FilterFieldDto[]> {
  return getJson<FilterFieldDto[]>(`/controllers/${code}/filter`);
}

export function getView(code: string): Promise<ViewDto> {
  return getJson<ViewDto>(`/controllers/${code}/view`);
}

export function getGrid(code: string): Promise<GridColumnDto[]> {
  return getJson<GridColumnDto[]>(`/controllers/${code}/grid`);
}

// Title/Subtitle tự khai ở ConfigureGrid (field.Title(...)/field.Subtitle(...)) -
// null nghĩa là report không khai, FE tự dùng giá trị mặc định.
export function getBanner(code: string): Promise<BannerDto> {
  return getJson<BannerDto>(`/controllers/${code}/banner`);
}

export async function execute(
  code: string,
  parameters: Record<string, unknown>
): Promise<ExecuteResult> {
  const res = await fetch(`${API_BASE}/controllers/${code}/execute`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ parameters }),
  });

  if (res.status === 409) {
    const body = await res.json();
    throw new ApiConflictError(body.code, body.error);
  }
  if (res.status === 400) {
    const body = await res.json();
    throw new ApiValidationError(body.errors);
  }
  if (!res.ok) throw new Error(`Execute thất bại: ${res.status}`);

  return res.json();
}
