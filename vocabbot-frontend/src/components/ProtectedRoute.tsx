import { ReactNode } from "react";
import { useAuth } from "../context/AuthContext";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return (
      <div style={{ padding: 40, textAlign: "center", fontFamily: "monospace" }}>
        Входим…
      </div>
    );
  }

  if (!isAuthenticated) {
    // Автовход не удался (backend недоступен и т.д.) — раньше здесь был
    // редирект на /login, но этот экран убран (см. AuthContext). Просто
    // показываем понятное сообщение вместо белого экрана.
    return (
      <div style={{ padding: 40, textAlign: "center", fontFamily: "monospace" }}>
        Не удалось подключиться к серверу. Обнови страницу через минуту.
      </div>
    );
  }

  return <>{children}</>;
}
