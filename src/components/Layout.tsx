import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/imoveis', label: 'Imóveis', end: false },
  { to: '/inquilinos', label: 'Inquilinos', end: false },
  { to: '/recebimentos', label: 'Recebimentos', end: false },
]

export function Layout() {
  const { usuario, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="app-shell">
      <header className="header">
        <div className="header-content">
          <div className="header-brand">
            <img src="/favicon.svg" alt="Lucrare" />
            <span>LUCRARE · APP ALUGUEL</span>
          </div>
          <nav className="nav">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  isActive ? 'nav-link active' : 'nav-link'
                }
              >
                {item.label}
              </NavLink>
            ))}
            <button
              type="button"
              className="logout-btn"
              onClick={handleLogout}
              title={usuario ? `Sair (${usuario})` : 'Sair'}
            >
              Sair
            </button>
          </nav>
        </div>
      </header>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
