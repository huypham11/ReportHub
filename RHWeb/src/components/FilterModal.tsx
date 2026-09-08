import { useRef, useState, type ReactNode } from "react";
import { Button } from "antd";

interface Props {
  title: string;
  children: ReactNode;
  onSubmit: () => void;
  submitLoading: boolean;
  onCancel: () => void;
}

const DEFAULT_WIDTH = 720;
const DEFAULT_HEIGHT = 460;
const MIN_WIDTH = 420;
const MIN_HEIGHT = 260;

// Modal tự dựng (không dùng antd Modal) vì cần 2 thứ antd Modal không hỗ trợ
// sẵn: kéo cạnh để đổi kích thước, và double-click viền để phóng to/thu nhỏ.
export function FilterModal({ title, children, onSubmit, submitLoading, onCancel }: Props) {
  const [maximized, setMaximized] = useState(false);
  const [size, setSize] = useState({ width: DEFAULT_WIDTH, height: DEFAULT_HEIGHT });
  const boxRef = useRef<HTMLDivElement>(null);

  function toggleMaximize() {
    setMaximized((m) => !m);
  }

  function startResize(axis: "x" | "y" | "both") {
    return (e: React.MouseEvent) => {
      if (maximized) return;
      e.preventDefault();
      const startX = e.clientX;
      const startY = e.clientY;
      const startSize = boxRef.current?.getBoundingClientRect() ?? size;

      function onMouseMove(ev: MouseEvent) {
        setSize((prev) => {
          const next = { ...prev };
          if (axis === "x" || axis === "both") {
            next.width = Math.max(
              MIN_WIDTH,
              Math.min(window.innerWidth - 40, startSize.width + (ev.clientX - startX))
            );
          }
          if (axis === "y" || axis === "both") {
            next.height = Math.max(
              MIN_HEIGHT,
              Math.min(window.innerHeight - 40, startSize.height + (ev.clientY - startY))
            );
          }
          return next;
        });
      }
      function onMouseUp() {
        window.removeEventListener("mousemove", onMouseMove);
        window.removeEventListener("mouseup", onMouseUp);
      }
      window.addEventListener("mousemove", onMouseMove);
      window.addEventListener("mouseup", onMouseUp);
    };
  }

  return (
    <div className="filter-modal-overlay">
      <div
        ref={boxRef}
        className={`filter-modal-box${maximized ? " maximized" : ""}`}
        style={maximized ? undefined : { width: size.width, height: size.height }}
      >
        <div className="filter-modal-header" onDoubleClick={toggleMaximize}>
          <span className="filter-modal-title">{title}</span>
        </div>

        <div className="filter-modal-body">{children}</div>

        <div className="filter-modal-footer">
          <Button onClick={onCancel}>Hủy</Button>
          <Button type="primary" loading={submitLoading} onClick={onSubmit}>
            Nhận
          </Button>
        </div>

        {!maximized && (
          <>
            <div
              className="modal-resize-edge modal-resize-right"
              onMouseDown={startResize("x")}
              onDoubleClick={toggleMaximize}
            />
            <div
              className="modal-resize-edge modal-resize-bottom"
              onMouseDown={startResize("y")}
              onDoubleClick={toggleMaximize}
            />
            <div
              className="modal-resize-edge modal-resize-corner"
              onMouseDown={startResize("both")}
              onDoubleClick={toggleMaximize}
            />
          </>
        )}
      </div>
    </div>
  );
}
