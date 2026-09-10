import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import ProtectedRoute from "./rotas/ProtectedRoute";
import Login from "./paginas/Login";
import Dashboard from "./paginas/Dashboard";
import Fechamentos from "./paginas/Fechamentos";
import Empresas from "./paginas/Empresas";
import ContasContabeis from "./paginas/ContasContabeis";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          <Route
            path="/fechamentos"
            element={
              <ProtectedRoute>
                <Fechamentos />
              </ProtectedRoute>
            }
          />
          <Route
            path="/empresas"
            element={
              <ProtectedRoute>
                <Empresas />
              </ProtectedRoute>
            }
          />
          <Route
            path="/contas"
            element={
              <ProtectedRoute>
                <ContasContabeis />
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
