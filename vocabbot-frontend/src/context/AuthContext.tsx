import { createContext, useContext, useState, useCallback, ReactNode } from "react";
import * as authApi from "../api/auth";
import { setToken, clearToken, getToken } from "../api/client";

interface AuthState {
  isAuthenticated: boolean;
  email: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName?: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

// email не приходит отдельно при перезагрузке страницы (мы храним только
// JWT) — держим его в localStorage рядом с токеном просто для отображения
// в UI ("Привет, email"), не для авторизации.
const EMAIL_KEY = "vocabbot_email";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() =>
    getToken() ? localStorage.getItem(EMAIL_KEY) : null
  );

  const applyAuth = (res: authApi.AuthResponse) => {
    setToken(res.token);
    localStorage.setItem(EMAIL_KEY, res.email);
    setEmail(res.email);
  };

  const login = useCallback(async (loginEmail: string, password: string) => {
    const res = await authApi.login(loginEmail, password);
    applyAuth(res);
  }, []);

  const register = useCallback(
    async (regEmail: string, password: string, firstName?: string) => {
      const res = await authApi.register(regEmail, password, firstName);
      applyAuth(res);
    },
    []
  );

  const logout = useCallback(() => {
    clearToken();
    localStorage.removeItem(EMAIL_KEY);
    setEmail(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{ isAuthenticated: !!email, email, login, register, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth должен использоваться внутри <AuthProvider>");
  return ctx;
}
