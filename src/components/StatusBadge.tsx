type Tone = 'ok' | 'warn' | 'bad' | 'neutral'

const toneByStatus: Record<string, Tone> = {
  Pago: 'ok',
  Ativo: 'ok',
  Disponível: 'ok',
  Pendente: 'warn',
  Manutenção: 'warn',
  Atrasado: 'bad',
  Inativo: 'bad',
  Alugado: 'neutral',
}

export function StatusBadge({ status }: { status: string }) {
  const tone = toneByStatus[status] ?? 'neutral'
  return <span className={`status ${tone}`}>{status}</span>
}
