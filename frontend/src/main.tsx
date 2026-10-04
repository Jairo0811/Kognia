import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { Portal } from './portal';
import './accessibility.css';
import './kognia-ui.css';
import './kognia-overrides.css';
import './kognia-home-sections.css';
import './kognia-product.css';
import './student-dashboard-layout-fix.css';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Portal />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
