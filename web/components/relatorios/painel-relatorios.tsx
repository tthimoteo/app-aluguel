"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { selectClass } from "@/components/imoveis/campos-endereco";
import { IndicadoresGrid, moeda, pct } from "@/components/relatorios/indicadores-grid";
import {
  GraficoEvolucaoRendimento,
  GraficoGeradoRecebido,
  GraficoBarrasSimples,
} from "@/components/relatorios/graficos";
import { SecaoRelatorio, TabelaSimples } from "@/components/relatorios/tabela-simples";
import type { RelatorioPainel } from "@/lib/api/relatorios";
import { formatarData } from "@/lib/format";
import { cn } from "@/lib/utils";

const ABAS = [
  { id: "dashboard", rotulo: "Dashboard" },
  { id: "financeiro", rotulo: "Financeiro" },
  { id: "imoveis", rotulo: "Imóveis" },
  { id: "contratos", rotulo: "Contratos" },
  { id: "fiscal", rotulo: "Fiscal" },
] as const;

type AbaId = (typeof ABAS)[number]["id"];

const MESES = [
  { v: "", l: "Todos" },
  { v: "1", l: "Janeiro" },
  { v: "2", l: "Fevereiro" },
  { v: "3", l: "Março" },
  { v: "4", l: "Abril" },
  { v: "5", l: "Maio" },
  { v: "6", l: "Junho" },
  { v: "7", l: "Julho" },
  { v: "8", l: "Agosto" },
  { v: "9", l: "Setembro" },
  { v: "10", l: "Outubro" },
  { v: "11", l: "Novembro" },
  { v: "12", l: "Dezembro" },
];

export function PainelRelatorios({
  painel,
  imoveis,
  clientes,
  ehAdmin,
  anoPadrao,
}: {
  painel: RelatorioPainel;
  imoveis: { id: string; nome: string }[];
  clientes: { id: string; nome: string }[];
  ehAdmin: boolean;
  anoPadrao: number;
}) {
  const router = useRouter();
  const search = useSearchParams();
  const [pending, startTransition] = useTransition();
  const [aba, setAba] = useState<AbaId>("dashboard");

  const [ano, setAno] = useState(search.get("ano") ?? String(anoPadrao));
  const [mes, setMes] = useState(search.get("mes") ?? "");
  const [dataInicio, setDataInicio] = useState(search.get("dataInicio") ?? "");
  const [dataFim, setDataFim] = useState(search.get("dataFim") ?? "");
  const [clienteId, setClienteId] = useState(search.get("clienteId") ?? "");
  const [imovelId, setImovelId] = useState(search.get("imovelId") ?? "");
  const [diasAtraso, setDiasAtraso] = useState(search.get("diasAtrasoMinimos") ?? "1");

  function aplicarFiltros() {
    const params = new URLSearchParams();
    if (ano) params.set("ano", ano);
    if (mes) params.set("mes", mes);
    if (dataInicio) params.set("dataInicio", dataInicio);
    if (dataFim) params.set("dataFim", dataFim);
    if (clienteId) params.set("clienteId", clienteId);
    if (imovelId) params.set("imovelId", imovelId);
    if (diasAtraso) params.set("diasAtrasoMinimos", diasAtraso);
    startTransition(() => {
      router.push(`/relatorios?${params.toString()}`);
    });
  }

  const d = painel.dashboard;

  const conteudo = useMemo(() => {
    if (aba === "dashboard") return <AbaDashboard painel={painel} />;
    if (aba === "financeiro") return <AbaFinanceiro painel={painel} />;
    if (aba === "imoveis") return <AbaImoveis painel={painel} />;
    if (aba === "contratos") return <AbaContratos painel={painel} />;
    return <AbaFiscal painel={painel} ehAdmin={ehAdmin} />;
  }, [aba, painel, ehAdmin]);

  return (
    <div className="space-y-4">
      <section className="rounded-lg bg-card p-4 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7">
          <Campo rotulo="Ano" htmlFor="ano">
            <Input
              id="ano"
              type="number"
              min={2020}
              max={2100}
              value={ano}
              onChange={(e) => setAno(e.target.value)}
              className="h-9 rounded-[4px]"
            />
          </Campo>
          <Campo rotulo="Mês" htmlFor="mes">
            <select id="mes" value={mes} onChange={(e) => setMes(e.target.value)} className={selectClass}>
              {MESES.map((m) => (
                <option key={m.v || "all"} value={m.v}>
                  {m.l}
                </option>
              ))}
            </select>
          </Campo>
          <Campo rotulo="Data início" htmlFor="dataInicio">
            <Input
              id="dataInicio"
              type="date"
              value={dataInicio}
              onChange={(e) => setDataInicio(e.target.value)}
              className="h-9 rounded-[4px]"
            />
          </Campo>
          <Campo rotulo="Data fim" htmlFor="dataFim">
            <Input
              id="dataFim"
              type="date"
              value={dataFim}
              onChange={(e) => setDataFim(e.target.value)}
              className="h-9 rounded-[4px]"
            />
          </Campo>
          {ehAdmin ? (
            <Campo rotulo="Proprietário" htmlFor="clienteId">
              <select
                id="clienteId"
                value={clienteId}
                onChange={(e) => setClienteId(e.target.value)}
                className={selectClass}
              >
                <option value="">Todos</option>
                {clientes.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nome}
                  </option>
                ))}
              </select>
            </Campo>
          ) : null}
          <Campo rotulo="Imóvel" htmlFor="imovelId">
            <select
              id="imovelId"
              value={imovelId}
              onChange={(e) => setImovelId(e.target.value)}
              className={selectClass}
            >
              <option value="">Todos</option>
              {imoveis.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.nome}
                </option>
              ))}
            </select>
          </Campo>
          <Campo rotulo="Dias atraso (mín.)" htmlFor="diasAtraso">
            <Input
              id="diasAtraso"
              type="number"
              min={0}
              value={diasAtraso}
              onChange={(e) => setDiasAtraso(e.target.value)}
              className="h-9 rounded-[4px]"
            />
          </Campo>
        </div>
        <div className="mt-3 flex justify-end">
          <Button
            type="button"
            className="h-9 rounded-[4px]"
            disabled={pending}
            onClick={aplicarFiltros}
          >
            {pending ? "Atualizando…" : "Aplicar filtros"}
          </Button>
        </div>
      </section>

      <IndicadoresGrid
        itens={[
          { titulo: "Receita total", valor: moeda(d.receitaTotalPeriodo) },
          { titulo: "Receita líquida (aluguel)", valor: moeda(d.receitaLiquidaAluguel) },
          { titulo: "IPTU repassado", valor: moeda(d.valorIptuRepassado) },
          { titulo: "Condomínio repassado", valor: moeda(d.valorCondominioRepassado) },
          { titulo: "Total recebido", valor: moeda(d.totalRecebido), destaque: "ok" },
          { titulo: "Total em aberto", valor: moeda(d.totalEmAberto), destaque: "danger" },
          {
            titulo: "Inadimplência",
            valor: pct(d.inadimplenciaPercentual),
            destaque: d.inadimplenciaPercentual > 10 ? "danger" : "muted",
          },
          { titulo: "Contratos ativos", valor: d.contratosAtivos },
          { titulo: "Imóveis ocupados", valor: d.imoveisOcupados },
          { titulo: "Imóveis vagos", valor: d.imoveisVagos },
        ]}
      />

      <div className="flex flex-wrap gap-1 border-b border-border pb-px">
        {ABAS.map((a) => (
          <button
            key={a.id}
            type="button"
            onClick={() => setAba(a.id)}
            className={cn(
              "rounded-t-[4px] px-3 py-2 text-sm font-medium transition-colors",
              aba === a.id
                ? "bg-card text-primary ring-1 ring-border ring-b-transparent"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            {a.rotulo}
          </button>
        ))}
      </div>

      <div className="space-y-4">{conteudo}</div>
    </div>
  );
}

function Campo({
  rotulo,
  htmlFor,
  children,
}: {
  rotulo: string;
  htmlFor: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor={htmlFor} className="text-xs text-[#333] dark:text-foreground">
        {rotulo}
      </Label>
      {children}
    </div>
  );
}

function AbaDashboard({ painel }: { painel: RelatorioPainel }) {
  const d = painel.dashboard;
  return (
    <>
      <SecaoRelatorio
        titulo="Evolução mensal da receita"
        descricao="Aluguel, IPTU e condomínio por competência — sazonalidade da carteira."
      >
        <GraficoEvolucaoRendimento dados={d.evolucaoMensal} />
      </SecaoRelatorio>
      <div className="grid gap-4 lg:grid-cols-2">
        <SecaoRelatorio titulo="Aging de recebíveis" descricao="Cobranças em aberto por faixa de atraso.">
          <TabelaSimples
            colunas={[
              { chave: "faixa", rotulo: "Faixa" },
              { chave: "qtd", rotulo: "Qtd.", alinhar: "right" },
              { chave: "valor", rotulo: "Valor", alinhar: "right" },
            ]}
            linhas={d.agingRecebiveis.map((a) => ({
              faixa: a.faixa,
              qtd: a.quantidade,
              valor: moeda(a.valor),
            }))}
            chave={(l) => String(l.faixa)}
          />
        </SecaoRelatorio>
        <SecaoRelatorio
          titulo="Concentração de receita"
          descricao={`Os 5 maiores contratos representam ${pct(d.concentracaoTop5Percentual)} da receita do período.`}
        >
          <IndicadoresGrid
            itens={[
              { titulo: "Top 5 contratos", valor: pct(d.concentracaoTop5Percentual) },
              { titulo: "Taxa de vacância", valor: pct(painel.vacancia.taxaVacanciaPercentual) },
              { titulo: "Inadimplência", valor: pct(d.inadimplenciaPercentual), destaque: "danger" },
            ]}
          />
        </SecaoRelatorio>
      </div>
    </>
  );
}

function AbaFinanceiro({ painel }: { painel: RelatorioPainel }) {
  const r = painel.rendimento;
  const c = painel.cobrancas;
  const dre = painel.dre;
  return (
    <>
      <SecaoRelatorio titulo="1 · Rendimento no período" descricao="Composição da receita faturada.">
        <IndicadoresGrid
          className="mb-4"
          itens={[
            { titulo: "Aluguel", valor: moeda(r.aluguel) },
            { titulo: "IPTU", valor: moeda(r.iptu) },
            { titulo: "Condomínio", valor: moeda(r.condominio) },
            { titulo: "Multas", valor: moeda(r.multas) },
            { titulo: "Juros", valor: moeda(r.juros) },
            { titulo: "Receita total", valor: moeda(r.receitaTotal) },
          ]}
        />
        <GraficoEvolucaoRendimento dados={r.evolucao} />
      </SecaoRelatorio>

      <SecaoRelatorio
        titulo="2 · Cobranças geradas × recebidas"
        descricao="Evidencia perda de caixa entre o faturado e o efetivamente pago."
      >
        <IndicadoresGrid
          className="mb-4"
          itens={[
            { titulo: "Geradas", valor: moeda(c.geradas) },
            { titulo: "Pagas", valor: moeda(c.pagas), destaque: "ok" },
            { titulo: "Em aberto", valor: moeda(c.emAberto), destaque: "danger" },
            { titulo: "Taxa de recebimento", valor: pct(c.taxaRecebimentoPercentual) },
          ]}
        />
        <GraficoGeradoRecebido dados={c.porCompetencia} />
      </SecaoRelatorio>

      <SecaoRelatorio titulo="5 · Fluxo de caixa previsto" descricao="Próximos 30 / 60 / 90 dias (competências).">
        <GraficoBarrasSimples
          nome="Previsão"
          cor="#00427A"
          dados={painel.fluxoCaixaPrevisto.map((f) => ({ rotulo: f.rotulo, valor: f.previsao }))}
        />
        <div className="mt-3">
          <TabelaSimples
            colunas={[
              { chave: "mes", rotulo: "Mês" },
              { chave: "prev", rotulo: "Previsão", alinhar: "right" },
            ]}
            linhas={painel.fluxoCaixaPrevisto.map((f) => ({
              mes: f.rotulo,
              prev: moeda(f.previsao),
            }))}
            chave={(l) => String(l.mes)}
          />
        </div>
      </SecaoRelatorio>

      <SecaoRelatorio titulo="11 · DRE simplificado da carteira" descricao="Receitas próprias vs. pass-through.">
        <TabelaSimples
          colunas={[
            { chave: "item", rotulo: "Item" },
            { chave: "valor", rotulo: "Valor", alinhar: "right" },
          ]}
          linhas={[
            { item: "Receita de aluguel", valor: moeda(dre.receitaAluguel) },
            { item: "Multas", valor: moeda(dre.multas) },
            { item: "Juros", valor: moeda(dre.juros) },
            { item: "IPTU repassado", valor: moeda(dre.iptuRepassado) },
            { item: "Condomínio repassado", valor: moeda(dre.condominioRepassado) },
            { item: "Total movimentado", valor: moeda(dre.totalMovimentado) },
          ]}
          chave={(l, i) => `${String(l.item)}-${i}`}
        />
      </SecaoRelatorio>

      {painel.proprietarios.length > 1 ? (
        <SecaoRelatorio titulo="10 · Ranking de proprietários" descricao="Receita por cliente (locador) no período.">
          <TabelaSimples
            colunas={[
              { chave: "nome", rotulo: "Proprietário" },
              { chave: "rec", rotulo: "Receita", alinhar: "right" },
              { chave: "pct", rotulo: "% carteira", alinhar: "right" },
            ]}
            linhas={painel.proprietarios.map((p) => ({
              nome: p.clienteNome,
              rec: moeda(p.receita),
              pct: pct(p.percentualDaCarteira),
            }))}
            chave={(l, i) => `${String(l.nome)}-${i}`}
          />
        </SecaoRelatorio>
      ) : null}
    </>
  );
}

function AbaImoveis({ painel }: { painel: RelatorioPainel }) {
  const v = painel.vacancia;
  const inad = painel.inadimplentes;
  return (
    <>
      <SecaoRelatorio titulo="3 · Inadimplentes" descricao="Cobranças vencidas sem pagamento.">
        <IndicadoresGrid
          className="mb-4"
          itens={[
            { titulo: "Total inadimplente", valor: moeda(inad.totalInadimplente), destaque: "danger" },
            { titulo: "% da carteira", valor: pct(inad.percentualCarteira) },
            { titulo: "Devedores", valor: inad.quantidadeDevedores },
          ]}
        />
        <TabelaSimples
          colunas={[
            { chave: "inq", rotulo: "Inquilino" },
            { chave: "imov", rotulo: "Imóvel" },
            { chave: "valor", rotulo: "Valor", alinhar: "right" },
            { chave: "dias", rotulo: "Dias atraso", alinhar: "right" },
          ]}
          linhas={inad.itens.map((i) => ({
            inq: i.inquilinoNome,
            imov: i.imovelNome,
            valor: moeda(i.valor),
            dias: i.diasAtraso,
          }))}
          chave={(_, i) => String(inad.itens[i]?.nfseId ?? i)}
        />
      </SecaoRelatorio>

      <SecaoRelatorio titulo="4 · Ranking de atrasos" descricao="Média de dias em atraso por imóvel.">
        <TabelaSimples
          colunas={[
            { chave: "imov", rotulo: "Imóvel" },
            { chave: "media", rotulo: "Média dias", alinhar: "right" },
            { chave: "qtd", rotulo: "Ocorrências", alinhar: "right" },
          ]}
          linhas={painel.rankingAtrasos.map((r) => ({
            imov: r.imovelNome,
            media: r.mediaDiasAtraso,
            qtd: r.quantidadeAtrasos,
          }))}
          chave={(l, i) => `${String(l.imov)}-${i}`}
        />
      </SecaoRelatorio>

      <SecaoRelatorio titulo="6 · Rentabilidade por imóvel" descricao="Receita no período e média mensal.">
        <TabelaSimples
          colunas={[
            { chave: "imov", rotulo: "Imóvel" },
            { chave: "ano", rotulo: "Receita período", alinhar: "right" },
            { chave: "media", rotulo: "Média mensal", alinhar: "right" },
            { chave: "comps", rotulo: "Competências", alinhar: "right" },
          ]}
          linhas={painel.rentabilidadeImoveis.map((r) => ({
            imov: r.imovelNome,
            ano: moeda(r.receitaPeriodo),
            media: moeda(r.receitaMediaMensal),
            comps: r.competenciasComReceita,
          }))}
          chave={(l, i) => `${String(l.imov)}-${i}`}
        />
      </SecaoRelatorio>

      <SecaoRelatorio titulo="7 · Vacância" descricao="Ocupação da carteira e imóveis sem contrato ativo.">
        <IndicadoresGrid
          className="mb-4"
          itens={[
            { titulo: "Imóveis totais", valor: v.imoveisTotais },
            { titulo: "Ocupados", valor: v.ocupados, destaque: "ok" },
            { titulo: "Vagos", valor: v.vagos },
            { titulo: "Taxa de vacância", valor: pct(v.taxaVacanciaPercentual) },
          ]}
        />
        <TabelaSimples
          colunas={[
            { chave: "imov", rotulo: "Imóvel" },
            { chave: "dias", rotulo: "Dias vago", alinhar: "right" },
            { chave: "desde", rotulo: "Desde" },
          ]}
          linhas={v.imoveisVagosDetalhe.map((i) => ({
            imov: i.imovelNome,
            dias: i.diasVago,
            desde: formatarData(i.desde),
          }))}
          chave={(l, i) => `${String(l.imov)}-${i}`}
        />
      </SecaoRelatorio>
    </>
  );
}

function AbaContratos({ painel }: { painel: RelatorioPainel }) {
  const faixas = [
    { label: "Até 30 dias", max: 30 },
    { label: "31–60 dias", max: 60 },
    { label: "61–90 dias", max: 90 },
  ];
  return (
    <>
      <SecaoRelatorio
        titulo="8 · Contratos próximos do vencimento"
        descricao="Ajuda a evitar vacâncias inesperadas (faixas 30 / 60 / 90 dias)."
      >
        {faixas.map((f, idx) => {
          const min = idx === 0 ? 0 : faixas[idx - 1]!.max + 1;
          const itens = painel.contratosVencendo.filter(
            (c) => c.diasRestantes >= min && c.diasRestantes <= f.max,
          );
          return (
            <div key={f.label} className="mb-4 last:mb-0">
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {f.label} ({itens.length})
              </p>
              <TabelaSimples
                colunas={[
                  { chave: "contrato", rotulo: "Contrato" },
                  { chave: "venc", rotulo: "Vencimento" },
                  { chave: "dias", rotulo: "Dias", alinhar: "right" },
                ]}
                linhas={itens.map((c) => ({
                  contrato: `${c.inquilinoNome} / ${c.imovelNome}`,
                  venc: formatarData(c.dataFimPrevista),
                  dias: c.diasRestantes,
                }))}
                chave={(l, i) => `${String(l.contrato)}-${i}`}
              />
            </div>
          );
        })}
      </SecaoRelatorio>

      <SecaoRelatorio
        titulo="Score de risco do inquilino"
        descricao="Histórico de atrasos (quantidade e média de dias)."
      >
        <TabelaSimples
          colunas={[
            { chave: "nome", rotulo: "Inquilino" },
            { chave: "qtd", rotulo: "Atrasos", alinhar: "right" },
            { chave: "media", rotulo: "Média dias", alinhar: "right" },
            { chave: "risco", rotulo: "Risco" },
          ]}
          linhas={painel.scoresInquilinos.map((s) => ({
            nome: s.inquilinoNome,
            qtd: s.quantidadeAtrasos,
            media: s.mediaDiasAtraso,
            risco: s.risco,
          }))}
          chave={(l, i) => `${String(l.nome)}-${i}`}
        />
      </SecaoRelatorio>
    </>
  );
}

function AbaFiscal({ painel, ehAdmin }: { painel: RelatorioPainel; ehAdmin: boolean }) {
  const f = painel.fiscal;
  return (
    <>
      <SecaoRelatorio titulo="9 · Emissão de NFS-e" descricao="Painel fiscal do período filtrado.">
        <IndicadoresGrid
          className="mb-4"
          itens={[
            { titulo: "NFS-e emitidas", valor: f.emitidas },
            { titulo: "NFS-e canceladas", valor: f.canceladas },
            { titulo: "Valor faturado", valor: moeda(f.valorFaturadoEmitidas) },
            {
              titulo: "Emitidas sem pagamento",
              valor: f.notasSemPagamentoCorrespondente,
              destaque: "danger",
            },
            {
              titulo: "Cobranças sem nota emitida",
              valor: f.cobrancasSemNotaEmitida,
            },
          ]}
        />
        <GraficoBarrasSimples
          nome="Valor faturado"
          dados={f.porCompetencia.map((c) => ({ rotulo: c.rotulo, valor: c.valor }))}
        />
        <div className="mt-3">
          <TabelaSimples
            colunas={[
              { chave: "comp", rotulo: "Competência" },
              { chave: "qtd", rotulo: "Qtd.", alinhar: "right" },
              { chave: "valor", rotulo: "Valor", alinhar: "right" },
            ]}
            linhas={f.porCompetencia.map((c) => ({
              comp: c.rotulo,
              qtd: c.quantidade,
              valor: moeda(c.valor),
            }))}
            chave={(l, i) => `${String(l.comp)}-${i}`}
          />
        </div>
      </SecaoRelatorio>
      {ehAdmin && painel.proprietarios.length > 0 ? (
        <SecaoRelatorio titulo="Receita fiscal por proprietário">
          <TabelaSimples
            colunas={[
              { chave: "nome", rotulo: "Proprietário" },
              { chave: "rec", rotulo: "Receita", alinhar: "right" },
            ]}
            linhas={painel.proprietarios.map((p) => ({
              nome: p.clienteNome,
              rec: moeda(p.receita),
            }))}
            chave={(l, i) => `${String(l.nome)}-${i}`}
          />
        </SecaoRelatorio>
      ) : null}
    </>
  );
}
