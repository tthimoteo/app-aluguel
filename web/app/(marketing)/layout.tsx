import { Montserrat } from "next/font/google";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

const montserrat = Montserrat({
  subsets: ["latin"],
  variable: "--font-landing",
  display: "swap",
});

export default function MarketingLayout({ children }: { children: ReactNode }) {
  return (
    <div
      className={cn(
        montserrat.variable,
        "min-h-svh bg-[#f9f9f5] text-[#2c3e50] antialiased",
        "font-[family-name:var(--font-landing),Montserrat,sans-serif]",
      )}
    >
      {children}
    </div>
  );
}
