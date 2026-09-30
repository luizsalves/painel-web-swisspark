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
  Sparkles, Cpu,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { MODULO_CONCILIACAO } from "../../interfaces/Auth";

interface NavItem {
  label: string;
  icon: LucideIcon;
  path?: string;
  disabled?: boolean;
  /** A tela pertence a este módulo. Quem não está nele nem vê que ela existe. */
  modulo?: string;
  /** A tela é da área administrativa. */
  requerAdministrador?: boolean;
  /** A tela é restrita ao superadministrador do sistema — ninguém mais, nem admin de módulo. */
  somenteSuperAdmin?: boolean;
}

// Itens sem "disabled" já têm página implementada; os demais aguardam a próxima etapa.
const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", icon: LayoutDashboard, path: "/dashboard", modulo: MODULO_CONCILIACAO },
  { label: "Fechamentos", icon: CalendarCheck2, path: "/fechamentos", modulo: MODULO_CONCILIACAO },
  { label: "Empresas", icon: Building2, path: "/empresas", modulo: MODULO_CONCILIACAO },
  { label: "Contas Contábeis", icon: BookText, path: "/contas", modulo: MODULO_CONCILIACAO },
  { label: "Usuários", icon: Users, path: "/usuarios", requerAdministrador: true },
  { label: "CFO Tati • IA", icon: Sparkles, path: "/ia-planning", somenteSuperAdmin: true },
  { label: "Relatórios", icon: BarChart3, disabled: true, modulo: MODULO_CONCILIACAO },
  { label: "Pendências", icon: AlertTriangle, disabled: true, modulo: MODULO_CONCILIACAO },
  { label: "Configurações", icon: Settings, disabled: true, requerAdministrador: true },
  { label: "Auditoria", icon: ShieldCheck, disabled: true, requerAdministrador: true },
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
  const { podeAcessarModulo, administraAlgumModulo, user } = useAuth();
  const isPlanning = useLocation().pathname === "/ia-planning";

  // O menu mostra só o que a pessoa pode abrir. Esconder aqui é cosmético — o
  // que protege de verdade é o [ExigeModulo]/[ExigeSuperAdmin] de cada endpoint no backend.
  const itensVisiveis = NAV_ITEMS.filter((item) => {
    if (item.requerAdministrador && !administraAlgumModulo) return false;
    if (item.somenteSuperAdmin && !user?.isSuperAdmin) return false;
    if (item.modulo && !podeAcessarModulo(item.modulo)) return false;
    return true;
  });

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
            {itensVisiveis.map((item) => {
              const Icon = isPlanning && item.path === "/ia-planning" ? Cpu : item.icon;
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

