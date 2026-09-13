import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../contexts/AuthContext";
import LoadingSpinner from "../componentes/ui/LoadingSpinner";

interface ProtectedRouteProps {
  children: ReactNode;
  /** A tela pertence a este módulo. Quem não está nele volta para a raiz. */
  requireModulo?: string;
  /** A tela é da área administrativa. */
  requireAdministrador?: boolean;
}

export default function ProtectedRoute({
  children,
  requireModulo,
  requireAdministrador,
}: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, precisaTrocarSenha, podeAcessarModulo, administraAlgumModulo } =
    useAuth();

  if (isLoading) {
    return (
      <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100vh" }}>
        <LoadingSpinner size={32} />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Senha provisória fecha o painel até ser trocada. O backend recusa do mesmo
  // jeito — aqui é só para a pessoa ver a tela certa em vez de um 403.
  if (precisaTrocarSenha) {
    return <Navigate to="/trocar-senha" replace />;
  }

  // A trava de verdade está no backend, no [ExigeModulo] de cada endpoint; aqui
  // é só para não mostrar uma tela que a pessoa não pode usar.
  if (requireModulo && !podeAcessarModulo(requireModulo)) {
    return <Navigate to="/" replace />;
  }

  if (requireAdministrador && !administraAlgumModulo) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
