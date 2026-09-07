import {
  BarChart3,
  Building2,
  Calculator,
  FileText,
  Home,
  Landmark,
  ScrollText,
  User,
  UserCog,
  Users,
  type LucideIcon,
} from "lucide-react";
import type { NavItem } from "@/lib/navigation";

const ICONES: Record<NavItem["icone"], LucideIcon> = {
  home: Home,
  building: Building2,
  users: Users,
  file: FileText,
  userCog: UserCog,
  user: User,
  barChart: BarChart3,
  calculator: Calculator,
  scroll: ScrollText,
  landmark: Landmark,
};

export function NavIcon({ name, className }: { name: NavItem["icone"]; className?: string }) {
  const Icon = ICONES[name];
  return <Icon className={className} />;
}
