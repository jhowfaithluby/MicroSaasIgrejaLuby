import { useMembers } from '../hooks/useMembers';

export const MemberList = () => {
  const { data, isLoading, isError } = useMembers();

  if (isLoading) {
    return <p role="status">Carregando membros...</p>;
  }

  if (isError) {
    return <p role="alert">Não foi possível carregar os membros.</p>;
  }

  if (!data || data.length === 0) {
    return <p role="status">Nenhum membro cadastrado até o momento.</p>;
  }

  return (
    <table aria-label="Lista de membros">
      <thead>
        <tr>
          <th>Nome</th>
          <th>E-mail</th>
          <th>Telefone</th>
          <th>Data de nascimento</th>
        </tr>
      </thead>
      <tbody>
        {data.map((member) => (
          <tr key={member.id}>
            <td>{member.fullName}</td>
            <td>{member.email}</td>
            <td>{member.phone}</td>
            <td>{member.birthDate ? new Date(member.birthDate).toLocaleDateString() : '-'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
