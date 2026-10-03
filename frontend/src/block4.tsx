import { FormEvent, useEffect, useState } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { api, getSession } from './lib/api';

type Favorite = { id: string; title: string; slug: string; summary: string; level: string; createdAtUtc: string };
type Review = { id: string; rating: number; comment: string; studentName: string; createdAtUtc: string; updatedAtUtc?: string };
type NotificationItem = { id: string; type: number; title: string; message: string; actionUrl?: string; isRead: boolean; createdAtUtc: string };
type NotificationFeed = { unreadCount: number; items: NotificationItem[] };
type AdminDashboard = { users: number; courses: number; publishedCourses: number; enrollments: number; reviews: number; visibleReviews: number; unreadNotifications: number; certificates: number };
type AdminUser = { id: string; email: string; firstName: string; lastName: string; emailConfirmed: boolean; roles: string[] };
type AdminCourse = { id: string; title: string; slug: string; status: number; instructorUserId: string; category: string; createdAtUtc: string; publishedAtUtc?: string };
type AdminReview = { id: string; courseId: string; courseTitle: string; studentUserId: string; rating: number; comment: string; isVisible: boolean; createdAtUtc: string };

export function FavoritesPage() {
  const session = getSession();
  const [items, setItems] = useState<Favorite[]>([]);
  const [message, setMessage] = useState('');

  async function load() {
    setItems(await api<Favorite[]>('/api/engagement/favorites'));
  }

  useEffect(() => { if (session) void load().catch(() => setMessage('No fue posible cargar tus favoritos.')); }, []);
  if (!session) return <Navigate to="/login" replace />;

  async function remove(courseId: string) {
    await api(`/api/engagement/favorites/${courseId}`, { method: 'DELETE' });
    setMessage('Curso eliminado de favoritos.');
    await load();
  }

  return <main>
    <p><Link to="/">← Inicio</Link></p>
    <h1>Mis favoritos</h1>
    {message && <p role="status">{message}</p>}
    {items.length === 0 ? <p>No tienes cursos guardados.</p> : <ul>{items.map((item) => <li key={item.id}>
      <strong>{item.title}</strong> — {item.level}<p>{item.summary}</p>
      <button type="button" onClick={() => void remove(item.id)}>Quitar de favoritos</button>{' '}
      <Link to={`/courses/${item.id}/reviews`}>Ver reseñas</Link>
    </li>)}</ul>}
  </main>;
}

export function CourseReviewsPage() {
  const { courseId } = useParams();
  const session = getSession();
  const [reviews, setReviews] = useState<Review[]>([]);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [message, setMessage] = useState('');

  async function load() {
    if (!courseId) return;
    setReviews(await api<Review[]>(`/api/engagement/courses/${courseId}/reviews`));
  }

  useEffect(() => { void load().catch(() => setMessage('No fue posible cargar las reseñas.')); }, [courseId]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!courseId || !session) return;
    try {
      await api(`/api/engagement/courses/${courseId}/reviews`, { method: 'POST', body: JSON.stringify({ rating, comment }) });
      setMessage('Reseña guardada.');
      setComment('');
      await load();
    } catch {
      setMessage('Debes estar inscrito en el curso para publicar una reseña.');
    }
  }

  return <main>
    <p><Link to="/courses">← Cursos</Link></p>
    <h1>Reseñas del curso</h1>
    {reviews.length === 0 ? <p>Todavía no hay reseñas visibles.</p> : <ul>{reviews.map((review) => <li key={review.id}>
      <strong>{review.studentName}</strong> · {review.rating}/5<p>{review.comment}</p>
    </li>)}</ul>}
    {session ? <form onSubmit={submit}>
      <h2>Tu reseña</h2>
      <label>Calificación<select value={rating} onChange={(e) => setRating(Number(e.target.value))}>{[5,4,3,2,1].map((value) => <option key={value} value={value}>{value}/5</option>)}</select></label>
      <label>Comentario<textarea value={comment} onChange={(e) => setComment(e.target.value)} maxLength={2000} /></label>
      <button type="submit">Guardar reseña</button>
    </form> : <p><Link to="/login">Inicia sesión para dejar una reseña</Link></p>}
    {message && <p role="status">{message}</p>}
  </main>;
}

export function NotificationsPage() {
  const session = getSession();
  const [feed, setFeed] = useState<NotificationFeed | null>(null);
  const [message, setMessage] = useState('');

  async function load() { setFeed(await api<NotificationFeed>('/api/notifications/')); }
  useEffect(() => { if (session) void load().catch(() => setMessage('No fue posible cargar las notificaciones.')); }, []);
  if (!session) return <Navigate to="/login" replace />;

  async function markRead(id: string) {
    await api(`/api/notifications/${id}/read`, { method: 'POST' });
    await load();
  }
  async function markAll() {
    await api('/api/notifications/read-all', { method: 'POST' });
    setMessage('Todas las notificaciones fueron marcadas como leídas.');
    await load();
  }

  return <main>
    <p><Link to="/">← Inicio</Link></p>
    <h1>Notificaciones</h1>
    <p>Sin leer: {feed?.unreadCount ?? 0}</p>
    <button type="button" onClick={() => void markAll()} disabled={!feed?.unreadCount}>Marcar todas como leídas</button>
    {message && <p role="status">{message}</p>}
    {!feed || feed.items.length === 0 ? <p>No tienes notificaciones.</p> : <ul>{feed.items.map((item) => <li key={item.id}>
      <strong>{item.title}</strong>{item.isRead ? ' · Leída' : ' · Nueva'}<p>{item.message}</p>
      {item.actionUrl && <Link to={item.actionUrl}>Abrir</Link>}{' '}
      {!item.isRead && <button type="button" onClick={() => void markRead(item.id)}>Marcar como leída</button>}
    </li>)}</ul>}
  </main>;
}

export function AdminPage() {
  const session = getSession();
  const isAdmin = session?.roles.includes('Administrator') ?? false;
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [courses, setCourses] = useState<AdminCourse[]>([]);
  const [reviews, setReviews] = useState<AdminReview[]>([]);
  const [message, setMessage] = useState('');
  const [broadcast, setBroadcast] = useState({ title: '', message: '', actionUrl: '/' });

  async function load() {
    const [summary, userData, courseData, reviewData] = await Promise.all([
      api<AdminDashboard>('/api/admin/dashboard'),
      api<AdminUser[]>('/api/admin/users'),
      api<AdminCourse[]>('/api/admin/courses'),
      api<AdminReview[]>('/api/admin/reviews'),
    ]);
    setDashboard(summary); setUsers(userData); setCourses(courseData); setReviews(reviewData);
  }

  useEffect(() => { if (isAdmin) void load().catch(() => setMessage('No fue posible cargar el portal administrativo.')); }, [isAdmin]);
  if (!session) return <Navigate to="/login" replace />;
  if (!isAdmin) return <main><h1>Administración</h1><p>Tu cuenta no tiene permisos de Administrator.</p></main>;

  async function updateRole(userId: string, role: string) {
    await api(`/api/admin/users/${userId}/role`, { method: 'PUT', body: JSON.stringify({ role }) });
    setMessage('Rol actualizado.'); await load();
  }
  async function updateCourseStatus(courseId: string, status: number) {
    await api(`/api/admin/courses/${courseId}/status`, { method: 'PUT', body: JSON.stringify({ status }) });
    setMessage('Estado del curso actualizado.'); await load();
  }
  async function toggleReview(reviewId: string, isVisible: boolean) {
    await api(`/api/admin/reviews/${reviewId}/visibility`, { method: 'PUT', body: JSON.stringify({ isVisible }) });
    setMessage('Visibilidad de la reseña actualizada.'); await load();
  }
  async function sendBroadcast(event: FormEvent) {
    event.preventDefault();
    const result = await api<{ recipients: number }>('/api/admin/notifications/broadcast', { method: 'POST', body: JSON.stringify(broadcast) });
    setMessage(`Notificación enviada a ${result.recipients} usuarios.`);
    setBroadcast({ title: '', message: '', actionUrl: '/' });
  }

  return <main>
    <p><Link to="/">← Inicio</Link></p>
    <h1>Portal administrativo</h1>
    {message && <p role="status">{message}</p>}
    {dashboard && <section><h2>Resumen</h2><dl>
      <dt>Usuarios</dt><dd>{dashboard.users}</dd><dt>Cursos</dt><dd>{dashboard.courses}</dd><dt>Cursos publicados</dt><dd>{dashboard.publishedCourses}</dd>
      <dt>Inscripciones</dt><dd>{dashboard.enrollments}</dd><dt>Certificados</dt><dd>{dashboard.certificates}</dd><dt>Reseñas visibles</dt><dd>{dashboard.visibleReviews}/{dashboard.reviews}</dd>
    </dl></section>}

    <section><h2>Usuarios y roles</h2><ul>{users.map((user) => <li key={user.id}>{user.firstName} {user.lastName} · {user.email} · {user.roles.join(', ') || 'Sin rol'}{' '}
      <select aria-label={`Rol de ${user.email}`} value={user.roles[0] ?? 'Student'} onChange={(e) => void updateRole(user.id, e.target.value)}><option>Student</option><option>Instructor</option><option>Administrator</option></select>
    </li>)}</ul></section>

    <section><h2>Moderación de cursos</h2><ul>{courses.map((course) => <li key={course.id}>{course.title} · {course.category}{' '}
      <select aria-label={`Estado de ${course.title}`} value={course.status} onChange={(e) => void updateCourseStatus(course.id, Number(e.target.value))}><option value={0}>Borrador</option><option value={1}>Publicado</option><option value={2}>Archivado</option></select>
    </li>)}</ul></section>

    <section><h2>Moderación de reseñas</h2>{reviews.length === 0 ? <p>Sin reseñas.</p> : <ul>{reviews.map((review) => <li key={review.id}>{review.courseTitle} · {review.rating}/5 · {review.comment}{' '}
      <button type="button" onClick={() => void toggleReview(review.id, !review.isVisible)}>{review.isVisible ? 'Ocultar' : 'Mostrar'}</button>
    </li>)}</ul>}</section>

    <section><h2>Notificación global</h2><form onSubmit={sendBroadcast}>
      <label>Título<input value={broadcast.title} onChange={(e) => setBroadcast({ ...broadcast, title: e.target.value })} required /></label>
      <label>Mensaje<textarea value={broadcast.message} onChange={(e) => setBroadcast({ ...broadcast, message: e.target.value })} required /></label>
      <label>Ruta de acción<input value={broadcast.actionUrl} onChange={(e) => setBroadcast({ ...broadcast, actionUrl: e.target.value })} /></label>
      <button type="submit">Enviar a todos</button>
    </form></section>
  </main>;
}
