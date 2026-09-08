import { themeQuartz } from "ag-grid-community";
import { BRAND_COLOR, BORDER_RADIUS, DARK_SURFACE, DARK_BORDER, DARK_TEXT } from "./theme";

// Dùng chung màu với antd (main.tsx ConfigProvider dark algorithm) để bảng kết
// quả và phần còn lại của trang (form, nút, dropdown) cùng 1 tông tối.
export const myTheme = themeQuartz.withParams({
  accentColor: BRAND_COLOR,
  backgroundColor: DARK_SURFACE,
  borderColor: DARK_BORDER,
  browserColorScheme: "dark",
  columnBorder: true,
  foregroundColor: DARK_TEXT,
  headerBackgroundColor: "#171b26",
  headerTextColor: DARK_TEXT,
  headerFontWeight: 600,
  fontFamily: "system-ui, sans-serif",
  fontSize: 13,
  headerFontSize: 13,
  wrapperBorderRadius: BORDER_RADIUS,
  spacing: 3,
});
