export type Perfil = "Administrador" | "Gestor" | "Analista";

export type NavItem = {
  href: string;
  label: string;
  icone: "home" | "building" | "users" | "file" | "userCog" | "user" | "barChart" | "calculator" | "scroll" | "landmark" | "receipt";
  visivelPara: Perfil[] | "todos";
  /** Se true, Gestor/Analista só veem o item quando o cliente do contexto permite Cobrança (plano NFS-e). */
  exigeCobranca?: boolean;
};

/** Menu §13 — visibilidade por perfil (Gestor/Analista/Administrador). */
export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Início", icone: "home", visivelPara: "todos" },
  { href: "/clientes", label: "Clientes", icone: "landmark", visivelPara: ["Administrador"] },
  { href: "/imoveis", label: "Imóveis", icone: "building", visivelPara: "todos" },
  {
    href: "/cobrancas",
    label: "Cobrança",
    icone: "receipt",
    visivelPara: "todos",
    exigeCobranca: true,
  },
  { href: "/usuarios", label: "Usuários", icone: "userCog", visivelPara: ["Administrador", "Gestor"] },
  { href: "/minha-conta", label: "Minha Conta", icone: "user", visivelPara: ["Gestor"] },
  { href: "/relatorios", label: "Relatórios", icone: "barChart", visivelPara: ["Administrador", "Gestor", "Analista"] },
  {
    href: "/contabilidade",
    label: "Dados para Contabilidade",
    icone: "calculator",
    visivelPara: ["Gestor"],
  },
  { href: "/auditoria", label: "Auditoria", icone: "scroll", visivelPara: ["Administrador", "Gestor"] },
];

export type OpcoesMenu = {
  /** Administrador: sempre true. Gestor/Analista: plano do cliente atual permite NFS-e/boletos. */
  permiteCobranca?: boolean;
  ehAdministrador?: boolean;
};

export function itensVisiveis(roles: readonly string[], opcoes: OpcoesMenu = {}): NavItem[] {
  const perfis = new Set(roles);
  const ehAdmin = opcoes.ehAdministrador ?? perfis.has("Administrador");
  const permiteCobranca = ehAdmin || Boolean(opcoes.permiteCobranca);

  return NAV_ITEMS.filter((item) => {
    if (item.exigeCobranca && !permiteCobranca) return false;
    if (item.visivelPara === "todos") return true;
    return item.visivelPara.some((p) => perfis.has(p));
  });
}

const TITULOS_FORA_DO_MENU: Record<string, string> = {
  "/imoveis/novo": "Incluir imóvel",
  "/inquilinos": "Inquilinos",
  "/contratos": "Contratos",
};

const UUID =
  "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";

export function tituloDaRota(pathname: string): string {
  if (pathname === "/") return "Início";
  if (new RegExp(`^/imoveis/${UUID}(\\?.*)?$`).test(pathname)) {
    return "Imóvel";
  }
  if (new RegExp(`^/clientes/${UUID}(\\?.*)?$`).test(pathname)) {
    return "Editar cliente";
  }
  if (new RegExp(`^/cobrancas/${UUID}(\\?.*)?$`).test(pathname)) {
    return "Detalhe da cobrança";
  }
  const candidatos: { href: string; label: string }[] = [
    ...Object.entries(TITULOS_FORA_DO_MENU).map(([href, label]) => ({ href, label })),
    ...NAV_ITEMS.filter((i) => i.href !== "/").map((i) => ({ href: i.href, label: i.label })),
  ];
  candidatos.sort((a, b) => b.href.length - a.href.length);
  const match = candidatos.find(({ href }) => pathname === href || pathname.startsWith(`${href}/`));
  return match?.label ?? "APP Aluguel";
}
