import { useEffect, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { api, getSession } from './lib/api';

type Analytics = Record<string, unknown>;

function JsonMetrics({ data }: { data: Analytics }) {
  const entries = Object.entries(data).filter(([, value]) => typeof value === 'number' || typeof value === 'string');
  return <dl className="metric-grid">{entries.map(([key, value]) => <div className="metric-card" key={key}><dt>{key}</dt><dd>{String(value)}</dd></div>)}</dl>;
}

export function AnalyticsPage() {
  const session = getSession();
  const token = session?.accessToken;
  const [student, setStudent] = useState<Analytics | null>(null);
  const [instructor, setInstructor] = useState<Analytics | null>(null);
  const [admin, setAdmin] = useState<Analytics | null>(null);
  const [error, setError] = useState('');
  const isInstructor = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;
  const isAdmin = session?.roles.includes('Administrator') ?? false;

  useEffect(() => {
    if (!token) return;
    Promise.all([
      api<Analytics>('/api/analytics/student'),
      isInstructor ? api<Analytics>('/api/analytics/instructor') : Promise.resolve(null),
      isAdmin ? api<Analytics>('/api/analytics/admin') : Promise.resolve(null),
    ]).then(([s, i, a]) => { setStudent(s); setInstructor(i); setAdmin(a); })
      .catch(() => setError('No fue posible cargar las analíticas.'));
  }, [token, isAdmin, isInstructor]);

  if (!session) return <Navigate to="/login" replace />;

  return <main>
    <p><Link to="/">← Inicio</Link></p>
    <h1>Analíticas</h1>
    {error && <p role="alert">{error}</p>}
    {!student && !error && <p role="status">Cargando métricas…</p>}
    {student && <section><h2>Mi aprendizaje</h2><JsonMetrics data={student} /></section>}
    {instructor && <section><h2>Rendimiento del instructor</h2><JsonMetrics data={instructor} /></section>}
    {admin && <section><h2>Analítica comercial</h2><JsonMetrics data={admin} /></section>}
  </main>;
}
