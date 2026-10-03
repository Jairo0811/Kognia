import { Link, Route, Routes } from 'react-router-dom';
import { App } from './App';
import { BillingPage, InstructorDashboardPage, StudentDashboardPage } from './block3';
import { AdminPage, CourseReviewsPage, FavoritesPage, NotificationsPage } from './block4';
import { getSession } from './lib/api';

export function Portal() {
  const session = getSession();
  const isInstructor = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;
  const isAdmin = session?.roles.includes('Administrator') ?? false;

  return <>
    <aside aria-label="Accesos de cuenta">
      <nav>
        <Link to="/billing">Planes</Link>{' '}
        {session && <><Link to="/dashboard">Dashboard</Link>{' '}<Link to="/favorites">Favoritos</Link>{' '}<Link to="/notifications">Notificaciones</Link>{' '}</>}
        {isInstructor && <><Link to="/instructor/dashboard">Métricas instructor</Link>{' '}</>}
        {isAdmin && <Link to="/admin">Administración</Link>}
      </nav>
    </aside>
    <Routes>
      <Route path="/billing" element={<BillingPage />} />
      <Route path="/dashboard" element={<StudentDashboardPage />} />
      <Route path="/instructor/dashboard" element={<InstructorDashboardPage />} />
      <Route path="/favorites" element={<FavoritesPage />} />
      <Route path="/notifications" element={<NotificationsPage />} />
      <Route path="/courses/:courseId/reviews" element={<CourseReviewsPage />} />
      <Route path="/admin" element={<AdminPage />} />
      <Route path="/*" element={<App />} />
    </Routes>
  </>;
}
