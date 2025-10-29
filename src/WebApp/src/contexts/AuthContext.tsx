import axios, { type AxiosRequestConfig } from 'axios';
import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState
} from 'react';
import { api } from '../lib/api';

type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
};

type LoginInput = {
  email: string;
  password: string;
};

type RegisterInput = {
  fullName: string;
  email: string;
  password: string;
};

type AuthUser = {
  email: string;
};

type AuthContextValue = {
  tokens: TokenResponse | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (input: LoginInput) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  logout: () => Promise<void>;
};

const STORAGE_KEY = 'microsaas-auth';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

type RetryableAxiosRequestConfig = AxiosRequestConfig & { _retry?: boolean };

const extractUser = (token: string | null): AuthUser | null => {
  if (!token) {
    return null;
  }

  try {
    const [, payload] = token.split('.');
    if (!payload) {
      return null;
    }

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = JSON.parse(atob(normalized));
    const email = decoded.email ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];

    if (typeof email === 'string') {
      return { email };
    }

    return null;
  } catch (error) {
    console.warn('Falha ao interpretar token JWT', error);
    return null;
  }
};

const readStoredTokens = (): TokenResponse | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);
  if (!stored) {
    return null;
  }

  try {
    return JSON.parse(stored) as TokenResponse;
  } catch (error) {
    console.warn('Não foi possível ler tokens armazenados', error);
    return null;
  }
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [tokens, setTokens] = useState<TokenResponse | null>(() => readStoredTokens());
  const [user, setUser] = useState<AuthUser | null>(() => extractUser(readStoredTokens()?.accessToken ?? null));
  const refreshInFlight = useRef<Promise<string | null> | null>(null);

  const persistTokens = useCallback((next: TokenResponse | null) => {
    setTokens(next);
    setUser(extractUser(next?.accessToken ?? null));

    if (typeof window !== 'undefined') {
      if (next) {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
      } else {
        window.localStorage.removeItem(STORAGE_KEY);
      }
    }
  }, []);

  const logout = useCallback(async () => {
    if (tokens?.refreshToken) {
      try {
        await api.post('/api/v1/auth/logout', { refreshToken: tokens.refreshToken });
      } catch (error) {
        console.warn('Falha ao realizar logout', error);
      }
    }

    persistTokens(null);
  }, [tokens, persistTokens]);

  const refreshTokens = useCallback(async (): Promise<string | null> => {
    if (!tokens?.refreshToken) {
      return null;
    }

    if (refreshInFlight.current) {
      return refreshInFlight.current;
    }

    const refreshPromise = (async () => {
      try {
        const response = await api.post<TokenResponse>('/api/v1/auth/refresh', {
          refreshToken: tokens.refreshToken
        });
        persistTokens(response.data);
        return response.data.accessToken;
      } catch (error) {
        console.warn('Não foi possível renovar o token de acesso', error);
        persistTokens(null);
        return null;
      } finally {
        refreshInFlight.current = null;
      }
    })();

    refreshInFlight.current = refreshPromise;
    return refreshPromise;
  }, [tokens, persistTokens]);

  useEffect(() => {
    const requestInterceptor = api.interceptors.request.use(config => {
      if (tokens?.accessToken) {
        config.headers = config.headers ?? {};
        config.headers.Authorization = `Bearer ${tokens.accessToken}`;
      }
      return config;
    });

    const responseInterceptor = api.interceptors.response.use(
      response => response,
      async error => {
        if (!axios.isAxiosError(error)) {
          return Promise.reject(error);
        }

        const originalRequest = (error.config ?? {}) as RetryableAxiosRequestConfig;

        if (
          error.response?.status === 401 &&
          tokens?.refreshToken &&
          !originalRequest._retry
        ) {
          originalRequest._retry = true;
          const newAccessToken = await refreshTokens();

          if (newAccessToken) {
            originalRequest.headers = originalRequest.headers ?? {};
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
            return api.request(originalRequest);
          }

          await logout();
        }

        return Promise.reject(error);
      }
    );

    return () => {
      api.interceptors.request.eject(requestInterceptor);
      api.interceptors.response.eject(responseInterceptor);
    };
  }, [tokens, refreshTokens, logout]);

  const extractErrorMessage = useCallback((error: unknown) => {
    if (axios.isAxiosError(error)) {
      const payload = error.response?.data as { detail?: string; title?: string } | undefined;
      const message = payload?.detail ?? payload?.title;
      return message ?? 'Não foi possível processar a solicitação.';
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'Não foi possível processar a solicitação.';
  }, []);

  const login = useCallback(
    async (input: LoginInput) => {
      try {
        const response = await api.post<TokenResponse>('/api/v1/auth/login', input);
        persistTokens(response.data);
      } catch (error) {
        throw new Error(extractErrorMessage(error));
      }
    },
    [persistTokens, extractErrorMessage]
  );

  const register = useCallback(
    async (input: RegisterInput) => {
      try {
        const response = await api.post<TokenResponse>('/api/v1/auth/register', input);
        persistTokens(response.data);
      } catch (error) {
        throw new Error(extractErrorMessage(error));
      }
    },
    [persistTokens, extractErrorMessage]
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      tokens,
      user,
      isAuthenticated: Boolean(tokens?.accessToken),
      login,
      register,
      logout
    }),
    [tokens, user, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextValue => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth deve ser utilizado dentro de AuthProvider');
  }

  return context;
};
