# Lucrare — Padrões de Componentes

Exemplos de código (CSS puro, compatível com qualquer framework) reproduzindo os padrões reais do app Lucrare Gestão. Adapte para Tailwind/CSS-in-JS/styled-components conforme a stack do projeto, mantendo os mesmos valores.

## 1. Tela de login / autenticação

Estrutura: página inteira centraliza um card branco sobre fundo cinza-claro.

```css
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #f5f5f5;
  padding: 1rem;
}

.auth-card {
  width: 100%;
  max-width: 400px;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  padding: 2rem;
}

.auth-logo {
  text-align: center;
  margin-bottom: 1.5rem;
}

.auth-card h2 {
  text-align: center;
  margin-bottom: 1.5rem;
  color: #2c3e50;
}
```

Inputs e botão de submit (foco e hover em laranja da marca):

```css
.form-group input {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  box-sizing: border-box;
}
.form-group input:focus {
  outline: none;
  border-color: #db6838;
  box-shadow: 0 0 0 2px rgba(219, 104, 56, 0.2);
}

.submit-btn {
  width: 100%;
  padding: 0.75rem;
  background-color: #db6838;
  color: #fff;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.2s;
}
.submit-btn:hover:not(:disabled) {
  background-color: #c55a32;
}
.submit-btn:disabled {
  background-color: #bdc3c7;
  cursor: not-allowed;
}
```

Campos esperados em PT-BR: rótulo "Usuário:" / "Senha:", botão "Entrar".

## 2. Header / navegação do app

```css
.header {
  background-color: #2c3e50;
  color: #fff;
  padding: 1rem 0;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}
.header-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.nav-link {
  color: #fff;
  font-weight: 500;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  text-decoration: none;
  transition: background-color 0.2s;
}
.nav-link:hover {
  background-color: rgba(255, 255, 255, 0.1);
}
.nav-link.active {
  background-color: #db6838;
}
.logout-btn {
  background-color: #e74c3c;
  color: #fff;
  border: none;
  border-radius: 4px;
  padding: 0.5rem 1rem;
  transition: background-color 0.2s;
}
.logout-btn:hover {
  background-color: #c0392b;
}
```

Em telas `<768px`, o header vira coluna (`flex-direction: column`), e os itens de navegação se centralizam e ganham `width: 100%`.

## 3. Tabela de dados → cards no mobile

Este é o padrão mais distintivo do produto: em desktop usa `<table>`, em mobile (`<768px`) alterna para uma lista de cards. Controle via duas classes utilitárias:

```css
.desktop-view { display: table; }
.mobile-view { display: none; }

@media (max-width: 768px) {
  .desktop-view { display: none; }
  .mobile-view { display: block; }
}
```

Tabela (desktop):

```css
.customers-table {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  overflow-x: auto;
}
.customers-table table {
  width: 100%;
  min-width: 800px;
  border-collapse: collapse;
}
.customers-table th {
  background-color: #34495e;
  color: #fff;
  font-weight: 600;
  padding: 1rem;
  text-align: left;
  white-space: nowrap;
}
.customers-table td {
  padding: 1rem;
  border-bottom: 1px solid #ecf0f1;
  text-align: left;
}
.customers-table tbody tr:hover {
  background-color: #f8f9fa;
}
```

Card (mobile), com rótulo/valor por linha:

```css
.customer-card {
  background: #fff;
  border: 1px solid #e9ecef;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 1.5rem;
  transition: transform 0.2s, box-shadow 0.2s;
}
.customer-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}
.card-field {
  display: flex;
  justify-content: space-between;
  padding: 0.5rem 0;
  border-bottom: 1px solid #f8f9fa;
}
.card-field label {
  font-weight: 600;
  color: #495057;
  min-width: 80px;
}
.card-field span {
  color: #6c757d;
  text-align: right;
}
```

## 4. Badges de status

Pill pequeno, uppercase, cor por semântica:

```css
.status {
  display: inline-block;
  padding: 0.25rem 0.5rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
}
.status.active   { background-color: #d4edda; color: #155724; }
.status.inactive { background-color: #f8d7da; color: #721c24; }
.status.admin    { background-color: #db6838; color: #fff; }
.status.user     { background-color: #17a2b8; color: #fff; }
```

## 5. Botões de ação (linha de tabela/card)

Três ações padrão lado a lado — cores fixas por função, não por preferência:

```css
.edit-btn     { background-color: #db6838; color: #fff; } /* editar = laranja marca */
.comments-btn { background-color: #007bff; color: #fff; } /* ação secundária = azul */
.delete-btn   { background-color: #e74c3c; color: #fff; } /* destrutiva = vermelho */

.edit-btn, .comments-btn, .delete-btn {
  border: none;
  border-radius: 4px;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  cursor: pointer;
  transition: background-color 0.2s;
}
.edit-btn:hover { background-color: #c55a32; }
.comments-btn:hover { background-color: #0056b3; }
.delete-btn:hover { background-color: #c0392b; }
```

## 6. Modal

```css
.modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal {
  background: #fff;
  border-radius: 8px;
  width: 90%;
  max-width: 600px;
  max-height: 90vh;
  overflow-y: auto;
}
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-bottom: 1px solid #ecf0f1;
}
.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  padding-top: 1rem;
  margin-top: 1.5rem;
  border-top: 1px solid #ecf0f1;
}
.cancel-btn { background-color: #95a5a6; color: #fff; }
.save-btn   { background-color: #27ae60; color: #fff; }
```

## 7. Formulário em grid de 2 colunas (colapsa no mobile)

```css
.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}
@media (max-width: 768px) {
  .form-row { grid-template-columns: 1fr; }
}
.form-group label {
  display: block;
  font-weight: 500;
  color: #333;
  margin-bottom: 0.5rem;
}
```

## 8. Contador / prova social (site institucional)

Padrão de destaque numérico grande usado na home institucional para reforçar autoridade:

```html
<div class="stat">
  <span class="stat-number">+300</span>
  <span class="stat-label">Clientes</span>
</div>
```

Use números grandes e em negrito (ex.: `+300`, `+30 Anos`) combinados com um rótulo curto abaixo — reforça a mensagem de tradição e confiança da marca.

## Vocabulário de UI em PT-BR (usar consistentemente)

`Entrar`, `Usuário`, `Senha`, `Clientes`, `Usuários`, `Honorário`, `Status`, `Ativo`, `Inativo`, `Editar`, `Excluir`, `Cancelar`, `Salvar`, `Adicionar`, `Comentários`, `Buscar`, `Limpar busca`, `Nenhum resultado encontrado`, `Carregando...`.
