import Link from "next/link";
import { Logo } from "@/components/brand/logo";
import { SidebarNav } from "@/components/layout/sidebar-nav";
import type { NavItem } from "@/lib/navigation";

export function AppSidebar({ itens }: { itens: NavItem[] }) {
  return (
    <aside className="hidden h-svh w-64 shrink-0 flex-col bg-sidebar text-sidebar-foreground md:flex">
      <div className="border-b border-sidebar-border px-4 py-4">
        <Link href="/" className="block">
          <Logo />
        </Link>
      </div>
      <div className="flex-1 overflow-y-auto py-3">
        <SidebarNav itens={itens} />
      </div>
      <p className="border-t border-sidebar-border px-4 py-3 text-[11px] text-white/50">
        Lucrare Contabilidade · APP Aluguel
      </p>
    </aside>
  );
}
