export type PerfilUsuario = "Administrador" | "Gestor" | "Analista";

export type VinculoCliente = {
  clienteId: string;
  nome: string;
  perfil: string;
  status: string;
  /** Plano permite NFS-e e boletos (módulo Cobrança). */
  permiteCobranca?: boolean;
};

export type CobrancaListItem = {
  id: string;
  clienteId: string;
  clienteNome: string;
  imovelId: string;
  imovelNome: string;
  competencia: string;
  valorFaturado: number;
  statusNfse: string;
  temPagamento: boolean;
  dataPagamento: string | null;
  temXml: boolean;
  temPdfNfse: boolean;
  temBoletoPdf: boolean;
};

export type CobrancaResumo = {
  emAberto: number;
  atrasadas: number;
};

export type CobrancaDetalhe = {
  id: string;
  clienteId: string;
  clienteNome: string;
  imovelId: string;
  imovelNome: string;
  inquilinoId: string | null;
  inquilinoNome: string | null;
  competencia: string;
  numeroNfse: number | null;
  serie: string | null;
  chaveAcesso: string | null;
  statusNfse: string;
  valorServico: number;
  desconto: number;
  multa: number;
  juros: number;
  valorFaturado: number;
  dataEmissao: string | null;
  temPagamento: boolean;
  dataPagamento: string | null;
  valorPago: number | null;
  temXml: boolean;
  temPdfNfse: boolean;
  temBoletoPdf: boolean;
  boletoId: string | null;
  nossoNumero: string | null;
  linhaDigitavel: string | null;
  dataVencimentoBoleto: string | null;
  valorBoleto: number | null;
  statusBoleto: string | null;
};

export type UsuarioAutenticado = {
  id: string;
  email: string;
  nome: string;
  tenantId: string;
  clienteId: string | null;
  roles: string[];
  clientes?: VinculoCliente[];
};

export type TokensAutenticacao = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  usuario: UsuarioAutenticado;
};

export type Pagina<T> = {
  itens: T[];
  total: number;
  skip: number;
  take: number;
};

export type Endereco = {
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
  cep: string | null;
};

export type Plano = {
  id: string;
  codigo: string;
  nome: string;
  maxImoveis: number | null;
  maxUsuarios: number | null;
  permiteNfse: boolean;
  trialDias: number;
  valorMensal: number | null;
};

export type CertificadoDigital = {
  id: string;
  clienteId: string;
  thumbprint: string;
  validade: string;
  ativo: boolean;
  vencido: boolean;
  createdAt: string;
};

export type RegimeTributario =
  | "SimplesNacional"
  | "LucroPresumido"
  | "LucroReal"
  | "Mei";

export type Cliente = {
  id: string;
  tipoPessoa: string;
  tenantId: string;
  planoId: string | null;
  status: string;
  nomeExibicao: string;
  nome: string | null;
  cpf: string | null;
  dataNascimento: string | null;
  razaoSocial: string | null;
  nomeFantasia: string | null;
  cnpj: string | null;
  inscricaoMunicipal: string | null;
  cnaePrincipal: string | null;
  /** cTribNac — 6 dígitos (padrão recomendado: 990301) */
  codigoServico: string | null;
  /** cIndOp — 6 dígitos (padrão recomendado: 020201) */
  codigoIndicadorOperacao: string | null;
  /** cClassTrib — 6 dígitos (padrão recomendado: 200027) */
  classificacaoTributaria: string | null;
  regimeTributario: RegimeTributario | string | null;
  telefone: string | null;
  email: string | null;
  endereco: Endereco;
};

export type PropositoLocacao =
  | "Comercial"
  | "Residencial"
  | "AdministracaoDeImoveis"
  | "IntermediacaoImobiliaria";

export type Imovel = {
  id: string;
  tenantId: string;
  clienteId: string;
  nome: string;
  tipo: string;
  numeroIptu: string | null;
  numeroMatricula: string | null;
  status: string;
  competenciaInicial: string | null;
  propositoLocacao: PropositoLocacao | string | null;
  diaVencimentoCobranca: number | null;
  despesasCondominiais: number | null;
  valorIptu: number | null;
  endereco: Endereco;
  createdAt?: string;
  updatedAt?: string;
};

export type Inquilino = {
  id: string;
  tenantId: string;
  clienteId: string;
  imovelId: string | null;
  tipoPessoa: string;
  nome: string;
  documento: string;
  telefone: string | null;
  email: string | null;
  inscricaoMunicipal: string | null;
  status: string;
  endereco: Endereco;
};

export type Contrato = {
  id: string;
  tenantId: string;
  clienteId: string;
  imovelId: string;
  inquilinoId: string;
  numeroContrato: string;
  status: string;
  dataInicio: string;
  dataFimPrevista: string | null;
  diaVencimento: number;
  valorAluguel: number;
  jurosAtrasoPct: number | null;
  multaAtrasoPct: number | null;
  anexoPath: string | null;
};

export type Usuario = {
  id: string;
  tenantId: string;
  clienteId: string | null;
  nome: string;
  email: string;
  cpf: string | null;
  telefone: string | null;
  perfil: string;
  status: string;
  ultimoLogin: string | null;
};

export type AuthMe = {
  id: string | null;
  email: string | null;
  nome: string | null;
  tenantId: string | null;
  clienteId: string | null;
  roles: string[];
  clientes?: VinculoCliente[];
};
