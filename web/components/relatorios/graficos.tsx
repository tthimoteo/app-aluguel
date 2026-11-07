"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatarMoeda } from "@/lib/format";
import type { SerieGeradoRecebido, SerieMensal } from "@/lib/api/relatorios";

const tooltipStyle = {
  borderRadius: 4,
  border: "1px solid #E9ECEF",
  fontSize: 12,
};

function formatTick(v: number) {
  if (Math.abs(v) >= 1000) return `${(v / 1000).toLocaleString("pt-BR", { maximumFractionDigits: 1 })}k`;
  return v.toLocaleString("pt-BR");
}

export function GraficoEvolucaoRendimento({ dados }: { dados: SerieMensal[] }) {
  if (dados.length === 0) {
    return <p className="text-sm text-muted-foreground">Sem dados no período.</p>;
  }
  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={dados} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#ECF0F1" />
          <XAxis dataKey="rotulo" tick={{ fontSize: 11 }} />
          <YAxis tickFormatter={formatTick} tick={{ fontSize: 11 }} width={48} />
          <Tooltip
            formatter={(value) => formatarMoeda(Number(value ?? 0))}
            contentStyle={tooltipStyle}
          />
          <Legend />
          <Bar dataKey="aluguel" name="Aluguel" stackId="a" fill="#DB6838" radius={[0, 0, 0, 0]} />
          <Bar dataKey="iptu" name="IPTU" stackId="a" fill="#2C3E50" />
          <Bar dataKey="condominio" name="Condomínio" stackId="a" fill="#95A5A6" radius={[2, 2, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

export function GraficoGeradoRecebido({ dados }: { dados: SerieGeradoRecebido[] }) {
  if (dados.length === 0) {
    return <p className="text-sm text-muted-foreground">Sem cobranças no período.</p>;
  }
  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={dados} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#ECF0F1" />
          <XAxis dataKey="rotulo" tick={{ fontSize: 11 }} />
          <YAxis tickFormatter={formatTick} tick={{ fontSize: 11 }} width={48} />
          <Tooltip
            formatter={(value) => formatarMoeda(Number(value ?? 0))}
            contentStyle={tooltipStyle}
          />
          <Legend />
          <Bar dataKey="gerado" name="Gerado" fill="#2C3E50" radius={[2, 2, 0, 0]} />
          <Bar dataKey="recebido" name="Recebido" fill="#27AE60" radius={[2, 2, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

export function GraficoBarrasSimples({
  dados,
  dataKey,
  nome,
  cor = "#DB6838",
}: {
  dados: { rotulo: string; valor: number }[];
  dataKey?: string;
  nome: string;
  cor?: string;
}) {
  const key = dataKey ?? "valor";
  if (dados.length === 0) {
    return <p className="text-sm text-muted-foreground">Sem dados.</p>;
  }
  return (
    <div className="h-64 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={dados} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#ECF0F1" />
          <XAxis dataKey="rotulo" tick={{ fontSize: 11 }} />
          <YAxis tickFormatter={formatTick} tick={{ fontSize: 11 }} width={48} />
          <Tooltip
            formatter={(value) => formatarMoeda(Number(value ?? 0))}
            contentStyle={tooltipStyle}
          />
          <Bar dataKey={key} name={nome} fill={cor} radius={[2, 2, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
