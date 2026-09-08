import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type ColDef,
  type GridApi,
  type GridReadyEvent,
  type PaginationChangedEvent,
  type ValueFormatterParams,
} from "ag-grid-community";
import { Pagination } from "antd";
import { myTheme } from "../agGridTheme";
import type { GridColumnDto } from "../types";

ModuleRegistry.registerModules([AllCommunityModule]);

interface Props {
  columns: GridColumnDto[];
  rows: Record<string, unknown>[];
  // Nút/nội dung tuỳ ý đặt bên trái thanh phân trang (VD nút "Tìm", "Làm mới"
  // ở App.tsx) - ResultGrid không biết gì về modal Filter, chỉ chừa chỗ hiển thị.
  leftToolbar?: ReactNode;
  // Đang gọi lại /execute (bấm "Làm mới") - hiện overlay loading đè lên bảng,
  // vì đổi rowData quá nhanh (network local) nên tự thân không thấy gì thay đổi.
  loading?: boolean;
}

function formatDate(raw: string): string {
  // Backend trả ISO "yyyy-MM-dd..." không có "Z" - cắt thẳng chuỗi, KHÔNG qua
  // Date/toLocaleDateString vì sẽ lệch ngày theo timezone trình duyệt.
  const match = raw.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : raw;
}

function buildColDefs(columns: GridColumnDto[]): ColDef[] {
  return columns.map((c) => {
    const isNumeric = c.type === "Number" || c.type === "Int";
    // Không khai .Align(...) -> tự căn phải cho Number/Int, căn trái cho Date/String
    // (theo docs/how-to-create-report.md).
    const align = c.align ?? (isNumeric ? "Right" : "Left");
    const textAlign = align === "Right" ? "right" : align === "Center" ? "center" : "left";

    return {
      field: c.field,
      headerName: c.label,
      width: c.width ?? undefined,
      filter: isNumeric ? "agNumberColumnFilter" : c.type === "Date" ? "agDateColumnFilter" : true,
      cellStyle: { textAlign },
      // Header luôn căn giữa, không phụ thuộc Align của nội dung cell.
      headerClass: "header-align-center",
      valueFormatter: (p: ValueFormatterParams) => {
        if (p.value === null || p.value === undefined) return "";

        if (isNumeric) {
          const n = Number(p.value);
          if (Number.isNaN(n)) return String(p.value);
          return c.format === "#,##0" || c.format === "#,#0"
            ? Math.round(n).toLocaleString("vi-VN")
            : n.toLocaleString("vi-VN");
        }

        if (c.type === "Date" && typeof p.value === "string") return formatDate(p.value);

        return String(p.value);
      },
    };
  });
}

export function ResultGrid({ columns, rows, leftToolbar, loading }: Props) {
  const colDefs = useMemo(() => buildColDefs(columns), [columns]);
  const defaultColDef = useMemo<ColDef>(
    () => ({
      sortable: true,
      resizable: true,
      filter: true,
      // Ô nhập lọc riêng cho từng cột, hiện ngay dưới header (Community feature,
      // không cần mở menu cột mới lọc được).
      floatingFilter: true,
      minWidth: 120,
    }),
    []
  );

  const gridApiRef = useRef<GridApi | null>(null);
  // AG Grid Community chỉ vẽ sẵn thanh phân trang ở DƯỚI bảng (không có option
  // đẩy lên trên) - phải tắt thanh mặc định (suppressPaginationPanel) và tự
  // dựng bằng antd Pagination, điều khiển qua Grid API.
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [rowCount, setRowCount] = useState(0);

  const onGridReady = useCallback((e: GridReadyEvent) => {
    gridApiRef.current = e.api;
  }, []);

  const onPaginationChanged = useCallback((e: PaginationChangedEvent) => {
    setPage(e.api.paginationGetCurrentPage() + 1);
    setPageSize(e.api.paginationGetPageSize());
    setRowCount(e.api.paginationGetRowCount());
  }, []);

  if (columns.length === 0) {
    return <p className="hint">Report này không khai báo Grid hiển thị.</p>;
  }

  return (
    <div className="result-grid-wrap">
      <div className="grid-pagination-bar">
        <div className="grid-toolbar-left">{leftToolbar}</div>
        <Pagination
          size="small"
          current={page}
          pageSize={pageSize}
          total={rowCount}
          // antd mặc định cho search/gõ trong dropdown page size (showSearch: true
          // ở bên trong) - truyền object để tắt, chỉ cho click chọn.
          showSizeChanger={{ showSearch: false }}
          pageSizeOptions={[20, 50, 100]}
          showTotal={(total) => `Tổng ${total.toLocaleString("vi-VN")} bản ghi`}
          onChange={(nextPage, nextPageSize) => {
            const api = gridApiRef.current;
            if (!api) return;
            // AG Grid v33+ bỏ hẳn api.paginationSetPageSize() - phải đổi qua
            // updateGridOptions() để cập nhật gridOption paginationPageSize.
            if (nextPageSize !== pageSize) {
              api.updateGridOptions({ paginationPageSize: nextPageSize });
            }
            api.paginationGoToPage(nextPage - 1);
          }}
        />
      </div>

      <div className="ag-theme-reporthub result-grid-body">
        <AgGridReact
          theme={myTheme}
          loading={loading}
          rowData={rows}
          columnDefs={colDefs}
          defaultColDef={defaultColDef}
          // Click vào dòng để chọn, giữ hiệu ứng highlight (mặc định AG Grid v32+
          // không tự bật chọn khi click nếu không khai rõ enableClickSelection).
          rowSelection={{ mode: "singleRow", checkboxes: false, enableClickSelection: true }}
          pagination
          suppressPaginationPanel
          paginationPageSize={pageSize}
          onGridReady={onGridReady}
          onPaginationChanged={onPaginationChanged}
          animateRows
          overlayNoRowsTemplate="Không có dữ liệu."
          // overlay loading mặc định của AG Grid rỗng nếu không set template
          // (nền che vẫn bật nhưng không có nội dung nên nhìn như không đổi gì).
          overlayLoadingTemplate="<span class='ag-overlay-loading-center'>Đang tải...</span>"
        />
      </div>
    </div>
  );
}
