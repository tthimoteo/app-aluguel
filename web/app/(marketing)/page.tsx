import { LandingPage } from "@/components/landing/landing-page";
import { listarPlanosPublicos } from "@/lib/api/planos-publicos";

export const metadata = {
  title: "APP Aluguel · Lucrare",
  description:
    "Gerencie imóveis, receba aluguéis via PIX e emita NFS-e. Simples, preparado para a Reforma Tributária.",
};

export default async function MarketingHomePage() {
  const planos = await listarPlanosPublicos();
  return <LandingPage planos={planos} />;
}
