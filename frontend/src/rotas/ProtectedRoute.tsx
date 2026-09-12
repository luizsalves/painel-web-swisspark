import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../contexts/AuthContext";
import LoadingSpinner from "../componentes/ui/LoadingSpinner";
import type { UserRole } from "../interfaces/Auth";

interface ProtectedRouteProps {
  children: ReactNode;
  /** Quando informado, só esse perfil entra. Quem não tem volta para o dashboard. */
  requireRole?: UserRole;
}

export default function ProtectedRoute({ children, requireRole }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, user } = useAuth();

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

  // A trava de verdade está no backend; aqui é só para não mostrar uma tela que
  // a pessoa não pode usar.
  if (requireRole && user?.role !== requireRole) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}
