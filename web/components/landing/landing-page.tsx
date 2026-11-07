import Link from "next/link";
import type { ReactNode } from "react";
import {
  Building2,
  Check,
  CircleAlert,
  FileSpreadsheet,
  Landmark,
  MessageCircle,
  Receipt,
  ShieldCheck,
  Wallet,
  X,
} from "lucide-react";
import { SiteHeader } from "@/components/landing/site-header";
import { beneficiosDoPlano, precoRotulo } from "@/lib/planos";
import type { Plano } from "@/lib/api/types";
import { cn } from "@/lib/utils";

const DORES = [
  {
    titulo: "Cobranças esquecidas",
    texto: "Aluguéis vencem e o controle fica espalhado em mensagens, planilhas e e-mails.",
  },
  {
    titulo: "Falta de controle financeiro",
    texto: "Dificuldade para saber quanto foi recebido, quanto está em atraso e qual imóvel gera mais renda.",
  },
  {
    titulo: "Exigências fiscais aumentando",
    texto: "A Reforma Tributária exige cada vez mais controle sobre a emissão de documentos fiscais.",
  },
  {
    titulo: "Sistemas caros e complexos",
    texto: "ERPs imobiliários normalmente possuem custo elevado e recursos desnecessários para a maioria dos locadores.",
  },
] as const;

const FUNCIONALIDADES = [
  {
    titulo: "Cadastro de imóveis",
    itens: ["Casas", "Apartamentos", "Salas comerciais", "Galpões", "Terrenos"],
  },
  {
    titulo: "Cadastro de inquilinos",
    itens: ["Pessoa Física", "Pessoa Jurídica"],
  },
  {
    titulo: "Gestão de contratos",
    itens: ["Início e término", "Reajustes", "Históricos"],
  },
  {
    titulo: "Cobranças automáticas",
    itens: ["Aluguel", "IPTU", "Condomínio", "Multas", "Juros"],
  },
  {
    titulo: "Recebimento por PIX",
    itens: ["QR Code", "Baixa automática", "Histórico de pagamentos"],
  },
  {
    titulo: "Emissão de NFS-e",
    itens: ["Integrado ao Portal Nacional", "Preparado para exigências fiscais atuais"],
  },
] as const;

const REFORMA = [
  "Emitir NFS-e dos aluguéis",
  "Armazenar documentos fiscais",
  "Organizar receitas por competência",
  "Facilitar futuras adequações à CBS e IBS",
  "Manter histórico completo das operações",
] as const;

const PERFIS = [
  {
    titulo: "Pessoa Física",
    texto: "Possui um ou vários imóveis alugados.",
    exemplos: ["Casas", "Apartamentos", "Salões comerciais"],
  },
  {
    titulo: "Pessoa Jurídica",
    texto: "Empresas patrimoniais e holdings imobiliárias.",
    exemplos: ["CNPJ próprio", "Múltiplos imóveis", "Controle centralizado"],
  },
  {
    titulo: "Administradoras",
    texto: "Controle de múltiplos proprietários e emissão em escala.",
    exemplos: ["Carteira de imóveis", "Operação em volume", "Equipe colaborativa"],
  },
] as const;

const COMPARATIVO: { recurso: string; planilha: "ok" | "nao"; erp: "ok" | "nao" | "alguns"; nosso: "ok" }[] = [
  { recurso: "Controle de imóveis", planilha: "ok", erp: "ok", nosso: "ok" },
  { recurso: "Controle de inquilinos", planilha: "nao", erp: "ok", nosso: "ok" },
  { recurso: "Cobrança de aluguel", planilha: "nao", erp: "ok", nosso: "ok" },
  { recurso: "IPTU e condomínio", planilha: "nao", erp: "ok", nosso: "ok" },
  { recurso: "PIX integrado", planilha: "nao", erp: "ok", nosso: "ok" },
  { recurso: "Emissão de NFS-e", planilha: "nao", erp: "alguns", nosso: "ok" },
  { recurso: "Simplicidade", planilha: "ok", erp: "nao", nosso: "ok" },
  { recurso: "Custo reduzido", planilha: "ok", erp: "nao", nosso: "ok" },
];

const INDICADORES = [
  { titulo: "Receita por período", texto: "Aluguel, IPTU e condomínio consolidados." },
  { titulo: "Inadimplência", texto: "Quem está devendo, quanto e há quantos dias." },
  { titulo: "Cobrado × recebido", texto: "Controle financeiro completo da carteira." },
  { titulo: "Rentabilidade", texto: "Quais imóveis geram maior retorno." },
  { titulo: "Vacância", texto: "Imóveis ocupados e vagos em um só olhar." },
] as const;

const FAQ = [
  {
    q: "Preciso instalar algo?",
    a: "Não. O sistema funciona totalmente online.",
  },
  {
    q: "Posso controlar IPTU e condomínio separadamente?",
    a: "Sim.",
  },
  {
    q: "Posso emitir NFS-e?",
    a: "Sim, nos planos que incluem emissão via API Nacional da NFS-e.",
  },
  {
    q: "É compatível com a Reforma Tributária?",
    a: "O sistema foi desenvolvido considerando a evolução das obrigações fiscais e continuará sendo atualizado conforme a legislação.",
  },
  {
    q: "Posso administrar vários proprietários?",
    a: "Sim.",
  },
] as const;

function Celula({ valor }: { valor: "ok" | "nao" | "alguns" }) {
  if (valor === "ok") return <Check className="mx-auto size-5 text-[#27ae60]" aria-label="Sim" />;
  if (valor === "nao") return <X className="mx-auto size-5 text-[#e74c3c]" aria-label="Não" />;
  return <span className="text-sm font-semibold text-[#6c757d]">Alguns</span>;
}

function CtaPrimario({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="inline-flex h-11 items-center justify-center rounded-[4px] bg-[#e85d15] px-5 text-sm font-bold text-white shadow-[0_2px_8px_rgba(232,93,21,0.28)] transition hover:-translate-y-0.5 hover:bg-[#c55a32] hover:shadow-[0_4px_12px_rgba(232,93,21,0.35)]"
    >
      {children}
    </Link>
  );
}

function CtaSecundario({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="inline-flex h-11 items-center justify-center rounded-[4px] border border-white/40 bg-white/10 px-5 text-sm font-bold text-white backdrop-blur transition hover:bg-white/20"
    >
      {children}
    </Link>
  );
}

export function LandingPage({ planos }: { planos: Plano[] }) {
  const planosVitrine = planos
    .filter((p) => p.codigo !== "TRIAL")
    .sort((a, b) => (a.valorMensal ?? 9999) - (b.valorMensal ?? 9999));

  return (
    <>
      <SiteHeader />
      <main>
        {/* Hero */}
        <section className="relative overflow-hidden">
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,rgba(232,93,21,0.18),transparent_55%),linear-gradient(135deg,#003366_0%,#00427a_48%,#2c3e50_100%)]" />
          <div className="absolute -top-24 -right-16 size-72 rounded-full bg-[#e85d15]/20 blur-2xl" />
          <div className="absolute bottom-0 left-1/4 size-56 rounded-full bg-white/10 blur-3xl" />
          <div className="relative mx-auto grid max-w-6xl gap-10 px-4 py-16 md:grid-cols-[1.15fr_0.85fr] md:items-center md:px-6 md:py-24">
            <div className="animate-[fadeUp_0.7s_ease-out]">
              <p className="mb-3 text-xs font-bold tracking-[0.22em] text-[#f4a574] uppercase">
                Lucrare · APP Aluguel
              </p>
              <h1 className="max-w-xl text-3xl font-bold leading-tight text-white md:text-5xl md:leading-[1.15]">
                Gerencie seus imóveis, receba seus aluguéis e emita NFS-e em um único sistema.
              </h1>
              <p className="mt-5 max-w-xl text-base leading-relaxed text-white/85 md:text-lg">
                Controle imóveis, inquilinos, contratos, cobranças por PIX e emissão de NFS-e com uma
                plataforma simples, preparada para a Reforma Tributária e sem a complexidade dos ERPs
                tradicionais.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <CtaPrimario href="/cadastro">Testar Gratuitamente</CtaPrimario>
                <CtaSecundario href="/cadastro?modo=assinar">Assinar Agora</CtaSecundario>
                <CtaSecundario href="/#planos">Ver Planos</CtaSecundario>
              </div>
              <p className="mt-6 flex items-start gap-2 text-sm text-white/75">
                <ShieldCheck className="mt-0.5 size-4 shrink-0 text-[#f4a574]" />
                Você não precisa de imobiliária nem de ERP complexo — só de uma ferramenta simples para
                administrar, receber e ficar em conformidade.
              </p>
            </div>
            <div className="relative mx-auto w-full max-w-md animate-[fadeUp_0.9s_ease-out]">
              <div className="absolute -inset-6 rounded-full border border-[#e85d15]/40" />
              <div className="absolute -inset-12 rounded-full border border-white/10" />
              <div className="relative overflow-hidden rounded-[12px] bg-white p-6 shadow-[0_10px_25px_rgba(0,0,0,0.25)]">
                <div className="mb-4 flex items-center gap-3">
                  <div className="flex size-12 items-center justify-center rounded-full bg-[#003366] text-white">
                    <Building2 className="size-6" />
                  </div>
                  <div>
                    <p className="text-xs font-semibold tracking-wide text-[#e85d15] uppercase">
                      Visão da carteira
                    </p>
                    <p className="font-bold text-[#2c3e50]">Seus aluguéis, no controle</p>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3 text-sm">
                  {[
                    { rotulo: "Recebido no mês", valor: "R$ 18.450" },
                    { rotulo: "Em atraso", valor: "2 contratos" },
                    { rotulo: "Ocupação", valor: "92%" },
                    { rotulo: "NFS-e emitidas", valor: "14" },
                  ].map((item) => (
                    <div key={item.rotulo} className="rounded-[8px] bg-[#f8f9fa] p-3">
                      <p className="text-xs text-[#6c757d]">{item.rotulo}</p>
                      <p className="mt-1 font-bold text-[#2c3e50]">{item.valor}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Dor */}
        <section id="dor" className="scroll-mt-20 bg-[#f9f9f5] py-16 md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="max-w-3xl text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">
              Ainda controla seus aluguéis por planilhas ou WhatsApp?
            </h2>
            <p className="mt-3 max-w-2xl text-[#6c757d]">
              Seu concorrente também é o Excel, o papel, o WhatsApp e cobranças manuais — não só o ERP
              caro.
            </p>
            <div className="mt-10 grid gap-4 sm:grid-cols-2">
              {DORES.map((dor) => (
                <article
                  key={dor.titulo}
                  className="rounded-[8px] border border-[#e9ecef] bg-white p-5 shadow-[0_2px_8px_rgba(0,0,0,0.06)] transition hover:-translate-y-0.5 hover:shadow-[0_4px_12px_rgba(0,0,0,0.1)]"
                >
                  <div className="mb-3 flex size-10 items-center justify-center rounded-full bg-[#fff1e8] text-[#e85d15]">
                    <CircleAlert className="size-5" />
                  </div>
                  <h3 className="font-bold text-[#2c3e50]">{dor.titulo}</h3>
                  <p className="mt-2 text-sm leading-relaxed text-[#6c757d]">{dor.texto}</p>
                </article>
              ))}
            </div>
            <div className="mt-8 flex flex-wrap gap-3 text-sm text-[#6c757d]">
              {[
                { icon: FileSpreadsheet, label: "Planilhas Excel" },
                { icon: MessageCircle, label: "WhatsApp" },
                { icon: Wallet, label: "Cobranças manuais" },
                { icon: Landmark, label: "ERPs complexos" },
              ].map((item) => (
                <span
                  key={item.label}
                  className="inline-flex items-center gap-2 rounded-[12px] border border-[#e9ecef] bg-white px-3 py-1.5"
                >
                  <item.icon className="size-4 text-[#e85d15]" />
                  {item.label}
                </span>
              ))}
            </div>
          </div>
        </section>

        {/* Solução */}
        <section id="solucao" className="scroll-mt-20 bg-white py-16 md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">
              Tudo o que você precisa para administrar imóveis de aluguel
            </h2>
            <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {FUNCIONALIDADES.map((bloco, i) => (
                <article
                  key={bloco.titulo}
                  className={cn(
                    "rounded-[8px] p-5",
                    i % 2 === 0
                      ? "bg-[#003366] text-white shadow-[0_2px_8px_rgba(0,51,102,0.25)] ring-2 ring-[#e85d15]/70"
                      : "border border-[#003366]/20 bg-white text-[#003366]",
                  )}
                >
                  <h3 className="text-lg font-bold">{bloco.titulo}</h3>
                  <ul className="mt-3 space-y-1.5 text-sm">
                    {bloco.itens.map((item) => (
                      <li key={item} className="flex items-center gap-2">
                        <Check className={cn("size-4 shrink-0", i % 2 === 0 ? "text-[#f4a574]" : "text-[#e85d15]")} />
                        {item}
                      </li>
                    ))}
                  </ul>
                </article>
              ))}
            </div>
          </div>
        </section>

        {/* Reforma */}
        <section id="reforma" className="scroll-mt-20 bg-[#003366] py-16 text-white md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <div className="grid gap-10 md:grid-cols-[1fr_1fr] md:items-center">
              <div>
                <p className="text-xs font-bold tracking-[0.2em] text-[#f4a574] uppercase">Diferencial</p>
                <h2 className="mt-2 text-2xl font-bold md:text-3xl">
                  Sua locação preparada para a Reforma Tributária
                </h2>
                <p className="mt-4 text-white/80">
                  A legislação tributária brasileira está evoluindo e o controle fiscal das operações
                  imobiliárias exige cada vez mais organização.
                </p>
                <p className="mt-6 rounded-[8px] border border-white/15 bg-white/5 p-4 text-sm leading-relaxed text-white/90">
                  Enquanto você administra seus imóveis, nós cuidamos da tecnologia para acompanhar as
                  mudanças legais.
                </p>
              </div>
              <ul className="space-y-3">
                {REFORMA.map((item) => (
                  <li
                    key={item}
                    className="flex items-start gap-3 rounded-[8px] bg-white/10 px-4 py-3 text-sm font-medium"
                  >
                    <Check className="mt-0.5 size-5 shrink-0 text-[#e85d15]" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </section>

        {/* Para quem */}
        <section className="bg-[#f9f9f5] py-16 md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">
              Ideal para qualquer perfil de locador
            </h2>
            <div className="mt-10 grid gap-4 md:grid-cols-3">
              {PERFIS.map((perfil) => (
                <article
                  key={perfil.titulo}
                  className="rounded-[8px] border border-[#e9ecef] bg-white p-5 shadow-[0_2px_8px_rgba(0,0,0,0.06)]"
                >
                  <h3 className="text-lg font-bold text-[#2c3e50]">{perfil.titulo}</h3>
                  <p className="mt-2 text-sm text-[#6c757d]">{perfil.texto}</p>
                  <ul className="mt-4 space-y-1.5 text-sm text-[#2c3e50]">
                    {perfil.exemplos.map((ex) => (
                      <li key={ex} className="flex items-center gap-2">
                        <span className="size-1.5 rounded-full bg-[#e85d15]" />
                        {ex}
                      </li>
                    ))}
                  </ul>
                </article>
              ))}
            </div>
          </div>
        </section>

        {/* Comparativo */}
        <section className="bg-white py-16 md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">
              Por que escolher nossa plataforma?
            </h2>
            <div className="mt-8 overflow-x-auto rounded-[8px] border border-[#e9ecef] shadow-[0_2px_8px_rgba(0,0,0,0.05)]">
              <table className="min-w-full text-left text-sm">
                <thead className="bg-[#2c3e50] text-white">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Recurso</th>
                    <th className="px-4 py-3 text-center font-semibold">Planilha</th>
                    <th className="px-4 py-3 text-center font-semibold">ERP Imobiliário</th>
                    <th className="px-4 py-3 text-center font-semibold">Nosso sistema</th>
                  </tr>
                </thead>
                <tbody>
                  {COMPARATIVO.map((linha, i) => (
                    <tr key={linha.recurso} className={i % 2 ? "bg-[#f8f9fa]" : "bg-white"}>
                      <td className="px-4 py-3 font-medium text-[#2c3e50]">{linha.recurso}</td>
                      <td className="px-4 py-3 text-center">
                        <Celula valor={linha.planilha} />
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Celula valor={linha.erp} />
                      </td>
                      <td className="px-4 py-3 text-center">
                        <Celula valor={linha.nosso} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>

        {/* Indicadores */}
        <section className="relative overflow-hidden bg-[#e85d15] py-16 text-white md:py-20">
          <div className="absolute -top-10 -left-10 size-40 rounded-full bg-white/10" />
          <div className="absolute -right-8 bottom-0 size-56 rounded-full bg-[#003366]/20" />
          <div className="relative mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="text-2xl font-bold md:text-3xl">Tenha visão completa da sua carteira</h2>
            <p className="mt-3 max-w-2xl text-white/90">
              Relatórios pensados para quem precisa decidir com clareza — sem dashboard inchado.
            </p>
            <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
              {INDICADORES.map((ind) => (
                <article
                  key={ind.titulo}
                  className="rounded-[8px] border border-white/20 bg-white/10 p-4 backdrop-blur"
                >
                  <Receipt className="mb-3 size-5 text-white" />
                  <h3 className="font-bold">{ind.titulo}</h3>
                  <p className="mt-2 text-sm text-white/85">{ind.texto}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        {/* Depoimento institucional */}
        <section className="bg-[#f9f9f5] py-16 md:py-20">
          <div className="mx-auto max-w-3xl px-4 text-center md:px-6">
            <div className="mx-auto mb-6 flex size-16 items-center justify-center rounded-full bg-[#003366] ring-4 ring-[#e85d15]/40">
              <Building2 className="size-7 text-white" />
            </div>
            <blockquote className="text-xl font-semibold leading-relaxed text-[#003366] md:text-2xl">
              “Desenvolvido para simplificar a vida de proprietários e administradores de imóveis,
              eliminando controles manuais e reduzindo riscos fiscais.”
            </blockquote>
            <p className="mt-4 text-sm font-bold tracking-wide text-[#e85d15] uppercase">Lucrare Contabilidade</p>
          </div>
        </section>

        {/* Planos */}
        <section id="planos" className="scroll-mt-20 bg-white py-16 md:py-20">
          <div className="mx-auto max-w-6xl px-4 md:px-6">
            <h2 className="text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">Planos</h2>
            <p className="mt-3 max-w-2xl text-[#6c757d]">
              Comece no Trial gratuito ou assine com PIX. O plano Pro é sob consulta comercial.
            </p>
            <div className="mt-10 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              {planosVitrine.map((plano) => {
                const sobConsulta = plano.valorMensal == null;
                return (
                  <article
                    key={plano.id}
                    className="flex flex-col rounded-[8px] border border-[#e9ecef] bg-[#f9f9f5] p-5 shadow-[0_2px_8px_rgba(0,0,0,0.05)]"
                  >
                    <h3 className="text-lg font-bold text-[#2c3e50]">{plano.nome}</h3>
                    <p className="mt-2 text-2xl font-bold text-[#e85d15]">{precoRotulo(plano)}</p>
                    <ul className="mt-4 flex-1 space-y-2 text-sm text-[#6c757d]">
                      {beneficiosDoPlano(plano)
                        .slice(0, 5)
                        .map((b) => (
                          <li key={b} className="flex items-start gap-2">
                            <Check className="mt-0.5 size-4 shrink-0 text-[#27ae60]" />
                            <span>{b}</span>
                          </li>
                        ))}
                    </ul>
                    {sobConsulta ? (
                      <Link
                        href="/demonstracao"
                        className="mt-6 inline-flex h-10 items-center justify-center rounded-[4px] border border-[#003366] px-4 text-sm font-bold text-[#003366] transition hover:bg-[#003366] hover:text-white"
                      >
                        Solicitar Demonstração
                      </Link>
                    ) : (
                      <Link
                        href={`/cadastro?plano=${plano.id}&modo=assinar`}
                        className="mt-6 inline-flex h-10 items-center justify-center rounded-[4px] bg-[#e85d15] px-4 text-sm font-bold text-white transition hover:bg-[#c55a32]"
                      >
                        Assinar Agora
                      </Link>
                    )}
                  </article>
                );
              })}
            </div>
            <div className="mt-6">
              <Link href="/cadastro" className="text-sm font-semibold text-[#e85d15] hover:underline">
                Prefere testar 7 dias grátis? Criar conta Trial →
              </Link>
            </div>
          </div>
        </section>

        {/* FAQ */}
        <section id="faq" className="scroll-mt-20 bg-[#f9f9f5] py-16 md:py-20">
          <div className="mx-auto max-w-3xl px-4 md:px-6">
            <h2 className="text-2xl font-bold tracking-tight text-[#003366] md:text-3xl">FAQ</h2>
            <div className="mt-8 space-y-3">
              {FAQ.map((item) => (
                <details
                  key={item.q}
                  className="group rounded-[8px] border border-[#e9ecef] bg-white px-4 py-3 open:shadow-[0_2px_8px_rgba(0,0,0,0.06)]"
                >
                  <summary className="cursor-pointer list-none font-semibold text-[#2c3e50] marker:content-none">
                    <span className="flex items-center justify-between gap-3">
                      {item.q}
                      <span className="text-[#e85d15] transition group-open:rotate-45">+</span>
                    </span>
                  </summary>
                  <p className="mt-2 text-sm leading-relaxed text-[#6c757d]">{item.a}</p>
                </details>
              ))}
            </div>
          </div>
        </section>

        {/* CTA final */}
        <section className="bg-[#003366] py-16 text-white md:py-20">
          <div className="mx-auto max-w-3xl px-4 text-center md:px-6">
            <h2 className="text-2xl font-bold md:text-3xl">
              Comece a administrar seus imóveis de forma profissional
            </h2>
            <p className="mt-4 text-white/85">
              Pare de usar planilhas e controles manuais. Centralize contratos, cobranças, recebimentos e
              emissão de NFS-e em uma única plataforma.
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-3">
              <CtaPrimario href="/cadastro">Criar Conta Gratuita</CtaPrimario>
              <CtaSecundario href="/cadastro?modo=assinar">Assinar Plano</CtaSecundario>
              <CtaSecundario href="/demonstracao">Ver Demonstração</CtaSecundario>
            </div>
          </div>
        </section>
      </main>

      <footer className="border-t border-[#e9ecef] bg-white py-8">
        <div className="mx-auto flex max-w-6xl flex-col gap-3 px-4 text-sm text-[#6c757d] md:flex-row md:items-center md:justify-between md:px-6">
          <p>© {new Date().getFullYear()} Lucrare Contabilidade · APP Aluguel</p>
          <div className="flex gap-4">
            <Link href="/login" className="hover:text-[#e85d15]">
              Entrar
            </Link>
            <Link href="/cadastro" className="hover:text-[#e85d15]">
              Cadastro
            </Link>
            <a href="https://www.lucrarecontabilidade.com.br/" className="hover:text-[#e85d15]">
              Site institucional
            </a>
          </div>
        </div>
      </footer>

      <style>{`
        @keyframes fadeUp {
          from { opacity: 0; transform: translateY(16px); }
          to { opacity: 1; transform: translateY(0); }
        }
      `}</style>
    </>
  );
}
