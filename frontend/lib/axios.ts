import axios from "axios";
import { useAppStore } from "@/store/useAppStore";

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5229/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use((config) => {
  const token = useAppStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  // The backend's IdempotencyFilter requires this header on creation POSTs
  // (Portfolio, PortfolioEntry, CryptoCurrency, Exchange); harmless on other
  // requests since only those endpoints check for it.
  if (config.method === "post" && !config.headers["Idempotency-Key"]) {
    config.headers["Idempotency-Key"] = crypto.randomUUID();
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAppStore.getState().clearSession();
      if (typeof window !== "undefined") {
        // This interceptor runs outside React (no useRouter available here), so a
        // hard navigation is the only option.
        // eslint-disable-next-line @next/next/no-location-assign-relative-destination
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

export default api;
