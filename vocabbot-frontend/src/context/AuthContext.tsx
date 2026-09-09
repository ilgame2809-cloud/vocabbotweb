import { createContext, useContext, useState, useCallback, useEffect, ReactNode } from "react";
import * as authApi from "../api/auth";
import { setToken, clearToken, getToken } from "../api/client";

interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  email: string | null;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

const EMAIL_KEY = "vocabbot_email";

/**
 * ВРЕМЕННО (см. README): пока сайт тестируется и у него нет реальных
 * пользователей, экраны Login/Register убраны (файлы удалены из проекта,
 * не просто отключены от роутинга — раньше это привело к падению сборки
 * на Vercel из-за TS-ошибок в неиспользуемом коде) — при первом заходе
 * фронт сам логинится под одним общим демо-аккаунтом (создаёт его при
 * первом запуске, если ещё не существует). Backend по-прежнему требует
 * JWT на все защищённые эндпоинты — просто получение токена теперь не
 * требует от человека ничего вводить руками.
 *
 * Когда дойдёт до реального запуска с разными людьми — это нужно откатить:
 * написать заново роуты /login и /register (см. git-историю этого файла,
 * если нужно восстановить старую версию с login/register в AuthState) и
 * убрать автовход из этого файла.
 */
const DEMO_EMAIL = "demo@vocabbot.local";
const DEMO_PASSWORD = "Demo12345!";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() =>
    getToken() ? localStorage.getItem(EMAIL_KEY) : null
  );
  const [loading, setLoading] = useState(!getToken());

  const applyAuth = (res: authApi.AuthResponse) => {
    setToken(res.token);
    localStorage.setItem(EMAIL_KEY, res.email);
    setEmail(res.email);
  };

  useEffect(() => {
    if (getToken()) return; // уже есть токен с прошлого раза — ничего делать не нужно

    let cancelled = false;
    (async () => {
      try {
        const res = await authApi.login(DEMO_EMAIL, DEMO_PASSWORD);
        if (!cancelled) applyAuth(res);
      } catch {
        // Демо-аккаунта ещё нет на backend — создаём один раз.
        try {
          const res = await authApi.register(DEMO_EMAIL, DEMO_PASSWORD, "Демо");
          if (!cancelled) applyAuth(res);
        } catch (err) {
          // Backend недоступен — оставляем isAuthenticated=false, ProtectedRoute
          // сам решит, что показать (сейчас — просто пустой экран, см. App.tsx).
          console.error("Не удалось создать/войти в демо-аккаунт:", err);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const logout = useCallback(() => {
    clearToken();
    localStorage.removeItem(EMAIL_KEY);
    setEmail(null);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated: !!email, loading, email, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth должен использоваться внутри <AuthProvider>");
  return ctx;
}
