import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../lib/api';

export type Member = {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  birthDate?: string | null;
  createdAtUtc: string;
};

export type CreateMemberInput = {
  fullName: string;
  email: string;
  phone: string;
  birthDate?: string;
};

const MEMBERS_QUERY_KEY = ['members'];

export const useMembers = () => {
  return useQuery({
    queryKey: MEMBERS_QUERY_KEY,
    queryFn: async () => {
      const response = await api.get<Member[]>('/api/v1/members');
      return response.data;
    }
  });
};

export const useCreateMember = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (input: CreateMemberInput) => {
      const response = await api.post<Member>('/api/v1/members', input);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: MEMBERS_QUERY_KEY });
    }
  });
};
