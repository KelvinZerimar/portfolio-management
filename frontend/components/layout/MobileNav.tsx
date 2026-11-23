"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { NAV_ITEMS } from "./navItems";

// Six equal columns, icon over label: every destination visible at 390px with
// no horizontal scroll. The old scrolling strip cut off at "Exc…" with no hint
// that more items existed. Each cell is ≥ 48px tall (touch target).
export function MobileNav() {
  const pathname = usePathname();

  return (
    <nav
      aria-label="Navegación principal"
      className="grid grid-cols-6 bg-paper shadow-[0_1px_0_rgba(16,24,40,0.06)] md:hidden"
    >
      {NAV_ITEMS.map((item) => {
        const { href, label, icon: Icon } = item;
        const shortLabel = "shortLabel" in item ? item.shortLabel : label;
        const active = pathname === href || pathname?.startsWith(`${href}/`);
        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? "page" : undefined}
            className={
              "flex min-h-12 min-w-0 flex-col items-center justify-center gap-0.5 border-b-2 px-0.5 py-1.5 text-[11px] transition-colors " +
              (active
                ? "border-brand text-brand font-semibold"
                : "border-transparent text-ink-muted")
            }
          >
            <Icon size={16} strokeWidth={1.75} aria-hidden />
            <span className="w-full truncate text-center">{shortLabel}</span>
          </Link>
        );
      })}
    </nav>
  );
}
