import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './styles/global.css'
import './styles/components.css'
import './styles/dashboard.css'
import './styles/closings.css'
import './styles/balance.css'
import './styles/indicators.css'
import './styles/balanco.css'
import './styles/filters.css'
import './styles/iaPlanning.css'
import './styles/management.css'
import './styles/login.css'
import App from './App.tsx'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
    },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
)
