import { Suspense } from "react";
import { FormularioCadastro } from "@/components/landing/formulario-cadastro";
import { listarPlanosPublicos } from "@/lib/api/planos-publicos";

export const metadata = { title: "Criar conta" };

async function CadastroConteudo({
  searchParams,
}: {
  searchParams: Promise<{ plano?: string; modo?: string }>;
}) {
  const sp = await searchParams;
  const planos = await listarPlanosPublicos();
  const modo = sp.modo === "assinar" ? "assinar" : "trial";
  return (
    <FormularioCadastro planos={planos} planoInicialId={sp.plano ?? null} modoInicial={modo} />
  );
}

export default function CadastroPage({
  searchParams,
}: {
  searchParams: Promise<{ plano?: string; modo?: string }>;
}) {
  return (
    <Suspense fallback={<div className="p-8 text-center text-sm text-[#6c757d]">Carregando…</div>}>
      <CadastroConteudo searchParams={searchParams} />
    </Suspense>
  );
}
