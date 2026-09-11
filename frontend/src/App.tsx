import { Navigate, Route, Routes } from 'react-router-dom';

function HomePage() {
  return (
    <main>
      <h1>Kognia</h1>
      <p>Aprende. Avanza. Domina.</p>
    </main>
  );
}

function NotFoundPage() {
  return (
    <main>
      <h1>404</h1>
      <p>La página solicitada no existe.</p>
    </main>
  );
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/home" element={<Navigate to="/" replace />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
