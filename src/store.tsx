import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { Imovel, Inquilino, Recebimento } from './types'
import {
  nextId,
  seedImoveis,
  seedInquilinos,
  seedRecebimentos,
} from './data'

interface StoreValue {
  imoveis: Imovel[]
  inquilinos: Inquilino[]
  recebimentos: Recebimento[]
  addImovel: (data: Omit<Imovel, 'id'>) => void
  updateImovel: (id: string, data: Omit<Imovel, 'id'>) => void
  removeImovel: (id: string) => void
  addInquilino: (data: Omit<Inquilino, 'id'>) => void
  updateInquilino: (id: string, data: Omit<Inquilino, 'id'>) => void
  removeInquilino: (id: string) => void
  marcarRecebido: (id: string) => void
  inquilinoNome: (id: string | null) => string
  imovelCodigo: (id: string) => string
}

const StoreContext = createContext<StoreValue | null>(null)

export function StoreProvider({ children }: { children: ReactNode }) {
  const [imoveis, setImoveis] = useState<Imovel[]>(seedImoveis)
  const [inquilinos, setInquilinos] = useState<Inquilino[]>(seedInquilinos)
  const [recebimentos, setRecebimentos] = useState<Recebimento[]>(seedRecebimentos)

  const value = useMemo<StoreValue>(() => {
    return {
      imoveis,
      inquilinos,
      recebimentos,
      addImovel: (data) =>
        setImoveis((prev) => [...prev, { ...data, id: nextId('imo') }]),
      updateImovel: (id, data) =>
        setImoveis((prev) =>
          prev.map((item) => (item.id === id ? { ...data, id } : item)),
        ),
      removeImovel: (id) =>
        setImoveis((prev) => prev.filter((item) => item.id !== id)),
      addInquilino: (data) =>
        setInquilinos((prev) => [...prev, { ...data, id: nextId('inq') }]),
      updateInquilino: (id, data) =>
        setInquilinos((prev) =>
          prev.map((item) => (item.id === id ? { ...data, id } : item)),
        ),
      removeInquilino: (id) =>
        setInquilinos((prev) => prev.filter((item) => item.id !== id)),
      marcarRecebido: (id) =>
        setRecebimentos((prev) =>
          prev.map((item) =>
            item.id === id ? { ...item, status: 'Pago' } : item,
          ),
        ),
      inquilinoNome: (id) =>
        inquilinos.find((item) => item.id === id)?.nome ?? '—',
      imovelCodigo: (id) =>
        imoveis.find((item) => item.id === id)?.codigo ?? '—',
    }
  }, [imoveis, inquilinos, recebimentos])

  return <StoreContext.Provider value={value}>{children}</StoreContext.Provider>
}

export function useStore(): StoreValue {
  const ctx = useContext(StoreContext)
  if (!ctx) {
    throw new Error('useStore deve ser usado dentro de StoreProvider')
  }
  return ctx
}
