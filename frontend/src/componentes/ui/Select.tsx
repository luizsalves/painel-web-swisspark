import { ChevronDown } from "lucide-react";
import type { SelectHTMLAttributes } from "react";

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  options: SelectOption[];
}

export default function Select({ label, options, className, id, ...rest }: SelectProps) {
  const selectId = id ?? rest.name;

  return (
    <div className="sp-select">
      {label && (
        <label className="sp-select__label" htmlFor={selectId}>
          {label}
        </label>
      )}
      <div className="sp-select__control">
        <select id={selectId} className={`sp-select__input ${className ?? ""}`} {...rest}>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <ChevronDown size={16} className="sp-select__chevron" aria-hidden="true" />
      </div>
    </div>
  );
}

