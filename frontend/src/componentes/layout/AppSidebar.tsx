import {
  LayoutDashboard,
  CalendarCheck2,
  Building2,
  BookText,
  Users,
  BarChart3,
  AlertTriangle,
  Settings,
  ShieldCheck,
  PanelLeftClose,
  PanelLeftOpen,
  Moon,
  Sun,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { NavLink } from "react-router-dom";

interface NavItem {
  label: string;
  icon: LucideIcon;
  path?: string;
  disabled?: boolean;
}

// Itens sem "disabled" já têm página implementada; os demais aguardam a próxima etapa.
const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", icon: LayoutDashboard, path: "/dashboard" },
  { label: "Fechamentos", icon: CalendarCheck2, path: "/fechamentos" },
  { label: "Empresas", icon: Building2, path: "/empresas" },
  { label: "Contas Contábeis", icon: BookText, path: "/contas" },
  { label: "Usuários", icon: Users, disabled: true },
  { label: "Relatórios", icon: BarChart3, disabled: true },
  { label: "Pendências", icon: AlertTriangle, disabled: true },
  { label: "Configurações", icon: Settings, disabled: true },
  { label: "Auditoria", icon: ShieldCheck, disabled: true },
];

interface AppSidebarProps {
  isCollapsed: boolean;
  isBrandLight: boolean;
  onToggleCollapse: () => void;
  onToggleBrandTheme: () => void;
}

export default function AppSidebar({
  isCollapsed,
  isBrandLight,
  onToggleCollapse,
  onToggleBrandTheme,
}: AppSidebarProps) {
  return (
    <aside
      className={[
        "sp-sidebar",
        isCollapsed ? "sp-sidebar--collapsed" : "",
        isBrandLight ? "sp-sidebar--light" : "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div className="sp-sidebar__top">
        <div
          className={`sp-sidebar__brand ${
            isBrandLight ? "sp-sidebar__brand--light" : "sp-sidebar__brand--green"
          }`}
        >
          <img
            className={`sp-sidebar__brand-logo ${
              isBrandLight ? "" : "sp-sidebar__brand-logo--white"
            }`}
            src={
              isBrandLight
                ? "/branding/logo-swiss-park.png"
                : "/branding/logo_letra-branca.png"
            }
            alt="Swiss Park"
          />
          {!isCollapsed && (
            <span className="sp-sidebar__brand-text">
              PAINEL DE CONCILIAÇÃO
              <br />
              CONTÁBIL
            </span>
          )}
        </div>

        <nav className="sp-sidebar__nav" aria-label="Navegação principal">
          <ul>
            {NAV_ITEMS.map((item) => {
              const Icon = item.icon;
              const content = (
                <>
                  <Icon size={19} />
                  {!isCollapsed && <span>{item.label}</span>}
                </>
              );

              if (item.path) {
                return (
                  <li key={item.label}>
                    <NavLink
                      to={item.path}
                      end
                      className={({ isActive }) =>
                        ["sp-sidebar__link", isActive ? "sp-sidebar__link--active" : ""]
                          .filter(Boolean)
                          .join(" ")
                      }
                      title={isCollapsed ? item.label : undefined}
                    >
                      {content}
                    </NavLink>
                  </li>
                );
              }

              return (
                <li key={item.label}>
                  <button
                    type="button"
                    className={[
                      "sp-sidebar__link",
                      item.disabled ? "sp-sidebar__link--disabled" : "",
                    ]
                      .filter(Boolean)
                      .join(" ")}
                    disabled={item.disabled}
                    title={isCollapsed ? item.label : undefined}
                  >
                    {content}
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>
      </div>

      <div className="sp-sidebar__bottom">
        <button
          type="button"
          className="sp-sidebar__theme"
          onClick={onToggleBrandTheme}
          aria-pressed={isBrandLight}
          title={isCollapsed ? (isBrandLight ? "Usar fundo verde" : "Usar fundo branco") : undefined}
        >
          {isBrandLight ? <Moon size={18} /> : <Sun size={18} />}
          {!isCollapsed && <span>{isBrandLight ? "Fundo verde" : "Fundo branco"}</span>}
        </button>

        <button type="button" className="sp-sidebar__collapse" onClick={onToggleCollapse}>
          {isCollapsed ? <PanelLeftOpen size={18} /> : <PanelLeftClose size={18} />}
          {!isCollapsed && <span>Recolher menu</span>}
        </button>
      </div>
    </aside>
  );
}

