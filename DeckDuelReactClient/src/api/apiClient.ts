export async function apiFetch(
  input: RequestInfo,
  init: RequestInit = {}) 
  {
  const token = localStorage.getItem("deckdueljwt")

  const headers = {
    "Content-Type": "application/json",
    ...init.headers,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  }

  const response = await fetch(input, {
    ...init,
    headers,
  })

  if (!response.ok) {
    // Optional: auto-logout on 401
    if (response.status === 401) {
      localStorage.removeItem("deckdueljwt")
      window.location.href = "/login"
    }

    throw new Error("API request failed")
  }

  return response
}