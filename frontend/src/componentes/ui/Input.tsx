import { forwardRef } from "react";
import type { InputHTMLAttributes, ReactNode } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  leadingIcon?: ReactNode;
  trailingAdornment?: ReactNode;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, leadingIcon, trailingAdornment, id, className, ...rest }, ref) => {
    const inputId = id ?? rest.name;

    return (
      <div className="sp-field">
        {label && (
          <label className="sp-field__label" htmlFor={inputId}>
            {label}
          </label>
        )}
        <div className={`sp-field__control ${error ? "sp-field__control--error" : ""}`}>
          {leadingIcon && <span className="sp-field__icon">{leadingIcon}</span>}
          <input
            ref={ref}
            id={inputId}
            className={`sp-field__input ${className ?? ""}`}
            aria-invalid={Boolean(error)}
            {...rest}
          />
          {trailingAdornment && <span className="sp-field__adornment">{trailingAdornment}</span>}
        </div>
        {error && <span className="sp-field__error">{error}</span>}
      </div>
    );
  }
);

Input.displayName = "Input";

export default Input;

