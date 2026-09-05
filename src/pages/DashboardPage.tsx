import { formatBRL } from '../data'
import { StatusBadge } from '../components/StatusBadge'
import { useStore } from '../store'

export function DashboardPage() {
  const { imoveis, inquilinos, recebimentos, inquilinoNome, imovelCodigo } =
    useStore()

  const alugados = imoveis.filter((item) => item.status === 'Alugado').length
  const recebidoMes = recebimentos
    .filter((item) => item.status === 'Pago')
    .reduce((total, item) => total + item.valor, 0)
  const emAberto = recebimentos
    .filter((item) => item.status !== 'Pago')
    .reduce((total, item) => total + item.valor, 0)

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Dashboard</h1>
          <p className="page-subtitle">
            Visão geral da carteira de locações
          </p>
        </div>
      </div>

      <div className="stats-grid">
        <div className="stat-card accent">
          <span className="stat-label">Imóveis cadastrados</span>
          <span className="stat-number">{imoveis.length}</span>
        </div>
        <div className="stat-card info">
          <span className="stat-label">Imóveis alugados</span>
          <span className="stat-number">{alugados}</span>
        </div>
        <div className="stat-card success">
          <span className="stat-label">Recebido no mês</span>
          <span className="stat-number">{formatBRL(recebidoMes)}</span>
        </div>
        <div className="stat-card danger">
          <span className="stat-label">Em aberto</span>
          <span className="stat-number">{formatBRL(emAberto)}</span>
        </div>
      </div>

      <div className="panel">
        <h2>Recebimentos recentes</h2>
        <div className="data-table">
          <table className="desktop-view">
            <thead>
              <tr>
                <th>Competência</th>
                <th>Imóvel</th>
                <th>Inquilino</th>
                <th>Valor</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {recebimentos.map((item) => (
                <tr key={item.id}>
                  <td>{item.competencia}</td>
                  <td>{imovelCodigo(item.imovelId)}</td>
                  <td>{inquilinoNome(item.inquilinoId)}</td>
                  <td>{formatBRL(item.valor)}</td>
                  <td>
                    <StatusBadge status={item.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="mobile-view" style={{ padding: '1rem' }}>
            {recebimentos.map((item) => (
              <div className="data-card" key={item.id}>
                <div className="card-field">
                  <label>Competência</label>
                  <span>{item.competencia}</span>
                </div>
                <div className="card-field">
                  <label>Imóvel</label>
                  <span>{imovelCodigo(item.imovelId)}</span>
                </div>
                <div className="card-field">
                  <label>Inquilino</label>
                  <span>{inquilinoNome(item.inquilinoId)}</span>
                </div>
                <div className="card-field">
                  <label>Valor</label>
                  <span>{formatBRL(item.valor)}</span>
                </div>
                <div className="card-field">
                  <label>Status</label>
                  <span>
                    <StatusBadge status={item.status} />
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
        <p className="page-subtitle" style={{ marginTop: '1rem' }}>
          {inquilinos.length} inquilinos ativos na carteira.
        </p>
      </div>
    </>
  )
}
