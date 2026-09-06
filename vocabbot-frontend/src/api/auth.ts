import { apiFetch } from "./client";

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
  email: string;
}

export function register(email: string, password: string, firstName?: string) {
  return apiFetch<AuthResponse>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, firstName }),
  });
}

export function login(email: string, password: string) {
  return apiFetch<AuthResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}
