import { createContext, useContext, useEffect, useState } from "react";
import { login as loginApi } from "../api/auth";
import type { LoginRequest } from "../api/auth";

type AuthContextType = {
  token: string | null;
  inGameName: string | null;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(null);
  const [inGameName, setInGameName] = useState<string | null>(null);

  // Load auth state on app start
  useEffect(() => {
    const storedToken = localStorage.getItem("deckdueljwt");
    const storedInGameName = localStorage.getItem("deckduelingamename");

    if (storedToken) {
      setToken(storedToken);
    }

    if (storedInGameName) {
      setInGameName(storedInGameName);
    }
  }, []);

  const login = async (request: LoginRequest) => {
    const loginResponse = await loginApi(request);

    localStorage.setItem("deckdueljwt", loginResponse.token);
    localStorage.setItem("deckduelingamename", loginResponse.inGameName);
    setToken(loginResponse.token);
    setInGameName(loginResponse.inGameName);
  };

  const logout = () => {
    localStorage.removeItem("deckdueljwt");
    localStorage.removeItem("deckduelingamename");
    setToken(null);
    setInGameName(null);
  };

  return (
    <AuthContext
      value={{
        token,
        inGameName,
        isAuthenticated: !!token,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
}
