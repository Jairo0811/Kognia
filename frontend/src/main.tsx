import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { App } from './App';
import { BillingPage, InstructorDashboardPage, StudentDashboardPage } from './block3';

const queryClient = new QueryClient();

function RootRoutes() {
  return (
    <Routes>
      <Route path="/billing" element={<BillingPage />} />
      <Route path="/dashboard/student" element={<StudentDashboardPage />} />
      <Route path="/dashboard/instructor" element={<InstructorDashboardPage />} />
      <Route path="*" element={<App />} />
    </Routes>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <RootRoutes />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
