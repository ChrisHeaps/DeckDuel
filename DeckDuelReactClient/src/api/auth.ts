export type LoginRequest = {
  username: string;
  password: string;
  email: string | null;
};

export type LoginResponse = {
  token: string;
  inGameName: string;
};

export type RegisterRequest = {
  username: string;
  password: string;
  email: string | null;
};

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await fetch("https://localhost:7119/login", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Invalid credentials");
  }

  const data = (await response.json()) as LoginResponse;
  return data;
}

export async function register(request: RegisterRequest): Promise<void> {
  const response = await fetch("https://localhost:7119/registerUser", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Registration failed");
  }
}