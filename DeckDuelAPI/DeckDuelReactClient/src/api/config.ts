const baseUrl = import.meta.env.VITE_APPBASEURL;

if (!baseUrl) {
  throw new Error("Missing VITE_APPBASEURL in production build.");
}

export const API_BASE_URL = baseUrl.replace(/\/+$/, "");

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}