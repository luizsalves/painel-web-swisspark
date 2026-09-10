import type { User } from "../interfaces/Auth";

const STORAGE_KEY = "painel-swiss-park:auth";

export function storeUser(user: User): void {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
}

export function getStoredUser(): User | null {
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as User;
  } catch {
    return null;
  }
}

export function clearStoredUser(): void {
  window.localStorage.removeItem(STORAGE_KEY);
}
