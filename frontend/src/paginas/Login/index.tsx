import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Eye, EyeOff, Lock, Mail } from "lucide-react";
import Input from "../../componentes/ui/Input";
import Button from "../../componentes/ui/Button";
import ForgotPasswordModal from "../../componentes/auth/ForgotPasswordModal";
import MicrosoftIcon from "../../componentes/icons/MicrosoftIcon";
import { useAuth } from "../../contexts/AuthContext";

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [isRecoveryOpen, setIsRecoveryOpen] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (!email.trim() || !password.trim()) {
      setFormError("Preencha e-mail e senha para continuar.");
      return;
    }

    setIsSubmitting(true);
    const result = await login(email, password);
    setIsSubmitting(false);

    if (result.success) {
      navigate("/dashboard", { replace: true });
    } else {
      setFormError(result.message ?? "Não foi possível entrar. Tente novamente.");
    }
  }

  return (
    <div className="sp-login">
      <section className="sp-login__hero">
        <div className="sp-login__hero-illustration" aria-hidden="true" />

        <div className="sp-login__hero-content">
          <img className="sp-login__brand-logo" src="/branding/logo_letra-branca.png" alt="Swiss Park" />

          <h1 className="sp-login__hero-title">
            SISTEMA DE CONCILIAÇÃO
            <br />
            CONTÁBIL
          </h1>
          <p className="sp-login__hero-text">Acesse sua conta para continuar.</p>
        </div>

        <div className="sp-login__hero-stripes" aria-hidden="true">
          <span className="sp-login__stripe sp-login__stripe--green" />
          <span className="sp-login__stripe sp-login__stripe--red" />
        </div>
      </section>

      <section className="sp-login__panel">
        <div className="sp-login__card">
          <div className="sp-login__card-header">
            <h2>Bem-vindo(a)!</h2>
            <p>Informe seus dados para acessar o sistema</p>
          </div>

          <form className="sp-login__form" onSubmit={handleSubmit} noValidate>
            <Input
              label="E-mail"
              type="email"
              name="email"
              placeholder="seu.email@swisspark.com.br"
              leadingIcon={<Mail size={17} />}
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              error={formError && !email.trim() ? " " : undefined}
              autoComplete="email"
            />

            <Input
              label="Senha"
              type={isPasswordVisible ? "text" : "password"}
              name="password"
              placeholder="Digite sua senha"
              leadingIcon={<Lock size={17} />}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              error={formError && !password.trim() ? " " : undefined}
              autoComplete="current-password"
              trailingAdornment={
                <button
                  type="button"
                  onClick={() => setIsPasswordVisible((prev) => !prev)}
                  aria-label={isPasswordVisible ? "Ocultar senha" : "Mostrar senha"}
                >
                  {isPasswordVisible ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              }
            />

            {formError && <p className="sp-login__form-error">{formError}</p>}

            <button type="button" className="sp-login__forgot-link" onClick={() => setIsRecoveryOpen(true)}>
              Esqueceu sua senha?
            </button>

            <Button type="submit" fullWidth isLoading={isSubmitting}>
              Entrar
            </Button>

            <div className="sp-login__divider">
              <span />
              <span className="sp-login__divider-text">ou</span>
              <span />
            </div>

            <Button type="button" variant="secondary" fullWidth icon={<MicrosoftIcon size={18} />}>
              Entrar com Microsoft
            </Button>
          </form>
        </div>

        <p className="sp-login__footer">© 2026 Swiss Park. Todos os direitos reservados.</p>
      </section>

      <ForgotPasswordModal isOpen={isRecoveryOpen} onClose={() => setIsRecoveryOpen(false)} />
    </div>
  );
}
