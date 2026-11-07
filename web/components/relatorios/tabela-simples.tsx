import { DataList, DesktopTable, MobileCard, CardField } from "@/components/data/data-list";

export function TabelaSimples({
  colunas,
  linhas,
  chave,
}: {
  colunas: { chave: string; rotulo: string; alinhar?: "left" | "right" }[];
  linhas: Record<string, React.ReactNode>[];
  chave: (linha: Record<string, React.ReactNode>, idx: number) => string;
}) {
  if (linhas.length === 0) {
    return <p className="text-sm text-muted-foreground">Nenhum registro no filtro atual.</p>;
  }

  return (
    <DataList
      desktop={
        <DesktopTable>
          <table className="w-full text-sm">
            <thead className="bg-[#F8F9FA] text-left text-xs uppercase tracking-wide text-muted-foreground dark:bg-muted/40">
              <tr>
                {colunas.map((c) => (
                  <th
                    key={c.chave}
                    className={`px-4 py-3 font-medium ${c.alinhar === "right" ? "text-right" : ""}`}
                  >
                    {c.rotulo}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {linhas.map((linha, idx) => (
                <tr key={chave(linha, idx)} className="border-t border-border">
                  {colunas.map((c) => (
                    <td
                      key={c.chave}
                      className={`px-4 py-3 ${c.alinhar === "right" ? "text-right font-medium" : ""}`}
                    >
                      {linha[c.chave]}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </DesktopTable>
      }
      mobile={
        <>
          {linhas.map((linha, idx) => (
            <MobileCard key={chave(linha, idx)}>
              {colunas.map((c) => (
                <CardField key={c.chave} label={c.rotulo}>
                  {linha[c.chave]}
                </CardField>
              ))}
            </MobileCard>
          ))}
        </>
      }
    />
  );
}

export function SecaoRelatorio({
  titulo,
  descricao,
  children,
}: {
  titulo: string;
  descricao?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
      <h3 className="text-sm font-semibold">{titulo}</h3>
      {descricao ? <p className="mb-4 mt-1 text-xs text-muted-foreground">{descricao}</p> : <div className="mb-4" />}
      {children}
    </section>
  );
}
