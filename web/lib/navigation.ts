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
  { href: "/inquilinos", label: "Inquilinos", icone: "users", visivelPara: "todos" },
  { href: "/contratos", label: "Contratos", icone: "file", visivelPara: "todos" },
  { href: "/usuarios", label: "Usuários", icone: "userCog", visivelPara: ["Administrador", "Gestor"] },
  { href: "/minha-conta", label: "Minha Conta", icone: "user", visivelPara: ["Gestor"] },
  { href: "/relatorios", label: "Relatórios", icone: "barChart", visivelPara: ["Gestor", "Analista"] },
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

export function tituloDaRota(pathname: string): string {
  if (pathname === "/") return "Início";
  const item = NAV_ITEMS.find((i) => i.href !== "/" && (pathname === i.href || pathname.startsWith(`${i.href}/`)));
  return item?.label ?? "APP Aluguel";
}
