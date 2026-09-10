import type { ButtonHTMLAttributes, ReactNode } from "react";
import LoadingSpinner from "./LoadingSpinner";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  isLoading?: boolean;
  icon?: ReactNode;
  fullWidth?: boolean;
}

export default function Button({
  variant = "primary",
  isLoading = false,
  icon,
  fullWidth = false,
  disabled,
  children,
  className,
  ...rest
}: ButtonProps) {
  const classes = ["sp-btn", `sp-btn--${variant}`, fullWidth ? "sp-btn--full" : "", className]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classes} disabled={disabled || isLoading} {...rest}>
      {isLoading ? (
        <LoadingSpinner size={16} color="currentColor" />
      ) : (
        icon && <span className="sp-btn__icon">{icon}</span>
      )}
      <span>{children}</span>
    </button>
  );
}

