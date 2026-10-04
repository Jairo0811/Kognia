import { Link } from 'react-router-dom';
import { getSession } from './lib/api';

const courses = [
  { icon: '⚛', title: 'React 19 y TypeScript', meta: '12h 30m · Intermedio', accent: 'cyan' },
  { icon: '.NET', title: '.NET 10 y ASP.NET Core', meta: '14h 20m · Intermedio', accent: 'violet' },
  { icon: 'DB', title: 'SQL Server desde cero', meta: '10h 15m · Inicial', accent: 'blue' },
  { icon: 'AI', title: 'Inteligencia Artificial', meta: '15h 10m · Intermedio', accent: 'indigo' },
];

const features = [
  ['01', 'Aprende a tu ritmo', 'Lecciones organizadas, progreso medible y continuidad entre sesiones.'],
  ['02', 'Practica y demuestra', 'Evaluaciones automáticas, intentos, resultados y certificados verificables.'],
  ['03', 'Crece con datos', 'Dashboards para estudiantes, instructores y administradores con métricas reales.'],
];

const plans = [
  {
    name: 'Free',
    price: 'RD$0',
    cadence: '/mes',
    description: 'Empieza con acceso básico y descubre la experiencia Kognia.',
    features: ['Catálogo público', 'Cursos gratuitos seleccionados', 'Progreso básico', 'Cuenta de estudiante'],
  },
  {
    name: 'Premium Mensual',
    price: 'RD$699',
    cadence: '/mes',
    description: 'Acceso completo para aprender, evaluar y certificar tus habilidades.',
    features: ['Acceso Premium', 'Evaluaciones', 'Certificados verificables', 'Dashboards y progreso', 'Favoritos y notificaciones'],
    featured: true,
  },
  {
    name: 'Premium Anual',
    price: 'RD$6,999',
    cadence: '/año',
    description: 'Todo Premium con facturación anual y mejor precio efectivo.',
    features: ['Todo lo de Premium Mensual', 'Pago anual preferencial', 'Acceso continuo durante 12 meses', 'Historial de pagos'],
  },
];

export function KogniaHome() {
  const session = getSession();

  return (
    <main className="k-home" id="main-content">
      <section className="k-hero" aria-labelledby="k-hero-title">
        <div className="k-glow k-glow-a" />
        <div className="k-glow k-glow-b" />
        <div className="k-shell">
          <header className="k-topbar">
            <a className="k-brand" href="#inicio" aria-label="Kognia, inicio">
              <span className="k-mark" aria-hidden="true"><span>K</span></span>
              <span className="k-wordmark">Kognia</span>
            </a>
            <nav className="k-main-nav" aria-label="Navegación principal">
              <a href="#inicio">Inicio</a>
              <a href="#cursos">Cursos</a>
              <a href="#planes">Planes</a>
              {session && <Link to="/dashboard">Mi aprendizaje</Link>}
            </nav>
            <div className="k-nav-actions">
              {session ? (
                <Link className="k-btn k-btn-primary" to="/dashboard">Ir al dashboard</Link>
              ) : (
                <>
                  <Link className="k-login" to="/login">Iniciar sesión</Link>
                  <Link className="k-btn k-btn-primary" to="/register">Regístrate</Link>
                </>
              )}
            </div>
          </header>

          <div className="k-hero-grid" id="inicio">
            <div className="k-hero-copy">
              <span className="k-eyebrow">PLATAFORMA DE APRENDIZAJE ONLINE</span>
              <h1 id="k-hero-title">Aprende hoy,<br />construye tu <span>mañana</span></h1>
              <p className="k-lead">Cursos en tecnología, negocios, diseño y más. Aprende a tu ritmo, mide tu progreso y convierte conocimiento en resultados.</p>
              <div className="k-cta-row">
                <a className="k-btn k-btn-primary k-btn-lg" href="#cursos">Explorar cursos <span>→</span></a>
                <a className="k-btn k-btn-ghost k-btn-lg" href="#planes">Ver planes</a>
              </div>
              <div className="k-stats" aria-label="Métricas de Kognia">
                <div><strong>500+</strong><span>Cursos</span></div>
                <div><strong>120+</strong><span>Instructores</span></div>
                <div><strong>10K+</strong><span>Estudiantes</span></div>
                <div><strong>95%</strong><span>Satisfacción</span></div>
              </div>
            </div>

            <div className="k-hero-visual" aria-label="Vista previa de la experiencia Kognia">
              <div className="k-orbit k-orbit-1" />
              <div className="k-orbit k-orbit-2" />
              <div className="k-laptop">
                <div className="k-laptop-screen">
                  <div className="k-mini-bar"><span className="k-mini-logo">K</span><span>Tu aprendizaje</span><i /></div>
                  <div className="k-mini-course">
                    <span className="k-course-icon cyan">⚛</span>
                    <div><b>React 19 y TypeScript</b><small>37% completado</small><div className="k-progress"><i style={{ width: '37%' }} /></div></div>
                  </div>
                  <div className="k-mini-course">
                    <span className="k-course-icon violet">.N</span>
                    <div><b>.NET 10 y ASP.NET Core</b><small>68% completado</small><div className="k-progress"><i style={{ width: '68%' }} /></div></div>
                  </div>
                  <div className="k-mini-course">
                    <span className="k-course-icon blue">DB</span>
                    <div><b>SQL Server desde cero</b><small>28% completado</small><div className="k-progress"><i style={{ width: '28%' }} /></div></div>
                  </div>
                </div>
              </div>
              <div className="k-floating-card k-float-a"><span>✦</span><div><b>Aprende</b><small>a tu manera</small></div></div>
              <div className="k-floating-card k-float-b"><span>✓</span><div><b>Certifica</b><small>tu progreso</small></div></div>
            </div>
          </div>
        </div>
      </section>

      <section className="k-section k-featured" id="cursos">
        <div className="k-shell">
          <div className="k-section-heading">
            <div><span className="k-eyebrow">EXPLORA Y AVANZA</span><h2>Cursos destacados</h2></div>
            <span className="k-section-note">Todo empieza aquí, sin salir de la Home.</span>
          </div>
          <div className="k-course-grid">
            {courses.map((course) => (
              <article className="k-course-card" key={course.title}>
                <div className={`k-course-art ${course.accent}`}><span>{course.icon}</span><i /></div>
                <div className="k-course-body">
                  <span className="k-chip">TECNOLOGÍA</span>
                  <h3>{course.title}</h3>
                  <p>{course.meta}</p>
                  <div className="k-course-footer"><span>★ 4.8</span><Link to="/discover">Ver detalles</Link></div>
                </div>
              </article>
            ))}
          </div>
          <div className="k-inline-action">
            <p>¿Quieres explorar todo el catálogo con filtros, favoritos y reseñas?</p>
            <Link className="k-btn k-btn-ghost" to="/discover">Abrir catálogo completo</Link>
          </div>
        </div>
      </section>

      <section className="k-section k-pricing" id="planes">
        <div className="k-shell">
          <div className="k-section-heading centered">
            <div>
              <span className="k-eyebrow">ELIGE TU RITMO</span>
              <h2>Planes simples para aprender más</h2>
              <p>Comienza gratis o desbloquea la experiencia Premium de Kognia.</p>
            </div>
          </div>
          <div className="k-plan-grid">
            {plans.map((plan) => (
              <article className={`k-plan-card${plan.featured ? ' is-featured' : ''}`} key={plan.name}>
                {plan.featured && <span className="k-plan-badge">MÁS POPULAR</span>}
                <span className="k-chip">{plan.name.toUpperCase()}</span>
                <div className="k-plan-price"><strong>{plan.price}</strong><span>{plan.cadence}</span></div>
                <p>{plan.description}</p>
                <ul>
                  {plan.features.map((feature) => <li key={feature}>✓ {feature}</li>)}
                </ul>
                <Link className={`k-btn ${plan.featured ? 'k-btn-primary' : 'k-btn-ghost'} k-plan-cta`} to={session ? '/billing' : '/register'}>
                  {session ? 'Gestionar plan' : plan.price === 'RD$0' ? 'Comenzar gratis' : 'Elegir plan'}
                </Link>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="k-section k-how">
        <div className="k-shell">
          <div className="k-section-heading centered"><div><span className="k-eyebrow">APRENDE · CONECTA · CRECE</span><h2>Todo tu aprendizaje en un solo lugar</h2><p>Una experiencia coherente desde el primer curso hasta tu certificado.</p></div></div>
          <div className="k-feature-grid">
            {features.map(([num, title, text]) => <article className="k-feature-card" key={num}><span>{num}</span><h3>{title}</h3><p>{text}</p></article>)}
          </div>
        </div>
      </section>

      <section className="k-section k-final-cta">
        <div className="k-shell"><div className="k-cta-panel"><div><span className="k-eyebrow">TU SIGUIENTE NIVEL EMPIEZA AQUÍ</span><h2>Convierte curiosidad en progreso.</h2><p>Explora, aprende, evalúate y certifica tus habilidades dentro de Kognia.</p></div><Link className="k-btn k-btn-primary k-btn-lg" to={session ? '/dashboard' : '/register'}>{session ? 'Continuar aprendiendo' : 'Crear cuenta gratis'} →</Link></div></div>
      </section>

      <footer className="k-footer"><div className="k-shell"><a className="k-brand" href="#inicio"><span className="k-mark"><span>K</span></span><span className="k-wordmark">Kognia</span></a><p>Aprende · Conecta · Crece</p><span>© 2026 Kognia</span></div></footer>
    </main>
  );
}
