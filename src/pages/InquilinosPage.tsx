import { useState, type FormEvent } from 'react'
import { Modal } from '../components/Modal'
import { StatusBadge } from '../components/StatusBadge'
import { useStore } from '../store'
import type { Inquilino, InquilinoTipo } from '../types'

type FormState = {
  nome: string
  tipo: InquilinoTipo
  documento: string
  telefone: string
  email: string
}

const emptyForm: FormState = {
  nome: '',
  tipo: 'Pessoa Física',
  documento: '',
  telefone: '',
  email: '',
}

export function InquilinosPage() {
  const { inquilinos, addInquilino, updateInquilino, removeInquilino } =
    useStore()
  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)

  const openCreate = () => {
    setEditingId(null)
    setForm(emptyForm)
    setModalOpen(true)
  }

  const openEdit = (inquilino: Inquilino) => {
    setEditingId(inquilino.id)
    setForm({
      nome: inquilino.nome,
      tipo: inquilino.tipo,
      documento: inquilino.documento,
      telefone: inquilino.telefone,
      email: inquilino.email,
    })
    setModalOpen(true)
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const payload = {
      nome: form.nome.trim(),
      tipo: form.tipo,
      documento: form.documento.trim(),
      telefone: form.telefone.trim(),
      email: form.email.trim(),
    }
    if (editingId) {
      updateInquilino(editingId, payload)
    } else {
      addInquilino(payload)
    }
    setModalOpen(false)
  }

  const handleDelete = (inquilino: Inquilino) => {
    if (window.confirm(`Excluir o inquilino ${inquilino.nome}?`)) {
      removeInquilino(inquilino.id)
    }
  }

  const setField = (field: keyof FormState, value: string) =>
    setForm((prev) => ({ ...prev, [field]: value }))

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Inquilinos</h1>
          <p className="page-subtitle">
            {inquilinos.length} inquilinos cadastrados
          </p>
        </div>
        <button type="button" className="btn btn-primary" onClick={openCreate}>
          + Adicionar inquilino
        </button>
      </div>

      <div className="data-table">
        {inquilinos.length === 0 ? (
          <div className="empty-state">Nenhum inquilino cadastrado.</div>
        ) : (
          <>
            <table className="desktop-view">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Tipo</th>
                  <th>Documento</th>
                  <th>Telefone</th>
                  <th>E-mail</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {inquilinos.map((inquilino) => (
                  <tr key={inquilino.id}>
                    <td>{inquilino.nome}</td>
                    <td>
                      <StatusBadge status={inquilino.tipo} />
                    </td>
                    <td>{inquilino.documento}</td>
                    <td>{inquilino.telefone}</td>
                    <td>{inquilino.email}</td>
                    <td>
                      <div className="action-btns">
                        <button
                          type="button"
                          className="edit-btn"
                          onClick={() => openEdit(inquilino)}
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          className="delete-btn"
                          onClick={() => handleDelete(inquilino)}
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
              {inquilinos.map((inquilino) => (
                <div className="data-card" key={inquilino.id}>
                  <div className="card-field">
                    <label>Nome</label>
                    <span>{inquilino.nome}</span>
                  </div>
                  <div className="card-field">
                    <label>Tipo</label>
                    <span>{inquilino.tipo}</span>
                  </div>
                  <div className="card-field">
                    <label>Documento</label>
                    <span>{inquilino.documento}</span>
                  </div>
                  <div className="card-field">
                    <label>Telefone</label>
                    <span>{inquilino.telefone}</span>
                  </div>
                  <div className="card-field">
                    <label>E-mail</label>
                    <span>{inquilino.email}</span>
                  </div>
                  <div className="card-actions">
                    <button
                      type="button"
                      className="edit-btn"
                      onClick={() => openEdit(inquilino)}
                    >
                      Editar
                    </button>
                    <button
                      type="button"
                      className="delete-btn"
                      onClick={() => handleDelete(inquilino)}
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
          title={editingId ? 'Editar inquilino' : 'Adicionar inquilino'}
          onClose={() => setModalOpen(false)}
        >
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="nome">Nome / Razão Social</label>
              <input
                id="nome"
                value={form.nome}
                required
                onChange={(e) => setField('nome', e.target.value)}
                placeholder="Nome completo ou razão social"
              />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="tipo">Tipo</label>
                <select
                  id="tipo"
                  value={form.tipo}
                  onChange={(e) => setField('tipo', e.target.value)}
                >
                  <option value="Pessoa Física">Pessoa Física</option>
                  <option value="Pessoa Jurídica">Pessoa Jurídica</option>
                </select>
              </div>
              <div className="form-group">
                <label htmlFor="documento">CPF / CNPJ</label>
                <input
                  id="documento"
                  value={form.documento}
                  required
                  onChange={(e) => setField('documento', e.target.value)}
                  placeholder="000.000.000-00"
                />
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label htmlFor="telefone">Telefone</label>
                <input
                  id="telefone"
                  value={form.telefone}
                  onChange={(e) => setField('telefone', e.target.value)}
                  placeholder="(11) 90000-0000"
                />
              </div>
              <div className="form-group">
                <label htmlFor="email">E-mail</label>
                <input
                  id="email"
                  type="email"
                  value={form.email}
                  onChange={(e) => setField('email', e.target.value)}
                  placeholder="email@dominio.com"
                />
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
