const baseUrl = import.meta.env.VITE_APPBASEURL ?? "https://localhost:7119";

console.log("MODE", import.meta.env.MODE);
console.log("VITE_APPBASEURL", import.meta.env.VITE_APPBASEURL);
console.log("Resolved API_BASE_URL", baseUrl);

export const API_BASE_URL = baseUrl.replace(/\/+$/, "");

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}