import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, getSession } from './lib/api';

type Course = {
  id: string;
  title: string;
  summary: string;
  level: string;
  category: string;
  lessons: number;
};

export function EngagementCatalogPage() {
  const session = getSession();
  const navigate = useNavigate();
  const [courses, setCourses] = useState<Course[]>([]);
  const [message, setMessage] = useState('');

  useEffect(() => {
    api<Course[]>('/api/catalog/courses').then(setCourses).catch(() => setMessage('No fue posible cargar los cursos.'));
  }, []);

  async function favorite(courseId: string) {
    if (!session) { navigate('/login'); return; }
    try {
      await api(`/api/engagement/favorites/${courseId}`, { method: 'POST' });
      setMessage('Curso agregado a favoritos.');
    } catch {
      setMessage('No fue posible guardar el curso.');
    }
  }

  return <main>
    <p><Link to="/">← Inicio</Link></p>
    <h1>Descubrir y guardar cursos</h1>
    {message && <p role="status">{message}</p>}
    {courses.length === 0 ? <p>No hay cursos publicados todavía.</p> : <ul>{courses.map((course) => <li key={course.id}>
      <h2>{course.title}</h2><p>{course.summary}</p><p>{course.category} · {course.level} · {course.lessons} lecciones</p>
      <button type="button" onClick={() => void favorite(course.id)}>{session ? 'Agregar a favoritos' : 'Inicia sesión para guardar'}</button>{' '}
      <Link to={`/courses/${course.id}/reviews`}>Reseñas</Link>
    </li>)}</ul>}
  </main>;
}
