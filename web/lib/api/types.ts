export type PerfilUsuario = "Administrador" | "Gestor" | "Analista";

export type VinculoCliente = {
  clienteId: string;
  nome: string;
  perfil: string;
  status: string;
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

export type Cliente = {
  id: string;
  tipoPessoa: string;
  tenantId: string;
  planoId: string | null;
  status: string;
  nomeExibicao: string;
  nome: string | null;
  cpf: string | null;
  razaoSocial: string | null;
  nomeFantasia: string | null;
  cnpj: string | null;
  telefone: string | null;
  email: string | null;
  endereco: Endereco;
};

export type Imovel = {
  id: string;
  tenantId: string;
  clienteId: string;
  nome: string;
  tipo: string;
  numeroIptu: string | null;
  numeroMatricula: string | null;
  status: string;
  endereco: Endereco;
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
