import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import { FiltrosProvider } from "./contexts/FiltrosContext";
import PainelFiltros from "./componentes/filtros/PainelFiltros";
import TatiFlutuante from "./componentes/tati/TatiFlutuante";
import ProtectedRoute from "./rotas/ProtectedRoute";
import Login from "./paginas/Login";
import Dashboard from "./paginas/Dashboard";
import Fechamentos from "./paginas/Fechamentos";
import Balancete from "./paginas/Balancete";
import Indicadores from "./paginas/Indicadores";
import Empresas from "./paginas/Empresas";
import ContasContabeis from "./paginas/ContasContabeis";
import IaPlanning from "./paginas/IaPlanning";
import Inicio from "./paginas/Inicio";
import { MODULO_CONCILIACAO } from "./interfaces/Auth";
import Usuarios from "./paginas/Usuarios";
import TrocarSenha from "./paginas/TrocarSenha";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <FiltrosProvider>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/trocar-senha" element={<TrocarSenha />} />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute requireModulo={MODULO_CONCILIACAO}>
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/fechamentos"
              element={
                <ProtectedRoute requireModulo={MODULO_CONCILIACAO}>
                  <Fechamentos />
                </ProtectedRoute>
              }
            />
            <Route
              path="/balancete"
              element={
                <ProtectedRoute>
                  <Balancete />
                </ProtectedRoute>
              }
            />
            <Route
              path="/indicadores"
              element={
                <ProtectedRoute>
                  <Indicadores />
                </ProtectedRoute>
              }
            />
            <Route
              path="/empresas"
              element={
                <ProtectedRoute requireModulo={MODULO_CONCILIACAO}>
                  <Empresas />
                </ProtectedRoute>
              }
            />
            <Route
              path="/contas"
              element={
                <ProtectedRoute requireModulo={MODULO_CONCILIACAO}>
                  <ContasContabeis />
                </ProtectedRoute>
              }
            />
            <Route
              path="/ia-planning"
              element={
                <ProtectedRoute requireSuperAdmin>
                  <IaPlanning />
                </ProtectedRoute>
              }
            />
            <Route
              path="/usuarios"
              element={
                <ProtectedRoute requireAdministrador>
                  <Usuarios />
                </ProtectedRoute>
              }
            />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <Inicio />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
          <PainelFiltros />
          <TatiFlutuante />
        </FiltrosProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
