import { useState } from "react";
import type { FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { AlertCircle, Eye, EyeOff, Lock, Mail } from "lucide-react";
import Input from "../../componentes/ui/Input";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useAuth } from "../../contexts/AuthContext";

export default function Login() {
  const navigate = useNavigate();
  const { login, erroDeAcesso, isAuthenticated, isLoading, precisaTrocarSenha } = useAuth();

  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [senhaVisivel, setSenhaVisivel] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [erroDoFormulario, setErroDoFormulario] = useState<string | null>(null);

  async function handleSubmit(evento: FormEvent) {
    evento.preventDefault();
    setErroDoFormulario(null);

    if (!email.trim() || !senha) {
      setErroDoFormulario("Preencha e-mail e senha para continuar.");
      return;
    }

    setEnviando(true);
    const resultado = await login(email, senha);
    setEnviando(false);

    if (!resultado.success) {
      setSenha("");
      return;
    }

    navigate("/dashboard", { replace: true });
  }

  if (isLoading) {
    return (
      <div className="sp-login__carregando">
        <LoadingSpinner size={32} />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to={precisaTrocarSenha ? "/trocar-senha" : "/dashboard"} replace />;
  }

  const mensagemDeErro = erroDoFormulario ?? erroDeAcesso;

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
              onChange={(evento) => setEmail(evento.target.value)}
              autoComplete="username"
            />

            <Input
              label="Senha"
              type={senhaVisivel ? "text" : "password"}
              name="senha"
              placeholder="Digite sua senha"
              leadingIcon={<Lock size={17} />}
              value={senha}
              onChange={(evento) => setSenha(evento.target.value)}
              autoComplete="current-password"
              trailingAdornment={
                <button
                  type="button"
                  onClick={() => setSenhaVisivel((visivel) => !visivel)}
                  aria-label={senhaVisivel ? "Ocultar senha" : "Mostrar senha"}
                >
                  {senhaVisivel ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              }
            />

            {mensagemDeErro && (
              <p className="sp-login__form-error" role="alert">
                <AlertCircle size={16} aria-hidden="true" />
                <span>{mensagemDeErro}</span>
              </p>
            )}

            <Button type="submit" fullWidth isLoading={enviando}>
              Entrar
            </Button>

            <p className="sp-login__hint">
              Esqueceu a senha? Procure um administrador do painel — ele consegue definir uma
              senha provisória para você.
            </p>
          </form>
        </div>

        <p className="sp-login__footer">© 2026 Swiss Park. Todos os direitos reservados.</p>
      </section>
    </div>
  );
}
