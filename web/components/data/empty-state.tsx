export function EmptyState({ mensagem = "Nenhum resultado encontrado" }: { mensagem?: string }) {
  return (
    <div className="rounded-lg border border-dashed border-border bg-card px-6 py-12 text-center text-sm text-muted-foreground">
      {mensagem}
    </div>
  );
}
