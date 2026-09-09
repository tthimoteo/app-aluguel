export type ClienteOpcao = {
  id: string;
  nome: string;
  detalhe?: string;
  perfil?: string;
};

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
