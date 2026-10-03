import { useEffect, useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { api, getSession } from './lib/api';

type Plan = {
  id: string;
  code: string;
  name: string;
  description: string;
  billingPeriod: number;
  priceAmount: number;
  currency: string;
};

type BillingSnapshot = {
  subscription?: {
    id: string;
    status: number;
    currentPeriodEndUtc: string;
    cancelAtPeriodEnd: boolean;
    plan: { code: string; name: string; priceAmount: number; currency: string };
  } | null;
  payments: Array<{ id: string; amount: number; currency: string; status: number; provider: string; createdAtUtc: string; paidAtUtc?: string }>;
  invoices: Array<{ id: string; number: string; amount: number; currency: string; status: number; issuedAtUtc: string; paidAtUtc?: string }>;
};

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

type InstructorDashboard = {
  stats: {
    totalCourses: number;
    publishedCourses: number;
    draftCourses: number;
    totalEnrollments: number;
    completedEnrollments: number;
    completionRatePercent: number;
    certificateCount: number;
    quizAttempts: number;
    passedQuizAttempts: number;
    quizPassRatePercent: number;
  };
  courses: Array<{
    id: string;
    title: string;
    status: number;
    enrollments: number;
    completions: number;
    certificates: number;
    quizAttempts: number;
    passedQuizAttempts: number;
  }>;
};

function money(value: number, currency: string) {
  return new Intl.NumberFormat('es-DO', { style: 'currency', currency }).format(value);
}

export function BillingPage() {
  const session = getSession();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [billing, setBilling] = useState<BillingSnapshot | null>(null);
  const [checkoutToken, setCheckoutToken] = useState('');
  const [message, setMessage] = useState('');

  async function loadBilling() {
    if (!session) return;
    setBilling(await api<BillingSnapshot>('/api/billing/me'));
  }

  useEffect(() => {
    api<Plan[]>('/api/billing/plans').then(setPlans).catch(() => setMessage('No fue posible cargar los planes.'));
    if (session) void loadBilling();
  }, []);

  async function subscribe(planId: string) {
    if (!session) return;
    setMessage('');
    try {
      const result = await api<{ requiresPayment: boolean; checkoutToken?: string }>('/api/billing/checkout', {
        method: 'POST',
        body: JSON.stringify({ planId }),
      });
      if (result.requiresPayment && result.checkoutToken) {
        setCheckoutToken(result.checkoutToken);
        setMessage('Checkout creado. En desarrollo puedes completar el pago con el botón de prueba.');
      } else {
        setMessage('Suscripción activada.');
        await loadBilling();
      }
    } catch {
      setMessage('No fue posible iniciar la suscripción.');
    }
  }

  async function completeSandbox() {
    if (!checkoutToken) return;
    try {
      await api(`/api/billing/sandbox/checkout/${checkoutToken}/complete`, { method: 'POST' });
      setCheckoutToken('');
      setMessage('Pago de prueba completado y suscripción activada.');
      await loadBilling();
    } catch {
      setMessage('El pago sandbox solo está disponible en Development/Testing.');
    }
  }

  async function cancelSubscription() {
    try {
      await api('/api/billing/subscription/cancel', { method: 'POST' });
      setMessage('La suscripción se cancelará al final del período actual.');
      await loadBilling();
    } catch {
      setMessage('No fue posible programar la cancelación.');
    }
  }

  return (
    <main>
      <p><Link to="/">← Inicio</Link></p>
      <h1>Planes y suscripción</h1>
      <section>
        <h2>Planes</h2>
        <ul>
          {plans.map((plan) => (
            <li key={plan.id}>
              <h3>{plan.name}</h3>
              <p>{plan.description}</p>
              <p>{money(plan.priceAmount, plan.currency)} · {plan.billingPeriod === 1 ? 'anual' : 'mensual'}</p>
              {session ? <button type="button" onClick={() => void subscribe(plan.id)}>Elegir plan</button> : <Link to="/login">Inicia sesión para suscribirte</Link>}
            </li>
          ))}
        </ul>
      </section>

      {checkoutToken && (
        <section>
          <h2>Pago sandbox</h2>
          <p>Token: <code>{checkoutToken}</code></p>
          <button type="button" onClick={() => void completeSandbox()}>Completar pago de prueba</button>
        </section>
      )}

      {billing?.subscription && (
        <section>
          <h2>Suscripción actual</h2>
          <p>{billing.subscription.plan.name} · {money(billing.subscription.plan.priceAmount, billing.subscription.plan.currency)}</p>
          <p>Válida hasta: {new Date(billing.subscription.currentPeriodEndUtc).toLocaleDateString('es-DO')}</p>
          <p>{billing.subscription.cancelAtPeriodEnd ? 'Cancelación programada.' : 'Renovación activa.'}</p>
          {!billing.subscription.cancelAtPeriodEnd && <button type="button" onClick={() => void cancelSubscription()}>Cancelar al final del período</button>}
        </section>
      )}

      {billing && (
        <section>
          <h2>Historial de pagos</h2>
          {billing.payments.length === 0 ? <p>Sin pagos registrados.</p> : (
            <ul>{billing.payments.map((payment) => <li key={payment.id}>{money(payment.amount, payment.currency)} · estado {payment.status} · {payment.provider}</li>)}</ul>
          )}
          <h2>Facturas</h2>
          {billing.invoices.length === 0 ? <p>Sin facturas registradas.</p> : (
            <ul>{billing.invoices.map((invoice) => <li key={invoice.id}>{invoice.number} · {money(invoice.amount, invoice.currency)} · estado {invoice.status}</li>)}</ul>
          )}
        </section>
      )}
      {message && <p role="status">{message}</p>}
    </main>
  );
}

export function StudentDashboardPage() {
  const session = getSession();
  const [data, setData] = useState<StudentDashboard | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!session) return;
    api<StudentDashboard>('/api/dashboard/student').then(setData).catch(() => setError('No fue posible cargar el dashboard.'));
  }, []);

  if (!session) return <Navigate to="/login" replace />;

  return (
    <main>
      <p><Link to="/">← Inicio</Link></p>
      <h1>Dashboard del estudiante</h1>
      {error && <p role="alert">{error}</p>}
      {!data ? <p>Cargando...</p> : (
        <>
          <section>
            <h2>Resumen</h2>
            <dl>
              <dt>Cursos activos</dt><dd>{data.stats.activeCourses}</dd>
              <dt>Cursos completados</dt><dd>{data.stats.completedCourses}</dd>
              <dt>Lecciones completadas</dt><dd>{data.stats.completedLessons}</dd>
              <dt>Progreso promedio</dt><dd>{data.stats.averageProgressPercent}%</dd>
              <dt>Certificados</dt><dd>{data.stats.certificateCount}</dd>
              <dt>Evaluaciones aprobadas</dt><dd>{data.stats.passedQuizAttempts} / {data.stats.quizAttempts}</dd>
            </dl>
            <p>Plan: {data.subscription?.name ?? 'Sin suscripción activa'}</p>
          </section>
          <section>
            <h2>Mis cursos</h2>
            {data.courses.length === 0 ? <p>Aún no estás inscrito en cursos.</p> : (
              <ul>{data.courses.map((course) => (
                <li key={course.enrollmentId}>
                  <strong>{course.title}</strong> — {course.progressPercent}% ({course.completedLessons}/{course.totalLessons}){' '}
                  <Link to={`/learn/${course.courseId}`}>Continuar</Link>
                </li>
              ))}</ul>
            )}
          </section>
          <section>
            <h2>Actividad reciente</h2>
            {data.recentActivity.length === 0 ? <p>Sin actividad todavía.</p> : (
              <ul>{data.recentActivity.map((item) => <li key={`${item.lessonId}-${item.lastAccessedAtUtc}`}>{item.courseTitle} · {item.lessonTitle} · {item.isCompleted ? 'Completada' : 'En progreso'}</li>)}</ul>
            )}
          </section>
        </>
      )}
    </main>
  );
}

export function InstructorDashboardPage() {
  const session = getSession();
  const canManage = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;
  const [data, setData] = useState<InstructorDashboard | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!canManage) return;
    api<InstructorDashboard>('/api/dashboard/instructor').then(setData).catch(() => setError('No fue posible cargar las métricas.'));
  }, [canManage]);

  if (!session) return <Navigate to="/login" replace />;
  if (!canManage) return <Navigate to="/dashboard/student" replace />;

  return (
    <main>
      <p><Link to="/instructor">← Portal del instructor</Link></p>
      <h1>Dashboard del instructor</h1>
      {error && <p role="alert">{error}</p>}
      {!data ? <p>Cargando...</p> : (
        <>
          <section>
            <h2>Métricas</h2>
            <dl>
              <dt>Cursos</dt><dd>{data.stats.totalCourses}</dd>
              <dt>Publicados</dt><dd>{data.stats.publishedCourses}</dd>
              <dt>Borradores</dt><dd>{data.stats.draftCourses}</dd>
              <dt>Inscripciones</dt><dd>{data.stats.totalEnrollments}</dd>
              <dt>Finalizaciones</dt><dd>{data.stats.completedEnrollments} ({data.stats.completionRatePercent}%)</dd>
              <dt>Certificados emitidos</dt><dd>{data.stats.certificateCount}</dd>
              <dt>Tasa de aprobación</dt><dd>{data.stats.quizPassRatePercent}%</dd>
            </dl>
          </section>
          <section>
            <h2>Rendimiento por curso</h2>
            {data.courses.length === 0 ? <p>No hay cursos para analizar.</p> : (
              <table>
                <thead><tr><th>Curso</th><th>Inscritos</th><th>Completados</th><th>Certificados</th><th>Intentos</th><th>Aprobados</th></tr></thead>
                <tbody>{data.courses.map((course) => (
                  <tr key={course.id}><td>{course.title}</td><td>{course.enrollments}</td><td>{course.completions}</td><td>{course.certificates}</td><td>{course.quizAttempts}</td><td>{course.passedQuizAttempts}</td></tr>
                ))}</tbody>
              </table>
            )}
          </section>
        </>
      )}
    </main>
  );
}
