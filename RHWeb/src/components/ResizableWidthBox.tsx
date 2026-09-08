import { useRef, useState, type ReactNode } from "react";

interface Props {
  children: ReactNode;
  minWidth?: number;
  maxWidth?: number;
}

// Bọc quanh khung bảng (Card chứa ResultGrid) để cho phép kéo cạnh phải chỉnh
// độ rộng bằng chuột - mặc định full-width (100%) như trước, chỉ chuyển sang
// độ rộng cố định (px) khi người dùng bắt đầu kéo.
export function ResizableWidthBox({ children, minWidth = 480, maxWidth = 1600 }: Props) {
  const [width, setWidth] = useState<number | "100%">("100%");
  const boxRef = useRef<HTMLDivElement>(null);

  function startResize(e: React.MouseEvent) {
    e.preventDefault();
    const startX = e.clientX;
    const startWidth = boxRef.current?.getBoundingClientRect().width ?? minWidth;

    function onMouseMove(ev: MouseEvent) {
      const next = Math.min(maxWidth, Math.max(minWidth, startWidth + (ev.clientX - startX)));
      setWidth(next);
    }
    function onMouseUp() {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    }
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);
  }

  return (
    <div
      ref={boxRef}
      className="resizable-box"
      style={{
        // "flex" luôn giữ nguyên để box này vẫn kéo dài hết chiều cao trong
        // app-content (flex-column) - CHỈ width/alignSelf mới điều khiển độ
        // rộng, không được gộp chung vào "flex" (nhầm trục sẽ mất full-height).
        flex: "1 1 auto",
        width,
        alignSelf: width === "100%" ? "stretch" : "flex-start",
      }}
    >
      {children}
      <div
        className="resize-handle"
        onMouseDown={startResize}
        title="Kéo để đổi độ rộng"
      />
    </div>
  );
}
