import { Navigate } from "react-router-dom";
import { Compass } from "lucide-react";
import AppLayout from "../../componentes/layout/AppLayout";
import AppHeader from "../../componentes/layout/AppHeader";
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

  // Acontece com quem pertence só a um módulo que ainda não tem tela pronta.
  return (
    <AppLayout>
      <AppHeader title="Bem-vindo(a)" subtitle="Seu acesso está ativo" />

      <main className="sp-management">
        <div className="sp-inicio-vazio">
          <Compass size={36} aria-hidden="true" />
          <h2>Nenhuma tela disponível ainda</h2>
          <p>
            Seu acesso está liberado, mas os módulos a que você pertence ainda não têm telas
            publicadas. Assim que houver, elas aparecem no menu ao lado.
          </p>
        </div>
      </main>
    </AppLayout>
  );
}
