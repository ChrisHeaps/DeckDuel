const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7119";

export const API_BASE_URL = baseUrl.replace(/\/+$/, "");

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}