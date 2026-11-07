import { PageHeader } from "@/components/data/page-header";
import { PainelPagamentoPix } from "@/components/minha-conta/painel-pagamento-pix";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function PagamentoPixPage({
  params,
}: {
  params: Promise<{ pagamentoId: string }>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Gestor")) redirect("/inicio");

  const { pagamentoId } = await params;
  const pagamento = await api.pagamentoPlano(pagamentoId);

  return (
    <div>
      <PageHeader
        titulo="Pagamento da assinatura"
        descricao="Escaneie o QR Code PIX ou use o copia e cola. A assinatura é ativada automaticamente após a liquidação."
      />
      <PainelPagamentoPix
        pagamentoInicial={pagamento}
        permitirSimular={process.env.NODE_ENV === "development"}
      />
    </div>
  );
}
