import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

interface AuthValue {
  usuario: string | null
  login: (usuario: string, senha: string) => boolean
  logout: () => void
}

const STORAGE_KEY = 'app-aluguel.usuario'

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<string | null>(() =>
    sessionStorage.getItem(STORAGE_KEY),
  )

  const value = useMemo<AuthValue>(
    () => ({
      usuario,
      login: (nome, senha) => {
        if (nome.trim().length === 0 || senha.trim().length === 0) {
          return false
        }
        sessionStorage.setItem(STORAGE_KEY, nome)
        setUsuario(nome)
        return true
      },
      logout: () => {
        sessionStorage.removeItem(STORAGE_KEY)
        setUsuario(null)
      },
    }),
    [usuario],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth deve ser usado dentro de AuthProvider')
  }
  return ctx
}
