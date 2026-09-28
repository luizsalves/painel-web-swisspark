import { useEffect, useMemo, useRef, useState } from "react";
import { ChevronDown } from "lucide-react";

export interface SearchableSelectOption {
  value: string;
  label: string;
}

interface SearchableSelectProps {
  value: string;
  options: SearchableSelectOption[];
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  ariaLabel?: string;
}

/** Combobox com busca: mostra a lista inteira (inclusive um item "Todos"), filtrável por texto. */
export default function SearchableSelect({
  value,
  options,
  onChange,
  placeholder = "Selecione...",
  disabled = false,
  ariaLabel,
}: SearchableSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const selectedLabel = options.find((option) => option.value === value)?.label ?? "";

  const opcoesFiltradas = useMemo(() => {
    const termo = query.trim().toLowerCase();
    if (!termo) return options;
    return options.filter((option) => option.label.toLowerCase().includes(termo));
  }, [options, query]);

  useEffect(() => {
    if (!isOpen) return;

    function aoClicarFora(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setQuery("");
      }
    }

    document.addEventListener("mousedown", aoClicarFora);
    return () => document.removeEventListener("mousedown", aoClicarFora);
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) searchRef.current?.focus();
  }, [isOpen]);

  return (
    <div className="sp-searchable-select" ref={containerRef}>
      <button
        type="button"
        className="sp-searchable-select__trigger"
        onClick={() => setIsOpen((atual) => !atual)}
        disabled={disabled}
        aria-label={ariaLabel}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
      >
        <span className={selectedLabel ? undefined : "sp-searchable-select__placeholder"}>
          {selectedLabel || placeholder}
        </span>
        <ChevronDown size={16} />
      </button>

      {isOpen && (
        <div className="sp-searchable-select__panel">
          <input
            ref={searchRef}
            type="text"
            className="sp-searchable-select__search"
            placeholder="Pesquisar..."
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
          <ul className="sp-searchable-select__list" role="listbox">
            {opcoesFiltradas.length === 0 && <li className="sp-searchable-select__empty">Nenhum resultado</li>}
            {opcoesFiltradas.map((option) => (
              <li key={option.value || "__todos__"}>
                <button
                  type="button"
                  role="option"
                  aria-selected={option.value === value}
                  className={
                    option.value === value
                      ? "sp-searchable-select__option sp-searchable-select__option--active"
                      : "sp-searchable-select__option"
                  }
                  onClick={() => {
                    onChange(option.value);
                    setIsOpen(false);
                    setQuery("");
                  }}
                >
                  {option.label}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
