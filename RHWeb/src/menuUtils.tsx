import { FileTextOutlined, FolderOutlined } from "@ant-design/icons";
import type { MenuProps } from "antd";
import type { MenuNodeDto } from "./types";

// Category (ReportCode null) không unique theo code -> dùng "cat-{id}" làm key
// riêng, tránh đụng với key report thật. Leaf dùng thẳng ReportCode làm key vì
// đó chính là giá trị cần khi chọn report.
export function categoryKey(id: number): string {
  return `cat-${id}`;
}

export function buildMenuItems(nodes: MenuNodeDto[]): MenuProps["items"] {
  return nodes.map((node) => {
    if (node.reportCode) {
      return {
        key: node.reportCode,
        label: node.label,
        icon: <FileTextOutlined />,
      };
    }

    return {
      key: categoryKey(node.id),
      label: node.label,
      icon: <FolderOutlined />,
      children: buildMenuItems(node.children),
    };
  });
}

export function findFirstReportCode(nodes: MenuNodeDto[]): string | null {
  for (const node of nodes) {
    if (node.reportCode) return node.reportCode;
    const found = findFirstReportCode(node.children);
    if (found) return found;
  }
  return null;
}

export function buildLabelByCode(nodes: MenuNodeDto[]): Map<string, string> {
  const map = new Map<string, string>();
  const walk = (list: MenuNodeDto[]) => {
    for (const node of list) {
      if (node.reportCode) map.set(node.reportCode, node.label);
      walk(node.children);
    }
  };
  walk(nodes);
  return map;
}

// Category cha của mọi node lá cần mở sẵn khi mount, để cây không hiện thu gọn
// hoàn toàn lúc đầu (giống ảnh mẫu FBO - luôn thấy trước ít nhất 1 nhánh mở).
export function collectAllCategoryKeys(nodes: MenuNodeDto[]): string[] {
  const keys: string[] = [];
  const walk = (list: MenuNodeDto[]) => {
    for (const node of list) {
      if (!node.reportCode) {
        keys.push(categoryKey(node.id));
        walk(node.children);
      }
    }
  };
  walk(nodes);
  return keys;
}
