import { Navigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { MODULO_CONCILIACAO } from "../../interfaces/Auth";

/**
 * Decide para onde mandar a pessoa ao entrar.
 *
 * Redirecionar direto para /dashboard daria laço infinito para quem não é da
 * conciliação: a rota rejeitaria e devolveria para cá, que mandaria de volta.
 * Por isso a primeira tela depende de a que módulo a pessoa pertence.
 */
export default function Inicio() {
  const { podeAcessarModulo, administraAlgumModulo } = useAuth();

  if (podeAcessarModulo(MODULO_CONCILIACAO)) {
    return <Navigate to="/dashboard" replace />;
  }

  if (administraAlgumModulo) {
    return <Navigate to="/usuarios" replace />;
  }

  return <Navigate to="/balancete" replace />;
}
