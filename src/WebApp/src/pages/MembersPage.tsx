import { MemberForm } from '../components/MemberForm';
import { MemberList } from '../components/MemberList';
import { useAuth } from '../contexts/AuthContext';

export const MembersPage = () => {
  const { user, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
  };

  return (
    <main className="container">
      <header className="page-header">
        <div>
          <h1>Gestão de membros</h1>
          <p>Mantenha os dados da sua comunidade organizados.</p>
        </div>
        <div className="session-card" role="status" aria-live="polite">
          <span>Conectado como</span>
          <strong>{user?.email ?? 'Usuário autenticado'}</strong>
          <button type="button" className="secondary" onClick={handleLogout}>
            Sair
          </button>
        </div>
      </header>
      <section className="layout">
        <MemberForm />
        <MemberList />
      </section>
    </main>
  );
};
