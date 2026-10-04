import { useEffect, useMemo, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { api, getSession } from './lib/api';
import './student-dashboard.css';

type StudentDashboard = {
  stats: {
    activeCourses: number;
    completedCourses: number;
    completedLessons: number;
    certificateCount: number;
    quizAttempts: number;
    passedQuizAttempts: number;
    averageProgressPercent: number;
  };
  subscription?: { code: string; name: string; currentPeriodEndUtc: string; cancelAtPeriodEnd: boolean } | null;
  courses: Array<{
    enrollmentId: string;
    courseId: string;
    title: string;
    slug: string;
    status: number;
    totalLessons: number;
    completedLessons: number;
    progressPercent: number;
  }>;
  recentActivity: Array<{
    lessonId: string;
    lessonTitle: string;
    courseTitle: string;
    isCompleted: boolean;
    lastAccessedAtUtc: string;
  }>;
};

type StatCard = {
  key: keyof StudentDashboard['stats'];
  label: string;
  icon: string;
  tone: string;
  suffix?: string;
};

const statCards: StatCard[] = [
  { key: 'activeCourses', label: 'Cursos activos', icon: '▰', tone: 'cyan' },
  { key: 'completedCourses', label: 'Cursos completados', icon: '✓', tone: 'violet' },
  { key: 'completedLessons', label: 'Lecciones completadas', icon: '▶', tone: 'blue' },
  { key: 'averageProgressPercent', label: 'Progreso promedio', icon: '↗', tone: 'indigo', suffix: '%' },
  { key: 'certificateCount', label: 'Certificados', icon: '◆', tone: 'gold' },
];

export function StudentDashboardPage() {
  const session = getSession();
  const [data, setData] = useState<StudentDashboard | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!session) return;
    api<StudentDashboard>('/api/dashboard/student')
      .then(setData)
      .catch(() => setError('No fue posible cargar tu progreso. Intenta nuevamente.'));
  }, []);

  const firstName = useMemo(() => {
    const raw = session?.email?.split('@')[0] ?? '';
    if (!raw) return 'Estudiante';
    return raw.charAt(0).toUpperCase() + raw.slice(1);
  }, [session?.email]);

  if (!session) return <Navigate to="/login" replace />;

  return (
    <main className="student-dashboard-shell">
      <section className="student-dashboard-hero" aria-labelledby="student-dashboard-title">
        <div>
          <Link className="student-dashboard-back" to="/">← Volver al inicio</Link>
          <p className="student-dashboard-eyebrow">Panel de aprendizaje</p>
          <h1 id="student-dashboard-title">Hola, {firstName}</h1>
          <p className="student-dashboard-subtitle">
            Sigue avanzando, revisa tu progreso y continúa donde lo dejaste.
          </p>
        </div>
        <div className="student-dashboard-hero-actions">
          <Link className="student-dashboard-btn student-dashboard-btn-secondary" to="/billing">Ver planes</Link>
          <Link className="student-dashboard-btn student-dashboard-btn-primary" to="/discover">Explorar cursos</Link>
        </div>
      </section>

      {error && <p className="student-dashboard-alert" role="alert">{error}</p>}

      {!data ? (
        <section className="student-dashboard-loading" aria-live="polite">
          <div className="student-dashboard-spinner" />
          <p>Cargando tu espacio de aprendizaje...</p>
        </section>
      ) : (
        <>
          <section className="student-dashboard-overview" aria-label="Resumen de progreso">
            <div className="student-dashboard-overview-head">
              <div>
                <p className="student-dashboard-kicker">Tu rendimiento</p>
                <h2>Resumen general</h2>
              </div>
              <span className={`student-dashboard-plan ${data.subscription ? 'is-active' : ''}`}>
                <span className="student-dashboard-plan-dot" />
                {data.subscription?.name ?? 'Sin suscripción activa'}
              </span>
            </div>

            <div className="student-dashboard-stats">
              {statCards.map((stat) => {
                const value = data.stats[stat.key];
                return (
                  <article className={`student-dashboard-stat tone-${stat.tone}`} key={stat.key}>
                    <span className="student-dashboard-stat-icon" aria-hidden="true">{stat.icon}</span>
                    <div>
                      <span className="student-dashboard-stat-label">{stat.label}</span>
                      <strong>{value}{stat.suffix ?? ''}</strong>
                    </div>
                  </article>
                );
              })}

              <article className="student-dashboard-stat tone-green">
                <span className="student-dashboard-stat-icon" aria-hidden="true">★</span>
                <div>
                  <span className="student-dashboard-stat-label">Evaluaciones aprobadas</span>
                  <strong>{data.stats.passedQuizAttempts} <small>/ {data.stats.quizAttempts}</small></strong>
                </div>
              </article>
            </div>
          </section>

          <div className="student-dashboard-grid">
            <section className="student-dashboard-panel student-dashboard-courses">
              <div className="student-dashboard-panel-head">
                <div>
                  <p className="student-dashboard-kicker">Aprendizaje</p>
                  <h2>Continúa aprendiendo</h2>
                </div>
                {data.courses.length > 0 && <Link to="/my-learning">Ver todos</Link>}
              </div>

              {data.courses.length === 0 ? (
                <div className="student-dashboard-empty">
                  <div className="student-dashboard-empty-icon" aria-hidden="true">K</div>
                  <h3>Tu próxima habilidad empieza aquí</h3>
                  <p>Aún no estás inscrito en ningún curso. Explora el catálogo y empieza tu primera ruta de aprendizaje.</p>
                  <Link className="student-dashboard-btn student-dashboard-btn-primary" to="/discover">Descubrir cursos</Link>
                </div>
              ) : (
                <div className="student-dashboard-course-list">
                  {data.courses.map((course) => (
                    <article className="student-dashboard-course-card" key={course.enrollmentId}>
                      <div className="student-dashboard-course-thumb" aria-hidden="true">
                        <span>K</span>
                      </div>
                      <div className="student-dashboard-course-body">
                        <div className="student-dashboard-course-title-row">
                          <h3>{course.title}</h3>
                          <strong>{course.progressPercent}%</strong>
                        </div>
                        <p>{course.completedLessons} de {course.totalLessons} lecciones completadas</p>
                        <div className="student-dashboard-progress" aria-label={`Progreso de ${course.title}: ${course.progressPercent}%`}>
                          <span style={{ width: `${Math.min(100, Math.max(0, course.progressPercent))}%` }} />
                        </div>
                        <Link className="student-dashboard-course-link" to={`/learn/${course.courseId}`}>Continuar curso →</Link>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>

            <aside className="student-dashboard-side">
              <section className="student-dashboard-panel student-dashboard-progress-card">
                <p className="student-dashboard-kicker">Meta actual</p>
                <h2>Progreso promedio</h2>
                <div className="student-dashboard-progress-number">{data.stats.averageProgressPercent}%</div>
                <div className="student-dashboard-progress student-dashboard-progress-large" aria-hidden="true">
                  <span style={{ width: `${Math.min(100, Math.max(0, data.stats.averageProgressPercent))}%` }} />
                </div>
                <p className="student-dashboard-muted">
                  {data.stats.activeCourses > 0
                    ? 'Mantén el ritmo y completa una lección más hoy.'
                    : 'Inscríbete en un curso para comenzar a medir tu avance.'}
                </p>
              </section>

              <section className="student-dashboard-panel student-dashboard-quick-links">
                <p className="student-dashboard-kicker">Accesos rápidos</p>
                <h2>Tu espacio Kognia</h2>
                <nav aria-label="Accesos rápidos del estudiante">
                  <Link to="/certificates"><span>◆</span><div><strong>Certificados</strong><small>Consulta tus logros</small></div><b>→</b></Link>
                  <Link to="/favorites"><span>♥</span><div><strong>Favoritos</strong><small>Contenido guardado</small></div><b>→</b></Link>
                  <Link to="/notifications"><span>●</span><div><strong>Notificaciones</strong><small>Novedades de tu cuenta</small></div><b>→</b></Link>
                </nav>
              </section>
            </aside>
          </div>

          <section className="student-dashboard-panel student-dashboard-activity">
            <div className="student-dashboard-panel-head">
              <div>
                <p className="student-dashboard-kicker">Historial</p>
                <h2>Actividad reciente</h2>
              </div>
            </div>

            {data.recentActivity.length === 0 ? (
              <div className="student-dashboard-activity-empty">
                <span aria-hidden="true">◌</span>
                <p>Tu actividad aparecerá aquí cuando empieces a avanzar en tus cursos.</p>
              </div>
            ) : (
              <div className="student-dashboard-timeline">
                {data.recentActivity.map((item) => (
                  <article key={`${item.lessonId}-${item.lastAccessedAtUtc}`}>
                    <span className={`student-dashboard-timeline-dot ${item.isCompleted ? 'is-complete' : ''}`} />
                    <div>
                      <strong>{item.lessonTitle}</strong>
                      <p>{item.courseTitle}</p>
                    </div>
                    <div className="student-dashboard-timeline-meta">
                      <span>{item.isCompleted ? 'Completada' : 'En progreso'}</span>
                      <time dateTime={item.lastAccessedAtUtc}>
                        {new Date(item.lastAccessedAtUtc).toLocaleDateString('es-DO', { day: '2-digit', month: 'short' })}
                      </time>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </main>
  );
}
