import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { PageHeader } from "@/components/data/page-header";
import { FormularioMinhaConta } from "@/components/minha-conta/formulario-minha-conta";
import { ApiError, api } from "@/lib/api/server";
import { requireSession, temPerfil } from "@/lib/auth/session";
import { nomeCliente } from "@/lib/format";

export default async function EditarClientePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const usuario = await requireSession();
  if (!temPerfil(usuario, "Administrador")) redirect("/");

  const { id } = await params;
  const me = await api.me();

  let cliente;
  try {
    cliente = await api.cliente(id);
  } catch (e) {
    if (e instanceof ApiError && (e.status === 404 || e.status === 400)) notFound();
    throw e;
  }

  const [planos, certificado] = await Promise.all([
    api.planos(),
    api.certificadoCliente(id),
  ]);

  return (
    <div>
      <p className="mb-3">
        <Link href="/clientes" className="text-sm font-medium text-primary hover:text-primary/80">
          ← Clientes
        </Link>
      </p>
      <PageHeader
        titulo={nomeCliente(cliente)}
        descricao="Edite cadastro, status operacional (Ativo/Inativo/Bloqueado) e plano de assinatura."
      />
      <FormularioMinhaConta
        me={me}
        cliente={cliente}
        planos={planos}
        certificado={certificado}
        variante="admin"
      />
    </div>
  );
}
