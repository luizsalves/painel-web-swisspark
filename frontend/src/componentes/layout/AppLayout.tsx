import { useState } from "react";
import type { ReactNode } from "react";
import AppSidebar from "./AppSidebar";

interface AppLayoutProps {
  children: ReactNode;
}

const SIDEBAR_BRAND_THEME_KEY = "sp-sidebar-brand-theme";

export default function AppLayout({ children }: AppLayoutProps) {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [isBrandLight, setIsBrandLight] = useState(() => {
    try {
      return window.localStorage.getItem(SIDEBAR_BRAND_THEME_KEY) !== "green";
    } catch {
      return true;
    }
  });

  const handleToggleBrandTheme = () => {
    setIsBrandLight((currentValue) => {
      const nextValue = !currentValue;

      try {
        window.localStorage.setItem(
          SIDEBAR_BRAND_THEME_KEY,
          nextValue ? "light" : "green"
        );
      } catch {
        // O tema continua funcionando durante a sessão mesmo sem armazenamento local.
      }

      return nextValue;
    });
  };

  return (
    <div className="sp-app-shell">
      <AppSidebar
        isCollapsed={isSidebarCollapsed}
        isBrandLight={isBrandLight}
        onToggleCollapse={() => setIsSidebarCollapsed((prev) => !prev)}
        onToggleBrandTheme={handleToggleBrandTheme}
      />
      <div className="sp-app-shell__content">{children}</div>
    </div>
  );
}

