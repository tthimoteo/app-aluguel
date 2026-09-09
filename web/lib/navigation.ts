export type Perfil = "Administrador" | "Gestor" | "Analista";

export type NavItem = {
  href: string;
  label: string;
  icone: "home" | "building" | "users" | "file" | "userCog" | "user" | "barChart" | "calculator" | "scroll" | "landmark";
  visivelPara: Perfil[] | "todos";
};

/** Menu §13 — visibilidade por perfil (Gestor/Analista/Administrador). */
export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Início", icone: "home", visivelPara: "todos" },
  { href: "/clientes", label: "Clientes", icone: "landmark", visivelPara: ["Administrador"] },
  { href: "/imoveis", label: "Imóveis", icone: "building", visivelPara: "todos" },
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

export function itensVisiveis(roles: readonly string[]): NavItem[] {
  const perfis = new Set(roles);
  return NAV_ITEMS.filter((item) => {
    if (item.visivelPara === "todos") return true;
    return item.visivelPara.some((p) => perfis.has(p));
  });
}

const TITULOS_FORA_DO_MENU: Record<string, string> = {
  "/imoveis/novo": "Incluir imóvel",
  "/inquilinos": "Inquilinos",
  "/contratos": "Contratos",
};

export function tituloDaRota(pathname: string): string {
  if (pathname === "/") return "Início";
  if (/^\/imoveis\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(\?.*)?$/.test(pathname)) {
    return "Imóvel";
  }
  const candidatos: { href: string; label: string }[] = [
    ...Object.entries(TITULOS_FORA_DO_MENU).map(([href, label]) => ({ href, label })),
    ...NAV_ITEMS.filter((i) => i.href !== "/").map((i) => ({ href: i.href, label: i.label })),
  ];
  candidatos.sort((a, b) => b.href.length - a.href.length);
  const match = candidatos.find(({ href }) => pathname === href || pathname.startsWith(`${href}/`));
  return match?.label ?? "APP Aluguel";
}
