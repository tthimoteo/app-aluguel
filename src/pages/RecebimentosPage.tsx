import { StatusBadge } from '../components/StatusBadge'
import { formatBRL } from '../data'
import { useStore } from '../store'

export function RecebimentosPage() {
  const { recebimentos, marcarRecebido, inquilinoNome, imovelCodigo } =
    useStore()

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Recebimentos</h1>
          <p className="page-subtitle">
            Controle de aluguéis por competência
          </p>
        </div>
      </div>

      <div className="data-table">
        {recebimentos.length === 0 ? (
          <div className="empty-state">Nenhum recebimento lançado.</div>
        ) : (
          <>
            <table className="desktop-view">
              <thead>
                <tr>
                  <th>Competência</th>
                  <th>Imóvel</th>
                  <th>Inquilino</th>
                  <th>Vencimento</th>
                  <th>Valor</th>
                  <th>Status</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {recebimentos.map((item) => (
                  <tr key={item.id}>
                    <td>{item.competencia}</td>
                    <td>{imovelCodigo(item.imovelId)}</td>
                    <td>{inquilinoNome(item.inquilinoId)}</td>
                    <td>
                      {new Date(item.vencimento).toLocaleDateString('pt-BR')}
                    </td>
                    <td>{formatBRL(item.valor)}</td>
                    <td>
                      <StatusBadge status={item.status} />
                    </td>
                    <td>
                      {item.status === 'Pago' ? (
                        <span className="page-subtitle">Quitado</span>
                      ) : (
                        <button
                          type="button"
                          className="btn btn-success"
                          onClick={() => marcarRecebido(item.id)}
                        >
                          Marcar recebido
                        </button>
                      )}
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
                    <label>Vencimento</label>
                    <span>
                      {new Date(item.vencimento).toLocaleDateString('pt-BR')}
                    </span>
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
                  {item.status !== 'Pago' && (
                    <div className="card-actions">
                      <button
                        type="button"
                        className="btn btn-success"
                        onClick={() => marcarRecebido(item.id)}
                      >
                        Marcar recebido
                      </button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </>
  )
}
