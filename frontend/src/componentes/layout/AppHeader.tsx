import { Bell } from "lucide-react";
import Select from "../ui/Select";
import UserMenu from "./UserMenu";
import PageTitle from "../ui/PageTitle";

interface SelectOption {
  value: string;
  label: string;
}

interface AppHeaderProps {
  title: string;
  subtitle: string;
  period?: string;
  company?: string;
  periodOptions?: SelectOption[];
  companyOptions?: SelectOption[];
  onPeriodChange?: (value: string) => void;
  onCompanyChange?: (value: string) => void;
}

export default function AppHeader({
  title,
  subtitle,
  period,
  company,
  periodOptions,
  companyOptions,
  onPeriodChange,
  onCompanyChange,
}: AppHeaderProps) {
  return (
    <header className="sp-header">
      <PageTitle title={title} subtitle={subtitle} />

      <div className="sp-header__actions">
        {period !== undefined && periodOptions && onPeriodChange && (
          <Select
            aria-label="Filtro de período"
            options={periodOptions}
            value={period}
            onChange={(event) => onPeriodChange(event.target.value)}
          />
        )}
        {company !== undefined && companyOptions && onCompanyChange && (
          <Select
            aria-label="Filtro de empresa"
            options={companyOptions}
            value={company}
            onChange={(event) => onCompanyChange(event.target.value)}
          />
        )}

        <button className="sp-header__icon-btn" aria-label="Notificações">
          <Bell size={19} />
          <span className="sp-header__icon-dot" />
        </button>

        <div className="sp-header__divider" />

        <UserMenu />
      </div>
    </header>
  );
}

