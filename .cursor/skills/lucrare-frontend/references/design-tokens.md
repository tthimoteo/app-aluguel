# Lucrare — Design Tokens

Tokens extraídos diretamente do CSS de produção do app (`lucraregestao.netlify.app`, bundle `main.fa7c4780.css`) e do HTML renderizado do site institucional Wix (`lucrarecontabilidade.com.br`). Sempre que possível, prefira os valores marcados como **[app]** para telas de dashboard/gestão e os marcados como **[site]** para páginas de marketing/institucionais — mas a cor de accent laranja é compartilhada pelos dois.

## Paleta de cores

### Accent / marca (compartilhado)

| Token | Hex | Uso |
|---|---|---|
| `--lucrare-accent` | `#DB6838` | CTA primário, links, foco de input, estado ativo, badge de destaque/admin |
| `--lucrare-accent-hover` | `#C55A32` | Hover de botão/link primário |
| `--lucrare-accent-soft` | `#DB683833` (20% alpha) | Anel de foco (`box-shadow`) em inputs |
| `--lucrare-accent-site` | `#DD6112` | Variante usada no site institucional (mesma família tonal) |

### Neutros / autoridade

| Token | Hex | Uso |
|---|---|---|
| `--lucrare-navy` | `#2C3E50` | Header do app, títulos de página, texto de destaque escuro |
| `--lucrare-navy-deep` | `#02356E` | Azul profundo usado no site institucional |
| `--lucrare-slate` | `#34495E` | Cabeçalho de tabelas |
| `--lucrare-gray-700` | `#495057` | Texto secundário forte (labels, autor de comentário) |
| `--lucrare-gray-600` | `#6C757D` | Texto muted, placeholders, botão cancelar |
| `--lucrare-gray-500` | `#7F8C8D` | Ícones/close button |
| `--lucrare-gray-400` | `#95A5A6` | Botão cancelar alternativo |
| `--lucrare-border` | `#DDD` / `#DEE2E6` | Bordas de input |
| `--lucrare-border-soft` | `#E9ECEF` / `#ECF0F1` / `#F0F0F0` | Divisores, bordas de card |

### Fundos

| Token | Hex | Uso |
|---|---|---|
| `--lucrare-bg-app` | `#F8F9FA` | Fundo geral do app |
| `--lucrare-bg-auth` | `#F5F5F5` | Fundo da tela de login |
| `--lucrare-bg-site` | `#F9F9F5` | Fundo de seções do site institucional |
| `--lucrare-surface` | `#FFFFFF` | Cards, modais, tabelas |

### Estados semânticos

| Token | Hex | Uso |
|---|---|---|
| `--lucrare-success` | `#27AE60` (alt. `#28A745`) | Ações de sucesso, salvar, status "ativo" |
| `--lucrare-success-bg` | `#D4EDDA` | Fundo de badge "ativo" |
| `--lucrare-success-text` | `#155724` | Texto de badge "ativo" |
| `--lucrare-danger` | `#E74C3C` (alt. `#DC3545`) | Excluir, logout, erro |
| `--lucrare-danger-bg` | `#F8D7DA` | Fundo de badge/erro "inativo" |
| `--lucrare-danger-text` | `#721C24` | Texto de badge/erro "inativo" |
| `--lucrare-warning` | `#FFC107` | Editar comentário, avisos |
| `--lucrare-info` | `#007BFF` (alt. `#17A2B8`) | Ação secundária (ex.: "comentários") |

## Tipografia

- **App (dashboard)**: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Fira Sans", "Droid Sans", "Helvetica Neue", sans-serif` — stack nativa do sistema, peso normal no corpo, `600–700` em títulos e labels de destaque.
- **Site institucional**: títulos em `helvetica-w01-bold, sans-serif` (ou `Montserrat`/`Montserrat Black` em variações), corpo em `Helvetica, Arial, sans-serif`.
- Escala de heading observada no site: `Heading L = 34px/1.4`, `Heading M = 25px/1.4`, `Heading S = 22px/1.4`.
- Tamanho de corpo padrão do app: `1rem` (inputs, botões); textos secundários em `0.75rem–0.9rem`.

## Espaçamento, raio e sombra

| Elemento | Border-radius | Sombra (repouso) | Sombra (hover) |
|---|---|---|---|
| Input / botão padrão | `4px` | — | — |
| Card de listagem (mobile) | `12px` | `0 2px 8px rgba(0,0,0,.1)` | `0 4px 12px–16px rgba(0,0,0,.15–.16)` + `translateY(-2px)` |
| Card institucional/modal | `8px`–`12px` | `0 2px 4px rgba(0,0,0,.1)` a `0 4px 6px rgba(0,0,0,.1)` | — |
| Modal (overlay) | `8px`–`12px` | `0 10px 25px rgba(0,0,0,.3)` | — |
| Badge/pill de status | `12px` | — | — |

Transições padrão: `transition: background-color .2s` (botões), `transition: transform .2s, box-shadow .2s` (cards).

Overlay de modal: `background-color: rgba(0,0,0,.5)`.

## Tailwind theme extension sugerido

```js
// tailwind.config.js — extend.colors
{
  lucrare: {
    accent: '#DB6838',
    'accent-hover': '#C55A32',
    navy: '#2C3E50',
    'navy-deep': '#02356E',
    slate: '#34495E',
    success: '#27AE60',
    danger: '#E74C3C',
    warning: '#FFC107',
    info: '#007BFF',
    'bg-app': '#F8F9FA',
    'bg-auth': '#F5F5F5',
    surface: '#FFFFFF',
  },
}
```

## CSS variables prontas para copiar

```css
:root {
  --lucrare-accent: #db6838;
  --lucrare-accent-hover: #c55a32;
  --lucrare-navy: #2c3e50;
  --lucrare-slate: #34495e;
  --lucrare-success: #27ae60;
  --lucrare-danger: #e74c3c;
  --lucrare-warning: #ffc107;
  --lucrare-info: #007bff;
  --lucrare-bg-app: #f8f9fa;
  --lucrare-bg-auth: #f5f5f5;
  --lucrare-surface: #ffffff;
  --lucrare-border: #dddddd;
  --lucrare-border-soft: #e9ecef;
  --lucrare-text-muted: #6c757d;
  --lucrare-font: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    Oxygen, Ubuntu, Cantarell, "Fira Sans", "Droid Sans", "Helvetica Neue",
    sans-serif;
}
```
