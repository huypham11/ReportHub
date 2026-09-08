import { useMemo } from "react";
import { Col, DatePicker, Form, Input, InputNumber, Row } from "antd";
import dayjs from "dayjs";
import type { FieldError, FilterFieldDto, ViewDto } from "../types";

interface Props {
  fields: FilterFieldDto[];
  view: ViewDto;
  values: Record<string, unknown>;
  errors: FieldError[];
  onChange: (field: string, value: unknown) => void;
  onSubmit: () => void;
}

// Không khai ConfigureView -> mỗi field full-width, xuống hàng riêng theo đúng
// thứ tự khai báo ở ConfigureFilter (theo docs/how-to-create-report.md).
function buildRows(fields: FilterFieldDto[], view: ViewDto) {
  if (view.length === 0) {
    return fields.map((f) => [{ field: f.field, span: 12 }]);
  }

  const mentioned = new Set(view.flat().map((item) => item.field));
  const leftover = fields
    .filter((f) => !mentioned.has(f.field))
    .map((f) => [{ field: f.field, span: 12 }]);

  return [...view, ...leftover];
}

function toDateValue(value: unknown) {
  // Backend trả ISO không có "Z" (giờ local) - cắt thẳng 10 ký tự đầu, KHÔNG
  // parse nguyên chuỗi qua dayjs với timezone vì sẽ lệch ngày.
  if (typeof value !== "string" || !value) return null;
  return dayjs(value.slice(0, 10), "YYYY-MM-DD");
}

export function FilterForm({
  fields,
  view,
  values,
  errors,
  onChange,
  onSubmit,
}: Props) {
  const rows = useMemo(() => buildRows(fields, view), [fields, view]);
  const fieldByName = useMemo(
    () => new Map(fields.map((f) => [f.field, f])),
    [fields]
  );
  const errorByField = useMemo(
    () => new Map(errors.map((e) => [e.field, e.message])),
    [errors]
  );

  return (
    <Form
      layout="vertical"
      onFinish={onSubmit}
      className="filter-form"
    >
      {rows.map((row, i) => (
        <Row gutter={16} key={i}>
          {row.map((item) => {
            const f = fieldByName.get(item.field);
            if (!f) return null;
            const error = errorByField.get(f.field);
            // Thang 12 cột của backend -> thang 24 cột của antd Row/Col.
            const span = Math.min(24, item.span * 2);

            return (
              <Col span={span} key={f.field}>
                <Form.Item
                  label={f.label}
                  required={f.isRequired}
                  validateStatus={error ? "error" : ""}
                  help={error}
                >
                  {f.type === "Date" ? (
                    <DatePicker
                      style={{ width: "100%" }}
                      format="DD/MM/YYYY"
                      value={toDateValue(values[f.field])}
                      onChange={(d) =>
                        onChange(f.field, d ? d.format("YYYY-MM-DD") : null)
                      }
                    />
                  ) : f.type === "Int" || f.type === "Number" ? (
                    <InputNumber
                      style={{ width: "100%" }}
                      value={values[f.field] as number | null}
                      onChange={(v) => onChange(f.field, v)}
                    />
                  ) : (
                    <Input
                      value={(values[f.field] as string) ?? ""}
                      onChange={(e) => onChange(f.field, e.target.value || null)}
                    />
                  )}
                </Form.Item>
              </Col>
            );
          })}
        </Row>
      ))}
    </Form>
  );
}
