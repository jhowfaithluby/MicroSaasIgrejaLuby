import { type ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MembersPage } from '../pages/MembersPage';
import * as useMembersModule from '../hooks/useMembers';

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => ({
    tokens: {
      accessToken: 'token',
      refreshToken: 'refresh',
      expiresAtUtc: new Date().toISOString()
    },
    user: { email: 'tester@example.com' },
    isAuthenticated: true,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn()
  })
}));

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false }
    }
  });

  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('MembersPage', () => {
  it('renderiza e permite cadastrar um novo membro', async () => {
    const wrapper = createWrapper();
    const createMember = vi.fn().mockResolvedValue(undefined);

    vi.spyOn(useMembersModule, 'useMembers').mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
      error: null,
      failureCount: 0,
      isFetched: true,
      isInitialLoading: false,
      isFetching: false,
      isPending: false,
      isSuccess: true,
      status: 'success',
      fetchStatus: 'idle',
      refetch: vi.fn()
    } as unknown as ReturnType<typeof useMembersModule.useMembers>);

    vi.spyOn(useMembersModule, 'useCreateMember').mockReturnValue({
      mutateAsync: createMember,
      isPending: false,
      data: undefined,
      error: null,
      failureCount: 0,
      isError: false,
      isIdle: false,
      isPaused: false,
      isSuccess: true,
      reset: vi.fn()
    } as unknown as ReturnType<typeof useMembersModule.useCreateMember>);

    render(<MembersPage />, { wrapper });

    await userEvent.type(screen.getByLabelText(/nome completo/i), 'João Souza');
    await userEvent.type(screen.getByLabelText(/e-mail/i), 'joao@example.com');
    await userEvent.type(screen.getByLabelText(/telefone/i), '+5511977778888');
    await userEvent.click(screen.getByRole('button', { name: /cadastrar membro/i }));

    await waitFor(() => {
      expect(createMember).toHaveBeenCalledWith({
        fullName: 'João Souza',
        email: 'joao@example.com',
        phone: '+5511977778888',
        birthDate: undefined
      });
    });
  });
});
