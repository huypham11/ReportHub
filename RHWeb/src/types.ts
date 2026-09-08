export type FieldType = "Date" | "Int" | "String" | "Number";
export type GridAlign = "Left" | "Center" | "Right";

export interface MenuNodeDto {
  id: number;
  label: string;
  reportCode: string | null;
  children: MenuNodeDto[];
}

export interface FilterFieldDto {
  field: string;
  label: string;
  type: FieldType;
  isRequired: boolean;
  lookupSource: string | null;
  defaultValue: unknown;
}

export interface GridColumnDto {
  field: string;
  label: string;
  type: FieldType;
  format: string | null;
  width: number | null;
  align: GridAlign | null;
}

export interface BannerDto {
  // filterTitle khai ở ConfigureFilter, gridTitle/subtitleTemplate khai ở
  // ConfigureGrid - 2 title độc lập nhau, không dùng chung.
  filterTitle: string | null;
  gridTitle: string | null;
  subtitleTemplate: string | null;
}

export interface ViewRowItemDto {
  field: string;
  span: number;
}

export type ViewDto = ViewRowItemDto[][];

export interface FieldError {
  field: string;
  message: string;
}

export interface ExecuteResult {
  rows: Record<string, unknown>[];
}
