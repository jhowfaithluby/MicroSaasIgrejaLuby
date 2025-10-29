import { Route, Routes } from 'react-router-dom';
import { useAuth } from './contexts/AuthContext';
import { AuthPage } from './pages/AuthPage';
import { MembersPage } from './pages/MembersPage';

const App = () => {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/" element={isAuthenticated ? <MembersPage /> : <AuthPage />} />
    </Routes>
  );
};

export default App;
