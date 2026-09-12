import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { AlertCircle } from "lucide-react";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import MicrosoftIcon from "../../componentes/icons/MicrosoftIcon";
import { useAuth } from "../../contexts/AuthContext";
import { MENSAGEM_ENTRA_NAO_CONFIGURADO } from "../../services/entraId";

export default function Login() {
  const navigate = useNavigate();
  const { entrarComMicrosoft, erroDeAcesso, entraIndisponivel, isAuthenticated, isLoading } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleEntrar() {
    setIsSubmitting(true);
    const resultado = await entrarComMicrosoft();
    setIsSubmitting(false);

    if (resultado.success) {
      navigate("/dashboard", { replace: true });
    }
  }

  if (isLoading) {
    return (
      <div className="sp-login__carregando">
        <LoadingSpinner size={32} />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const mensagemDeErro = entraIndisponivel ? MENSAGEM_ENTRA_NAO_CONFIGURADO : erroDeAcesso;

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
          <p className="sp-login__hero-text">Acesse com sua conta corporativa para continuar.</p>
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
            <p>Entre com a conta Microsoft da Swiss Park</p>
          </div>

          <div className="sp-login__form">
            <Button
              type="button"
              fullWidth
              icon={<MicrosoftIcon size={18} />}
              isLoading={isSubmitting}
              disabled={entraIndisponivel}
              onClick={handleEntrar}
            >
              Entrar com Microsoft
            </Button>

            {mensagemDeErro && (
              <p className="sp-login__form-error" role="alert">
                <AlertCircle size={16} aria-hidden="true" />
                <span>{mensagemDeErro}</span>
              </p>
            )}

            <p className="sp-login__hint">
              A senha é digitada no ambiente da Microsoft e não passa por este sistema. O acesso ao
              painel é liberado por um administrador.
            </p>
          </div>
        </div>

        <p className="sp-login__footer">© 2026 Swiss Park. Todos os direitos reservados.</p>
      </section>
    </div>
  );
}
