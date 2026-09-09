import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { Home } from "./pages/Home";
import { Study } from "./pages/Study";

// Login.tsx и Register.tsx удалены (не просто отключены) — см. комментарий
// в AuthContext.tsx про временный автовход демо-аккаунтом. Если понадобится
// вернуть настоящую регистрацию, эти два файла и роуты нужно будет
// написать заново — они не сохранены "на будущее" в проекте.

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Home />
              </ProtectedRoute>
            }
          />
          <Route
            path="/study"
            element={
              <ProtectedRoute>
                <Study />
              </ProtectedRoute>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
