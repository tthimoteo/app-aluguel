export type ImovelTipo = 'Residencial' | 'Comercial'
export type ImovelStatus = 'Disponível' | 'Alugado' | 'Manutenção'

export interface Imovel {
  id: string
  codigo: string
  tipo: ImovelTipo
  endereco: string
  cidade: string
  valorAluguel: number
  status: ImovelStatus
  inquilinoId: string | null
}

export type InquilinoTipo = 'Pessoa Física' | 'Pessoa Jurídica'

export interface Inquilino {
  id: string
  nome: string
  tipo: InquilinoTipo
  documento: string
  telefone: string
  email: string
}

export type RecebimentoStatus = 'Pago' | 'Pendente' | 'Atrasado'

export interface Recebimento {
  id: string
  imovelId: string
  inquilinoId: string
  competencia: string
  valor: number
  vencimento: string
  status: RecebimentoStatus
}
