import { useState } from "react";
import type { FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { AlertCircle, Lock, ShieldCheck } from "lucide-react";
import Input from "../../componentes/ui/Input";
import Button from "../../componentes/ui/Button";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import { useAuth } from "../../contexts/AuthContext";
import { extrairMensagemDeErro } from "../../services/api";

const TAMANHO_MINIMO = 8;

export default function TrocarSenha() {
  const navigate = useNavigate();
  const { isAuthenticated, isLoading, precisaTrocarSenha, trocarSenha, logout } = useAuth();

  const [senhaAtual, setSenhaAtual] = useState("");
  const [novaSenha, setNovaSenha] = useState("");
  const [confirmacao, setConfirmacao] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  async function handleSubmit(evento: FormEvent) {
    evento.preventDefault();
    setErro(null);

    if (novaSenha.length < TAMANHO_MINIMO) {
      setErro(`A nova senha precisa ter pelo menos ${TAMANHO_MINIMO} caracteres.`);
      return;
    }

    if (novaSenha !== confirmacao) {
      setErro("A confirmação não confere com a nova senha.");
      return;
    }

    setEnviando(true);

    try {
      await trocarSenha(senhaAtual, novaSenha);
      navigate("/dashboard", { replace: true });
    } catch (erroDaApi) {
      setErro(extrairMensagemDeErro(erroDaApi, "Não foi possível trocar a senha."));
      setEnviando(false);
    }
  }

  if (isLoading) {
    return (
      <div className="sp-login__carregando">
        <LoadingSpinner size={32} />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="sp-login__carregando">
      <div className="sp-login__card sp-troca-senha">
        <div className="sp-login__card-header">
          <h2>
            <ShieldCheck size={20} aria-hidden="true" /> Defina uma nova senha
          </h2>
          <p>
            {precisaTrocarSenha
              ? "Sua senha foi definida por um administrador. Escolha uma senha própria para continuar."
              : "Escolha uma nova senha para a sua conta."}
          </p>
        </div>

        <form className="sp-login__form" onSubmit={handleSubmit} noValidate>
          <Input
            label="Senha atual"
            type="password"
            name="senhaAtual"
            leadingIcon={<Lock size={17} />}
            value={senhaAtual}
            onChange={(evento) => setSenhaAtual(evento.target.value)}
            autoComplete="current-password"
          />

          <Input
            label="Nova senha"
            type="password"
            name="novaSenha"
            placeholder={`Mínimo de ${TAMANHO_MINIMO} caracteres`}
            leadingIcon={<Lock size={17} />}
            value={novaSenha}
            onChange={(evento) => setNovaSenha(evento.target.value)}
            autoComplete="new-password"
          />

          <Input
            label="Confirme a nova senha"
            type="password"
            name="confirmacao"
            leadingIcon={<Lock size={17} />}
            value={confirmacao}
            onChange={(evento) => setConfirmacao(evento.target.value)}
            autoComplete="new-password"
          />

          {erro && (
            <p className="sp-login__form-error" role="alert">
              <AlertCircle size={16} aria-hidden="true" />
              <span>{erro}</span>
            </p>
          )}

          <Button type="submit" fullWidth isLoading={enviando}>
            Salvar nova senha
          </Button>

          <button
            type="button"
            className="sp-troca-senha__sair"
            onClick={() => {
              logout();
              navigate("/login", { replace: true });
            }}
          >
            Sair
          </button>
        </form>
      </div>
    </div>
  );
}
