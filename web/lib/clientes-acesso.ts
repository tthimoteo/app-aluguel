export type ClienteOpcao = {
  id: string;
  nome: string;
  detalhe?: string;
  perfil?: string;
};

/** Primeiro id da lista que estiver em `preferidos`; senão o primeiro cliente. */
export function clientePreselecionado(
  clientes: readonly { id: string }[],
  ...preferidos: (string | null | undefined)[]
): string {
  for (const id of preferidos) {
    if (id && clientes.some((c) => c.id === id)) return id;
  }
  return clientes[0]?.id ?? "";
}

export function filtrarClientesAcessiveis(
  clientes: ClienteOpcao[],
  termo?: string,
): ClienteOpcao[] {
  const busca = termo?.trim().toLowerCase();
  if (!busca) return clientes;
  return clientes.filter(
    (c) =>
      c.nome.toLowerCase().includes(busca) ||
      (c.detalhe?.toLowerCase().includes(busca) ?? false),
  );
}
