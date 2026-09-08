import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ConfigProvider, theme as antdTheme } from 'antd'
import viVN from 'antd/locale/vi_VN'
import './index.css'
import App from './App.tsx'
import { BRAND_COLOR, BORDER_RADIUS, DARK_BG, DARK_SURFACE, DARK_BORDER } from './theme'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider
      locale={viVN}
      theme={{
        algorithm: antdTheme.darkAlgorithm,
        token: {
          colorPrimary: BRAND_COLOR,
          borderRadius: BORDER_RADIUS,
          fontFamily: 'system-ui, "Segoe UI", Roboto, sans-serif',
          colorBgContainer: DARK_SURFACE,
          colorBgLayout: DARK_BG,
          colorBgElevated: DARK_SURFACE,
          colorBorder: DARK_BORDER,
        },
      }}
    >
      <App />
    </ConfigProvider>
  </StrictMode>,
)
