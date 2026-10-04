import { NavLink, Route, Routes } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBell,
  faBookOpen,
  faCertificate,
  faChartLine,
  faCompass,
  faGaugeHigh,
  faHeart,
  faHouse,
  faLayerGroup,
  faShieldHalved,
  faTags,
} from '@fortawesome/free-solid-svg-icons';
import { App } from './App';
import { BillingPage, InstructorDashboardPage } from './block3';
import { StudentDashboardPage } from './StudentDashboard';
import { AdminPage, CourseReviewsPage, FavoritesPage, NotificationsPage } from './block4';
import { CertificateVerifyPage, CertificatesPage } from './certificates';
import { AcademicCertificatePreviewPageV3, CertificateDetailPageV3 } from './certificate-document-v3';
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
        <NavLink className="account-nav-brand" to="/" end aria-label="Kognia, inicio">
          <img src="/branding/kognia-logo.png" alt="Kognia" />
        </NavLink>
        <nav>
          <NavLink to="/" end><FontAwesomeIcon icon={faHouse} /><span>Inicio</span></NavLink>
          <NavLink to="/discover"><FontAwesomeIcon icon={faCompass} /><span>Descubrir</span></NavLink>
          <NavLink to="/billing"><FontAwesomeIcon icon={faTags} /><span>Planes</span></NavLink>
          <NavLink to="/dashboard"><FontAwesomeIcon icon={faGaugeHigh} /><span>Dashboard</span></NavLink>
          <NavLink to="/analytics"><FontAwesomeIcon icon={faChartLine} /><span>Analíticas</span></NavLink>
          <NavLink to="/certificates"><FontAwesomeIcon icon={faCertificate} /><span>Certificados</span></NavLink>
          <NavLink to="/favorites"><FontAwesomeIcon icon={faHeart} /><span>Favoritos</span></NavLink>
          <NavLink to="/notifications"><FontAwesomeIcon icon={faBell} /><span>Notificaciones</span></NavLink>
          {isInstructor && <NavLink to="/instructor/dashboard"><FontAwesomeIcon icon={faBookOpen} /><span>Métricas instructor</span></NavLink>}
          {isAdmin && <NavLink to="/admin"><FontAwesomeIcon icon={faShieldHalved} /><span>Administración</span></NavLink>}
        </nav>
        <div className="account-nav-footer" aria-hidden="true">
          <FontAwesomeIcon icon={faLayerGroup} />
          <span>Aprende · Conecta · Crece</span>
        </div>
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
        <Route path="/certificates/view/:verificationCode" element={<CertificateDetailPageV3 />} />
        <Route path="/certificates/verify/:verificationCode" element={<CertificateVerifyPage />} />
        <Route path="/certificates/demo/:studentKey" element={<AcademicCertificatePreviewPageV3 />} />
        <Route path="/favorites" element={<FavoritesPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/courses/:courseId/reviews" element={<CourseReviewsPage />} />
        <Route path="/admin" element={<AdminPage />} />
        <Route path="/*" element={<App />} />
      </Routes>
    </div>
  </>;
}
