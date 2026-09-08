import { useEffect, useMemo, useState } from "react";
import { Alert, Button, Card, Layout, Menu, Typography } from "antd";
import { InfoCircleOutlined, ReloadOutlined, SearchOutlined } from "@ant-design/icons";
import {
  ApiConflictError,
  ApiValidationError,
  execute,
  getBanner,
  getFilter,
  getGrid,
  getMenu,
  getView,
} from "./api/client";
import { FilterForm } from "./components/FilterForm";
import { FilterModal } from "./components/FilterModal";
import { ResizableWidthBox } from "./components/ResizableWidthBox";
import { ResultGrid } from "./components/ResultGrid";
import { buildFilterSummary, renderSubtitleTemplate } from "./filterSummary";
import {
  buildLabelByCode,
  buildMenuItems,
  collectAllCategoryKeys,
  findFirstReportCode,
} from "./menuUtils";
import type {
  BannerDto,
  FieldError,
  FilterFieldDto,
  GridColumnDto,
  MenuNodeDto,
  ViewDto,
} from "./types";
import "./App.css";

const { Header, Sider, Content } = Layout;
const { Title } = Typography;

const REPORT_QUERY_KEY = "report";

function getReportCodeFromUrl(): string | null {
  return new URLSearchParams(window.location.search).get(REPORT_QUERY_KEY);
}

// pushState (không phải replaceState) để nút Back/Forward của trình duyệt đi
// qua lại được giữa các report đã xem, giống 1 "trang" thật sự.
function setReportCodeInUrl(code: string) {
  const url = new URL(window.location.href);
  if (url.searchParams.get(REPORT_QUERY_KEY) === code) return;
  url.searchParams.set(REPORT_QUERY_KEY, code);
  window.history.pushState({}, "", url);
}

export default function App() {
  const [menuTree, setMenuTree] = useState<MenuNodeDto[]>([]);
  const [openKeys, setOpenKeys] = useState<string[]>([]);
  const [selectedCode, setSelectedCode] = useState<string>("");

  // "filter": đang nhập điều kiện lọc. "grid": đã ấn Nhận, đang xem kết quả -
  // 2 màn hình tách biệt, không hiện chung 1 lúc (giống Filter/Grid của FBO).
  const [screen, setScreen] = useState<"filter" | "grid">("filter");

  const [fields, setFields] = useState<FilterFieldDto[]>([]);
  const [view, setView] = useState<ViewDto>([]);
  const [columns, setColumns] = useState<GridColumnDto[]>([]);
  const [values, setValues] = useState<Record<string, unknown>>({});
  // filterTitle khai ở Filter.cs, gridTitle/subtitleTemplate khai ở Grid.cs - 2
  // title độc lập nhau. null = FE tự dùng giá trị mặc định (label menu / template
  // liệt kê field Filter).
  const [banner, setBanner] = useState<BannerDto>({
    filterTitle: null,
    gridTitle: null,
    subtitleTemplate: null,
  });

  // Đã chạy report thành công ít nhất 1 lần cho lựa chọn hiện tại chưa - dùng để
  // quyết định có hiện Grid phía sau modal Filter hay không (đổi report khác thì
  // reset về false, phải "Nhận" lại mới có Grid).
  const [hasResult, setHasResult] = useState(false);
  const [rows, setRows] = useState<Record<string, unknown>[]>([]);
  const [errors, setErrors] = useState<FieldError[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const menuItems = useMemo(() => buildMenuItems(menuTree), [menuTree]);
  const labelByCode = useMemo(() => buildLabelByCode(menuTree), [menuTree]);
  const selectedLabel = labelByCode.get(selectedCode) ?? "";
  // field.Title(...) khai riêng ở Filter.cs (modal) và Grid.cs (banner) - đều
  // fallback về label lấy từ menu nếu report không khai.
  const modalTitle = banner.filterTitle ?? selectedLabel;
  const bannerTitle = banner.gridTitle ?? selectedLabel;
  const filterSummary = useMemo(
    () =>
      banner.subtitleTemplate
        ? renderSubtitleTemplate(banner.subtitleTemplate, fields, values)
        : buildFilterSummary(fields, values),
    [banner.subtitleTemplate, fields, values]
  );

  useEffect(() => {
    getMenu()
      .then((tree) => {
        setMenuTree(tree);
        setOpenKeys(collectAllCategoryKeys(tree));

        // F5/mở lại link phải quay về đúng report đang xem (đọc từ URL), không
        // phải luôn nhảy về report đầu tiên trong menu.
        const codeFromUrl = getReportCodeFromUrl();
        const isValidCode = codeFromUrl && buildLabelByCode(tree).has(codeFromUrl);
        const initialCode = isValidCode ? codeFromUrl : findFirstReportCode(tree);
        if (initialCode) setSelectedCode(initialCode);
      })
      .catch((e) => setLoadError(String(e)));
  }, []);

  // Nút Back/Forward của trình duyệt đi qua lại giữa các report đã xem.
  useEffect(() => {
    function onPopState() {
      const codeFromUrl = getReportCodeFromUrl();
      if (codeFromUrl && codeFromUrl !== selectedCode) setSelectedCode(codeFromUrl);
    }
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, [selectedCode]);

  useEffect(() => {
    if (!selectedCode) return;

    setReportCodeInUrl(selectedCode);
    setLoadError(null);
    setErrors([]);
    setRows([]);
    setHasResult(false);
    setScreen("filter");

    Promise.all([
      getFilter(selectedCode),
      getView(selectedCode),
      getGrid(selectedCode),
      getBanner(selectedCode),
    ])
      .then(([f, v, g, b]) => {
        setFields(f);
        setView(v);
        setColumns(g);
        setBanner(b);
        const initial: Record<string, unknown> = {};
        for (const field of f) initial[field.field] = field.defaultValue ?? null;
        setValues(initial);
      })
      .catch((e) => {
        if (e instanceof ApiConflictError) {
          setLoadError(`Report '${e.code}' đang lỗi compile: ${e.detail}`);
        } else {
          setLoadError(String(e));
        }
      });
  }, [selectedCode]);

  async function handleSubmit() {
    setLoading(true);
    setErrors([]);
    try {
      const result = await execute(selectedCode, values);
      setRows(result.rows);
      setHasResult(true);
      setScreen("grid");
    } catch (e) {
      if (e instanceof ApiValidationError) {
        setErrors(e.errors);
      } else if (e instanceof ApiConflictError) {
        setLoadError(`Report '${e.code}' đang lỗi compile: ${e.detail}`);
      } else {
        setLoadError(String(e));
      }
    } finally {
      setLoading(false);
    }
  }

  // Load lại data với đúng điều kiện lọc lần chạy gần nhất (không mở modal) -
  // lỗi validate lúc này rất khó xảy ra (giá trị đã hợp lệ từ lần Nhận trước),
  // nhưng nếu có thì mở modal ra để người dùng thấy lỗi thay vì im lặng.
  async function handleRefresh() {
    setRefreshing(true);
    try {
      const result = await execute(selectedCode, values);
      setRows(result.rows);
    } catch (e) {
      if (e instanceof ApiValidationError) {
        setErrors(e.errors);
        setScreen("filter");
      } else if (e instanceof ApiConflictError) {
        setLoadError(`Report '${e.code}' đang lỗi compile: ${e.detail}`);
      } else {
        setLoadError(String(e));
      }
    } finally {
      setRefreshing(false);
    }
  }

  return (
    <Layout className="app-layout">
      <Header className="app-header">
        <Title level={4} className="app-title">
          ReportHub
        </Title>
      </Header>

      <Layout className="app-body">
        <Sider width={260} className="app-sider" theme="dark">
          <Menu
            mode="inline"
            theme="dark"
            className="nav-menu"
            items={menuItems}
            selectedKeys={selectedCode ? [selectedCode] : []}
            openKeys={openKeys}
            onOpenChange={setOpenKeys}
            onClick={({ key }) => {
              if (key.startsWith("cat-")) return; // category chỉ để mở/đóng nhánh
              // Click lại đúng report đang chọn (VD sau khi bấm Hủy về màn trống)
              // không làm đổi selectedCode -> effect load Filter sẽ không tự chạy
              // lại, phải tự mở modal ở đây để không bị kẹt màn trống.
              if (key === selectedCode) setScreen("filter");
              else setSelectedCode(key);
            }}
          />
        </Sider>

        <Content className="app-content">
          {loadError && (
            <Alert
              type="error"
              message={loadError}
              showIcon
              className="load-error"
            />
          )}

          {screen === "filter" && fields.length > 0 && (
            <FilterModal
              title={modalTitle || "Điều kiện lọc"}
              onSubmit={handleSubmit}
              submitLoading={loading}
              // Hủy không cần có Grid phía sau - đóng modal, màn trống cũng được.
              onCancel={() => setScreen("grid")}
            >
              <FilterForm
                fields={fields}
                view={view}
                values={values}
                errors={errors}
                onChange={(field, value) =>
                  setValues((prev) => ({ ...prev, [field]: value }))
                }
                onSubmit={handleSubmit}
              />
            </FilterModal>
          )}

          {hasResult && (
            <ResizableWidthBox>
              <Card
                className="panel panel-fill"
                styles={{
                  body: {
                    padding: 0,
                    flex: 1,
                    minHeight: 0,
                    display: "flex",
                    flexDirection: "column",
                  },
                }}
              >
                <div className="report-banner">
                  <div className="report-banner-text">
                    <div className="report-title">{bannerTitle}</div>
                    {filterSummary && (
                      <div className="report-subtitle">
                        <InfoCircleOutlined /> {filterSummary}
                      </div>
                    )}
                  </div>
                </div>
                <div className="report-body">
                  <ResultGrid
                    columns={columns}
                    rows={rows}
                    loading={refreshing}
                    leftToolbar={
                      <>
                        <Button icon={<SearchOutlined />} onClick={() => setScreen("filter")}>
                          Tìm
                        </Button>
                        <Button
                          icon={<ReloadOutlined />}
                          loading={refreshing}
                          onClick={handleRefresh}
                        >
                          Làm mới
                        </Button>
                      </>
                    }
                  />
                </div>
              </Card>
            </ResizableWidthBox>
          )}
        </Content>
      </Layout>
    </Layout>
  );
}
