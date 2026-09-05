import { StrictMode, type ReactNode } from 'react'
import { createRoot } from 'react-dom/client'
import {
  createBrowserRouter,
  Navigate,
  RouterProvider,
} from 'react-router-dom'
import './index.css'
import './styles.css'
import { AuthProvider, useAuth } from './auth'
import { StoreProvider } from './store'
import { Layout } from './components/Layout'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { ImoveisPage } from './pages/ImoveisPage'
import { InquilinosPage } from './pages/InquilinosPage'
import { RecebimentosPage } from './pages/RecebimentosPage'

function RequireAuth({ children }: { children: ReactNode }) {
  const { usuario } = useAuth()
  if (!usuario) {
    return <Navigate to="/login" replace />
  }
  return children
}

const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <Layout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'imoveis', element: <ImoveisPage /> },
      { path: 'inquilinos', element: <InquilinosPage /> },
      { path: 'recebimentos', element: <RecebimentosPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <StoreProvider>
        <RouterProvider router={router} />
      </StoreProvider>
    </AuthProvider>
  </StrictMode>,
)
