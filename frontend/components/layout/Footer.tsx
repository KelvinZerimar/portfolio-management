export function Footer() {
  return (
    <footer className="shrink-0 bg-paper px-4 py-3 text-center text-xs text-ink-muted shadow-[0_-1px_0_rgba(16,24,40,0.06)] md:px-6">
      © {new Date().getFullYear()} Portfolio Ledger
    </footer>
  );
}
