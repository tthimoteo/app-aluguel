import { PageHeader } from "@/components/data/page-header";
import { FormularioMinhaConta } from "@/components/minha-conta/formulario-minha-conta";
import { api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { redirect } from "next/navigation";

export default async function MinhaContaPage() {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Gestor")) redirect("/");

  const me = await api.me();
  const clienteId = me.clienteId ?? usuario.clienteId;
  if (!clienteId) {
    return (
      <div>
        <PageHeader
          titulo="Minha Conta"
          descricao="Não há cliente vinculado à sessão atual."
        />
      </div>
    );
  }

  const [cliente, planos, certificado] = await Promise.all([
    api.cliente(clienteId),
    api.planos(),
    api.certificadoCliente(clienteId),
  ]);

  return (
    <div>
      <PageHeader
        titulo="Minha Conta"
        descricao="Edite os dados cadastrais do cliente, consulte o plano vigente e altere a assinatura quando necessário."
      />
      <FormularioMinhaConta
        me={me}
        cliente={cliente}
        planos={planos}
        certificado={certificado}
      />
    </div>
  );
}
