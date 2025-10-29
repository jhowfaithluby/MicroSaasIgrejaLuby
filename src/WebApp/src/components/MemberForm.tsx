import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useCreateMember } from '../hooks/useMembers';

const schema = z.object({
  fullName: z.string().min(3, 'Informe o nome completo'),
  email: z.string().email('E-mail inválido'),
  phone: z
    .string()
    .min(8, 'Informe um telefone válido')
    .regex(/^\+?[0-9]{8,15}$/, 'Utilize somente números e código do país'),
  birthDate: z.string().optional()
});

export type MemberFormData = z.infer<typeof schema>;

export const MemberForm = () => {
  const { mutateAsync, isPending } = useCreateMember();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors }
  } = useForm<MemberFormData>({
    resolver: zodResolver(schema)
  });

  const onSubmit = async (data: MemberFormData) => {
    setSubmitError(null);
    try {
      await mutateAsync({
        fullName: data.fullName,
        email: data.email,
        phone: data.phone,
        birthDate: data.birthDate ? data.birthDate : undefined
      });
      reset();
    } catch (error) {
      setSubmitError('Não foi possível salvar o membro. Tente novamente.');
    }
  };

  return (
    <form className="card" onSubmit={handleSubmit(onSubmit)} aria-label="Cadastro de membros">
      <h2>Novo membro</h2>
      <label>
        Nome completo
        <input type="text" {...register('fullName')} aria-invalid={!!errors.fullName} />
        {errors.fullName && <span role="alert">{errors.fullName.message}</span>}
      </label>
      <label>
        E-mail
        <input type="email" {...register('email')} aria-invalid={!!errors.email} />
        {errors.email && <span role="alert">{errors.email.message}</span>}
      </label>
      <label>
        Telefone
        <input type="tel" {...register('phone')} aria-invalid={!!errors.phone} />
        {errors.phone && <span role="alert">{errors.phone.message}</span>}
      </label>
      <label>
        Data de nascimento
        <input type="date" {...register('birthDate')} />
      </label>
      <button type="submit" disabled={isPending}>
        {isPending ? 'Salvando...' : 'Cadastrar membro'}
      </button>
      {submitError && <span role="alert">{submitError}</span>}
    </form>
  );
};
