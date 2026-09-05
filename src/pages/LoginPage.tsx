import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [usuario, setUsuario] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    if (login(usuario, senha)) {
      setErro('')
      navigate('/')
    } else {
      setErro('Informe usuário e senha para entrar.')
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-logo">
          <img src="/favicon.svg" alt="Lucrare" width={56} height={56} />
          <span className="brand">LUCRARE</span>
        </div>
        <h2>APP Aluguel</h2>
        <p className="auth-subtitle">
          Gestão de locações e recebimentos
        </p>

        {erro && <div className="form-error">{erro}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="usuario">Usuário</label>
            <input
              id="usuario"
              type="text"
              value={usuario}
              autoComplete="username"
              onChange={(event) => setUsuario(event.target.value)}
              placeholder="seu.usuario"
            />
          </div>
          <div className="form-group">
            <label htmlFor="senha">Senha</label>
            <input
              id="senha"
              type="password"
              value={senha}
              autoComplete="current-password"
              onChange={(event) => setSenha(event.target.value)}
              placeholder="••••••••"
            />
          </div>
          <button type="submit" className="submit-btn">
            Entrar
          </button>
        </form>

        <p className="auth-hint">
          Ambiente de demonstração — use <strong>admin</strong> /{' '}
          <strong>lucrare</strong>.
        </p>
      </div>
    </div>
  )
}
