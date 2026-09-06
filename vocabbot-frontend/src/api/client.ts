const API_URL = import.meta.env.VITE_API_URL;

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

const TOKEN_KEY = "vocabbot_token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}
export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Общая обёртка над fetch: подставляет базовый URL backend'а, добавляет
 * Authorization-заголовок если есть токен, парсит JSON и оборачивает
 * ошибки в ApiError с понятным сообщением (WordsController/AuthController
 * возвращают либо строку, либо массив строк с ошибками Identity).
 */
export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`${API_URL}${path}`, { ...options, headers });

  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  const data = text ? JSON.parse(text) : undefined;

  if (!res.ok) {
    const message = Array.isArray(data)
      ? data.join(", ")
      : typeof data === "string"
      ? data
      : data?.title ?? `Ошибка запроса (${res.status})`;
    throw new ApiError(res.status, message);
  }

  return data as T;
}
