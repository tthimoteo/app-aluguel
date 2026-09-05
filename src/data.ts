import type { Imovel, Inquilino, Recebimento } from './types'

export const seedInquilinos: Inquilino[] = [
  {
    id: 'inq-1',
    nome: 'Maria Aparecida Souza',
    tipo: 'Pessoa Física',
    documento: '312.456.789-00',
    telefone: '(11) 98877-1200',
    email: 'maria.souza@email.com',
  },
  {
    id: 'inq-2',
    nome: 'Comércio Brás Têxtil LTDA',
    tipo: 'Pessoa Jurídica',
    documento: '51.095.456/0001-13',
    telefone: '(11) 3456-7890',
    email: 'contato@brastextil.com.br',
  },
  {
    id: 'inq-3',
    nome: 'João Carlos Pereira',
    tipo: 'Pessoa Física',
    documento: '987.654.321-11',
    telefone: '(11) 91234-5678',
    email: 'jc.pereira@email.com',
  },
]

export const seedImoveis: Imovel[] = [
  {
    id: 'imo-1',
    codigo: 'AP-101',
    tipo: 'Residencial',
    endereco: 'Rua Piratininga, 240 — Apto 101',
    cidade: 'São Paulo / SP',
    valorAluguel: 2200,
    status: 'Alugado',
    inquilinoId: 'inq-1',
  },
  {
    id: 'imo-2',
    codigo: 'LJ-05',
    tipo: 'Comercial',
    endereco: 'Rua Oriente, 1180 — Loja 05',
    cidade: 'São Paulo / SP',
    valorAluguel: 5800,
    status: 'Alugado',
    inquilinoId: 'inq-2',
  },
  {
    id: 'imo-3',
    codigo: 'AP-202',
    tipo: 'Residencial',
    endereco: 'Av. Celso Garcia, 3300 — Apto 202',
    cidade: 'São Paulo / SP',
    valorAluguel: 1950,
    status: 'Disponível',
    inquilinoId: null,
  },
]

export const seedRecebimentos: Recebimento[] = [
  {
    id: 'rec-1',
    imovelId: 'imo-1',
    inquilinoId: 'inq-1',
    competencia: '09/2026',
    valor: 2200,
    vencimento: '2026-09-10',
    status: 'Pago',
  },
  {
    id: 'rec-2',
    imovelId: 'imo-2',
    inquilinoId: 'inq-2',
    competencia: '09/2026',
    valor: 5800,
    vencimento: '2026-09-10',
    status: 'Pendente',
  },
  {
    id: 'rec-3',
    imovelId: 'imo-1',
    inquilinoId: 'inq-1',
    competencia: '08/2026',
    valor: 2200,
    vencimento: '2026-08-10',
    status: 'Atrasado',
  },
]

export function formatBRL(value: number): string {
  return value.toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  })
}

let idCounter = 1000
export function nextId(prefix: string): string {
  idCounter += 1
  return `${prefix}-${idCounter}`
}
