import { AppHeader } from "@/components/layout/app-header";
import { AppSidebar } from "@/components/layout/app-sidebar";
import { requireSession } from "@/lib/auth/session";
import { itensVisiveis } from "@/lib/navigation";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const usuario = await requireSession();
  const itens = itensVisiveis(usuario.roles);

  return (
    <div className="flex min-h-svh bg-background">
      <AppSidebar itens={itens} />
      <div className="flex min-w-0 flex-1 flex-col">
        <AppHeader usuario={usuario} itens={itens} />
        <main className="flex-1 overflow-auto p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
