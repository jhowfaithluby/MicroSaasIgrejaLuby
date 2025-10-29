import { FormEvent, useState } from 'react';
import { useAuth } from '../contexts/AuthContext';

export const AuthPage = () => {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const toggleMode = () => {
    setMode(current => (current === 'login' ? 'register' : 'login'));
    setError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      if (mode === 'login') {
        await login({ email, password });
      } else {
        await register({ fullName, email, password });
      }
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Não foi possível processar a solicitação.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="container auth-container">
      <div className="card auth-card">
        <header>
          <h1>{mode === 'login' ? 'Acesse sua conta' : 'Crie uma nova conta'}</h1>
          <p className="muted">
            {mode === 'login'
              ? 'Entre para gerenciar os membros da igreja com segurança.'
              : 'Cadastre-se para começar a gerenciar os membros.'}
          </p>
        </header>

        <form className="form" onSubmit={handleSubmit}>
          {mode === 'register' && (
            <div className="form-field">
              <label htmlFor="fullName">Nome completo</label>
              <input
                id="fullName"
                name="fullName"
                type="text"
                autoComplete="name"
                required
                value={fullName}
                onChange={event => setFullName(event.target.value)}
              />
            </div>
          )}

          <div className="form-field">
            <label htmlFor="email">E-mail</label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={event => setEmail(event.target.value)}
            />
          </div>

          <div className="form-field">
            <label htmlFor="password">Senha</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              required
              minLength={8}
              value={password}
              onChange={event => setPassword(event.target.value)}
            />
            <small className="muted">A senha deve conter pelo menos 8 caracteres, incluindo números e símbolos.</small>
          </div>

          {error && (
            <div role="alert" className="error-message">
              {error}
            </div>
          )}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Processando…' : mode === 'login' ? 'Entrar' : 'Cadastrar'}
          </button>
        </form>

        <button type="button" className="link-button" onClick={toggleMode}>
          {mode === 'login' ? 'Não possui conta? Cadastre-se' : 'Já tem conta? Fazer login'}
        </button>
      </div>
    </main>
  );
};
