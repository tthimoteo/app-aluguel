import { useState, type FormEvent } from 'react'
import { Modal } from '../components/Modal'
import { StatusBadge } from '../components/StatusBadge'
import { formatBRL } from '../data'
import { useStore } from '../store'
import type { Imovel, ImovelStatus, ImovelTipo } from '../types'

type FormState = {
  codigo: string
  tipo: ImovelTipo
  endereco: string
  cidade: string
  valorAluguel: string
  status: ImovelStatus
  inquilinoId: string
}

const emptyForm: FormState = {
  codigo: '',
  tipo: 'Residencial',
  endereco: '',
  cidade: '',
  valorAluguel: '',
  status: 'Disponível',
  inquilinoId: '',
}

export function ImoveisPage() {
  const {
    imoveis,
    inquilinos,
    inquilinoNome,
    addImovel,
    updateImovel,
    removeImovel,
  } = useStore()
  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)

  const openCreate = () => {
    setEditingId(null)
    setForm(emptyForm)
    setModalOpen(true)
  }

  const openEdit = (imovel: Imovel) => {
    setEditingId(imovel.id)
    setForm({
      codigo: imovel.codigo,
      tipo: imovel.tipo,
      endereco: imovel.endereco,
      cidade: imovel.cidade,
      valorAluguel: String(imovel.valorAluguel),
      status: imovel.status,
      inquilinoId: imovel.inquilinoId ?? '',
    })
    setModalOpen(true)
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const payload = {
      codigo: form.codigo.trim(),
      tipo: form.tipo,
      endereco: form.endereco.trim(),
      cidade: form.cidade.trim(),
      valorAluguel: Number(form.valorAluguel) || 0,
      status: form.status,
      inquilinoId: form.inquilinoId || null,
    }
    if (editingId) {
      updateImovel(editingId, payload)
    } else {
      addImovel(payload)
    }
    setModalOpen(false)
  }

  const handleDelete = (imovel: Imovel) => {
    if (window.confirm(`Excluir o imóvel ${imovel.codigo}?`)) {
      removeImovel(imovel.id)
    }
  }

  const setField = (field: keyof FormState, value: string) =>
    setForm((prev) => ({ ...prev, [field]: value }))

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Imóveis</h1>
          <p className="page-subtitle">{imoveis.length} imóveis na carteira</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={openCreate}>
          + Adicionar imóvel
        </button>
      </div>

      <div className="data-table">
        {imoveis.length === 0 ? (
          <div className="empty-state">Nenhum imóvel cadastrado.</div>
        ) : (
          <>
            <table className="desktop-view">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Tipo</th>
                  <th>Endereço</th>
                  <th>Aluguel</th>
                  <th>Inquilino</th>
                  <th>Status</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {imoveis.map((imovel) => (
                  <tr key={imovel.id}>
                    <td>{imovel.codigo}</td>
                    <td>{imovel.tipo}</td>
                    <td>{imovel.endereco}</td>
                    <td>{formatBRL(imovel.valorAluguel)}</td>
                    <td>{inquilinoNome(imovel.inquilinoId)}</td>
                    <td>
                      <StatusBadge status={imovel.status} />
                    </td>
                    <td>
                      <div className="action-btns">
                        <button
                          type="button"
                          className="edit-btn"
                          onClick={() => openEdit(imovel)}
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          className="delete-btn"
                          onClick={() => handleDelete(imovel)}
                        >
                          Excluir
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="mobile-view" style={{ padding: '1rem' }}>
              {imoveis.map((imovel) => (
                <div className="data-card" key={imovel.id}>
                  <div className="card-field">
                    <label>Código</label>
                    <span>{imovel.codigo}</span>
                  </div>
                  <div className="card-field">
                    <label>Tipo</label>
                    <span>{imovel.tipo}</span>
                  </div>
                  <div className="card-field">
                    <label>Endereço</label>
                    <span>{imovel.endereco}</span>
                  </div>
                  <div className="card-field">
                    <label>Aluguel</label>
                    <span>{formatBRL(imovel.valorAluguel)}</span>
                  </div>
                  <div className="card-field">
                    <label>Inquilino</label>
                    <span>{inquilinoNome(imovel.inquilinoId)}</span>
                  </div>
                  <div className="card-field">
                    <label>Status</label>
                    <span>
                      <StatusBadge status={imovel.status} />
                    </span>
                  </div>
                  <div className="card-actions">
                    <button
                      type="button"
                      className="edit-btn"
                      onClick={() => openEdit(imovel)}
                    >
                      Editar
                    </button>
                    <button
                      type="button"
                      className="delete-btn"
                      onClick={() => handleDelete(imovel)}
                    >
                      Excluir
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>

      {modalOpen && (
        <Modal
          title={editingId ? 'Editar imóvel' : 'Adicionar imóvel'}
          onClose={() => setModalOpen(false)}
        >
          <form onSubmit={handleSubmit}>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="codigo">Código</label>
                <input
                  id="codigo"
                  value={form.codigo}
                  required
                  onChange={(e) => setField('codigo', e.target.value)}
                  placeholder="AP-303"
                />
              </div>
              <div className="form-group">
                <label htmlFor="tipo">Tipo</label>
                <select
                  id="tipo"
                  value={form.tipo}
                  onChange={(e) => setField('tipo', e.target.value)}
                >
                  <option value="Residencial">Residencial</option>
                  <option value="Comercial">Comercial</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="endereco">Endereço</label>
              <input
                id="endereco"
                value={form.endereco}
                required
                onChange={(e) => setField('endereco', e.target.value)}
                placeholder="Rua, número e complemento"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="cidade">Cidade / UF</label>
                <input
                  id="cidade"
                  value={form.cidade}
                  onChange={(e) => setField('cidade', e.target.value)}
                  placeholder="São Paulo / SP"
                />
              </div>
              <div className="form-group">
                <label htmlFor="valor">Valor do aluguel (R$)</label>
                <input
                  id="valor"
                  type="number"
                  min="0"
                  step="0.01"
                  value={form.valorAluguel}
                  required
                  onChange={(e) => setField('valorAluguel', e.target.value)}
                  placeholder="0,00"
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="status">Status</label>
                <select
                  id="status"
                  value={form.status}
                  onChange={(e) => setField('status', e.target.value)}
                >
                  <option value="Disponível">Disponível</option>
                  <option value="Alugado">Alugado</option>
                  <option value="Manutenção">Manutenção</option>
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="inquilino">Inquilino</label>
                <select
                  id="inquilino"
                  value={form.inquilinoId}
                  onChange={(e) => setField('inquilinoId', e.target.value)}
                >
                  <option value="">— Sem inquilino —</option>
                  {inquilinos.map((inq) => (
                    <option key={inq.id} value={inq.id}>
                      {inq.nome}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="modal-actions">
              <button
                type="button"
                className="btn btn-cancel"
                onClick={() => setModalOpen(false)}
              >
                Cancelar
              </button>
              <button type="submit" className="btn btn-success">
                Salvar
              </button>
            </div>
          </form>
        </Modal>
      )}
    </>
  )
}
