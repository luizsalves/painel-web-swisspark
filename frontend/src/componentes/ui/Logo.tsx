interface LogoProps {
  variant?: "full" | "mark";
  tone?: "dark" | "light";
  size?: number;
}

/**
 * Marca Swiss Park: monograma "SP" em losango, remetendo a um selo
 * corporativo/contábil, seguido do nome da empresa quando variant="full".
 */
export default function Logo({ variant = "full", tone = "dark", size = 40 }: LogoProps) {
  const markBg = tone === "dark" ? "#FFFFFF" : "var(--color-green)";
  const markFg = tone === "dark" ? "var(--color-green)" : "#FFFFFF";
  const textColor = tone === "dark" ? "var(--color-text)" : "#FFFFFF";

  return (
    <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
      <svg width={size} height={size} viewBox="0 0 40 40" aria-hidden="true">
        <rect x="1" y="1" width="38" height="38" rx="10" fill={markBg} />
        <path
          d="M13 26.5V13.5H20.2C22.6 13.5 24.3 15 24.3 17.2C24.3 19.3 22.6 20.7 20.2 20.7H15.6V26.5H13ZM15.6 18.4H19.8C21 18.4 21.7 17.9 21.7 17.1C21.7 16.3 21 15.8 19.8 15.8H15.6V18.4Z"
          fill={markFg}
        />
        <path
          d="M23.5 24.8L25.2 23C26.1 24 27.3 24.6 28.8 24.6C30.2 24.6 30.9 24.1 30.9 23.3C30.9 22.6 30.4 22.3 28.6 21.9C25.9 21.3 24.2 20.5 24.2 18.1C24.2 15.8 26.2 14.3 28.9 14.3C30.9 14.3 32.4 15 33.4 16.1L31.8 18C31 17.2 29.9 16.7 28.8 16.7C27.6 16.7 26.9 17.2 26.9 17.9C26.9 18.6 27.5 18.9 29.3 19.3C31.9 19.9 33.6 20.8 33.6 23.1C33.6 25.5 31.6 27 28.7 27C26.5 27 24.6 26.2 23.5 24.8Z"
          fill={markFg}
        />
      </svg>
      {variant === "full" && (
        <span
          style={{
            fontWeight: 800,
            fontSize: size * 0.42,
            letterSpacing: "0.02em",
            color: textColor,
            lineHeight: 1,
          }}
        >
          Swiss Park
        </span>
      )}
    </div>
  );
}
