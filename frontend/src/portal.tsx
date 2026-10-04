import { Link, Route, Routes } from 'react-router-dom';
import { App } from './App';
import { BillingPage, InstructorDashboardPage } from './block3';
import { StudentDashboardPage } from './StudentDashboard';
import { AdminPage, CourseReviewsPage, FavoritesPage, NotificationsPage } from './block4';
import {
  AcademicCertificatePreviewPage,
  CertificateDetailPage,
  CertificateVerifyPage,
  CertificatesPage,
} from './certificates';
import { EngagementCatalogPage } from './engagementCatalog';
import { AnalyticsPage } from './finalBlock';
import { KogniaHome } from './KogniaHome';
import { getSession } from './lib/api';

export function Portal() {
  const session = getSession();
  const isInstructor = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;
  const isAdmin = session?.roles.includes('Administrator') ?? false;

  return <>
    <a className="skip-link" href="#main-content">Saltar al contenido principal</a>
    {session && (
      <aside className="account-nav" aria-label="Accesos de cuenta">
        <nav>
          <Link to="/">Inicio</Link>
          <Link to="/discover">Descubrir</Link>
          <Link to="/billing">Planes</Link>
          <Link to="/dashboard">Dashboard</Link>
          <Link to="/analytics">Analíticas</Link>
          <Link to="/certificates">Certificados</Link>
          <Link to="/favorites">Favoritos</Link>
          <Link to="/notifications">Notificaciones</Link>
          {isInstructor && <Link to="/instructor/dashboard">Métricas instructor</Link>}
          {isAdmin && <Link to="/admin">Administración</Link>}
        </nav>
      </aside>
    )}
    <div id="main-content" tabIndex={-1}>
      <Routes>
        <Route path="/" element={<KogniaHome />} />
        <Route path="/discover" element={<EngagementCatalogPage />} />
        <Route path="/billing" element={<BillingPage />} />
        <Route path="/dashboard" element={<StudentDashboardPage />} />
        <Route path="/analytics" element={<AnalyticsPage />} />
        <Route path="/instructor/dashboard" element={<InstructorDashboardPage />} />
        <Route path="/certificates" element={<CertificatesPage />} />
        <Route path="/certificates/view/:verificationCode" element={<CertificateDetailPage />} />
        <Route path="/certificates/verify/:verificationCode" element={<CertificateVerifyPage />} />
        <Route path="/certificates/demo/:studentKey" element={<AcademicCertificatePreviewPage />} />
        <Route path="/favorites" element={<FavoritesPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/courses/:courseId/reviews" element={<CourseReviewsPage />} />
        <Route path="/admin" element={<AdminPage />} />
        <Route path="/*" element={<App />} />
      </Routes>
    </div>
  </>;
}
