import { ArrowLeftRight, Bot, BookOpen, Coins, StickyNote, Wallet } from "lucide-react";

// No "Ajustes" entry: that page doesn't exist yet, and a nav link to a 404 is a broken
// path, not a placeholder — add it back once /settings actually ships something.
export const NAV_ITEMS = [
  { href: "/dashboard", label: "Dashboard", icon: BookOpen },
  { href: "/portfolio", label: "Portfolio", icon: Wallet },
  // shortLabel: the mobile nav fits six equal columns at 390px; "Criptomonedas"
  // does not fit one, so it gets a short form there (the sidebar keeps label).
  { href: "/cryptocurrencies", label: "Criptomonedas", shortLabel: "Cripto", icon: Coins },
  { href: "/exchanges", label: "Exchanges", icon: ArrowLeftRight },
  { href: "/notes", label: "Notas", icon: StickyNote },
  { href: "/agent", label: "Agente", icon: Bot },
] as const;
