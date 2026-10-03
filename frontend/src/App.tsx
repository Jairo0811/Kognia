import { FormEvent, useEffect, useState } from 'react';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { api, AuthSession, clearSession, getSession, saveSession } from './lib/api';

type CourseSummary = {
  id: string;
  title: string;
  slug: string;
  summary: string;
  level: string;
  category: string;
  instructorUserId: string;
  sections: number;
  lessons: number;
};

type Category = { id: string; name: string; slug: string; description?: string };

type InstructorCourse = {
  id: string;
  title: string;
  slug: string;
  status: number;
  level: string;
  categoryId: string;
};

function Layout({ children }: { children: React.ReactNode }) {
  const session = getSession();
  const navigate = useNavigate();
  const logout = () => {
    clearSession();
    navigate('/');
  };

  return (
    <>
      <header>
        <nav aria-label="Navegación principal">
          <Link to="/">Kognia</Link>{' '}
          <Link to="/courses">Cursos</Link>{' '}
          {session ? (
            <>
              <Link to="/instructor">Instructor</Link>{' '}
              <button type="button" onClick={logout}>Cerrar sesión</button>
            </>
          ) : (
            <>
              <Link to="/login">Iniciar sesión</Link>{' '}
              <Link to="/register">Crear cuenta</Link>
            </>
          )}
        </nav>
      </header>
      {children}
    </>
  );
}

function HomePage() {
  return (
    <Layout>
      <main>
        <h1>Kognia</h1>
        <p>Aprende. Avanza. Domina.</p>
        <p>Una plataforma de aprendizaje por suscripción para desarrollar habilidades medibles.</p>
        <Link to="/courses">Explorar cursos</Link>
      </main>
    </Layout>
  );
}

function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      const session = await api<AuthSession>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });
      saveSession(session);
      navigate('/');
    } catch {
      setError('No fue posible iniciar sesión. Verifica tus credenciales y la confirmación del correo.');
    }
  }

  return (
    <Layout>
      <main>
        <h1>Iniciar sesión</h1>
        <form onSubmit={submit}>
          <label>Correo electrónico<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
          <label>Contraseña<input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></label>
          <button type="submit">Entrar</button>
        </form>
        {error && <p role="alert">{error}</p>}
        <Link to="/forgot-password">¿Olvidaste tu contraseña?</Link>
      </main>
    </Layout>
  );
}

function RegisterPage() {
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '' });
  const [message, setMessage] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setMessage('');
    try {
      await api('/api/auth/register', { method: 'POST', body: JSON.stringify(form) });
      setMessage('Cuenta creada. Confirma tu correo antes de iniciar sesión.');
    } catch {
      setMessage('No fue posible crear la cuenta.');
    }
  }

  return (
    <Layout>
      <main>
        <h1>Crear cuenta</h1>
        <form onSubmit={submit}>
          <label>Nombre<input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required /></label>
          <label>Apellido<input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required /></label>
          <label>Correo<input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required /></label>
          <label>Contraseña<input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required minLength={8} /></label>
          <button type="submit">Registrarme</button>
        </form>
        {message && <p role="status">{message}</p>}
      </main>
    </Layout>
  );
}

function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  async function submit(event: FormEvent) {
    event.preventDefault();
    await api('/api/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) });
    setMessage('Si la cuenta existe, recibirás instrucciones para restablecer la contraseña.');
  }
  return (
    <Layout>
      <main>
        <h1>Recuperar contraseña</h1>
        <form onSubmit={submit}>
          <label>Correo<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
          <button type="submit">Enviar instrucciones</button>
        </form>
        {message && <p role="status">{message}</p>}
      </main>
    </Layout>
  );
}

function CoursesPage() {
  const [courses, setCourses] = useState<CourseSummary[]>([]);
  const [query, setQuery] = useState('');

  useEffect(() => {
    api<CourseSummary[]>(`/api/catalog/courses${query ? `?q=${encodeURIComponent(query)}` : ''}`)
      .then(setCourses)
      .catch(() => setCourses([]));
  }, [query]);

  return (
    <Layout>
      <main>
        <h1>Catálogo de cursos</h1>
        <label>Buscar cursos<input value={query} onChange={(e) => setQuery(e.target.value)} /></label>
        {courses.length === 0 ? <p>No hay cursos publicados todavía.</p> : (
          <ul>
            {courses.map((course) => (
              <li key={course.id}>
                <h2>{course.title}</h2>
                <p>{course.summary}</p>
                <p>{course.category} · {course.level} · {course.lessons} lecciones</p>
              </li>
            ))}
          </ul>
        )}
      </main>
    </Layout>
  );
}

function InstructorPage() {
  const session = getSession();
  const [courses, setCourses] = useState<InstructorCourse[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [form, setForm] = useState({ title: '', slug: '', summary: '', description: '', level: 'Beginner', categoryId: '' });
  const [message, setMessage] = useState('');

  const canManage = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;

  async function load() {
    const [courseData, categoryData] = await Promise.all([
      api<InstructorCourse[]>('/api/instructor/courses'),
      api<Category[]>('/api/catalog/categories'),
    ]);
    setCourses(courseData);
    setCategories(categoryData);
    if (!form.categoryId && categoryData[0]) setForm((current) => ({ ...current, categoryId: categoryData[0].id }));
  }

  useEffect(() => {
    if (canManage) void load();
  }, [canManage]);

  if (!session) return <Navigate to="/login" replace />;

  if (!canManage) {
    return <Layout><main><h1>Portal del instructor</h1><p>Tu cuenta no tiene el rol de Instructor o Administrator.</p></main></Layout>;
  }

  async function createCourse(event: FormEvent) {
    event.preventDefault();
    setMessage('');
    try {
      await api('/api/instructor/courses', { method: 'POST', body: JSON.stringify(form) });
      setMessage('Curso creado como borrador.');
      setForm({ title: '', slug: '', summary: '', description: '', level: 'Beginner', categoryId: categories[0]?.id ?? '' });
      await load();
    } catch {
      setMessage('No fue posible crear el curso.');
    }
  }

  return (
    <Layout>
      <main>
        <h1>Portal del instructor</h1>
        <section>
          <h2>Crear curso</h2>
          <form onSubmit={createCourse}>
            <label>Título<input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required /></label>
            <label>Slug<input value={form.slug} onChange={(e) => setForm({ ...form, slug: e.target.value })} required /></label>
            <label>Resumen<textarea value={form.summary} onChange={(e) => setForm({ ...form, summary: e.target.value })} required /></label>
            <label>Descripción<textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} required /></label>
            <label>Nivel<select value={form.level} onChange={(e) => setForm({ ...form, level: e.target.value })}><option>Beginner</option><option>Intermediate</option><option>Advanced</option></select></label>
            <label>Categoría<select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
            <button type="submit">Crear borrador</button>
          </form>
          {message && <p role="status">{message}</p>}
        </section>
        <section>
          <h2>Mis cursos</h2>
          {courses.length === 0 ? <p>No tienes cursos todavía.</p> : <ul>{courses.map((course) => <li key={course.id}>{course.title} — {course.status === 1 ? 'Publicado' : 'Borrador'}</li>)}</ul>}
        </section>
      </main>
    </Layout>
  );
}

function NotFoundPage() {
  return <Layout><main><h1>404</h1><p>La página solicitada no existe.</p></main></Layout>;
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/home" element={<Navigate to="/" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/courses" element={<CoursesPage />} />
      <Route path="/instructor" element={<InstructorPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
