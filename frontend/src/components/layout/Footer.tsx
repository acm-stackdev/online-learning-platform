import Link from "next/link";

const footerLinks = [
  { label: "About", href: "/about" },
  { label: "Terms", href: "/terms" },
  { label: "Privacy", href: "/privacy" },
];

export function Footer() {
  return (
    <footer id="site-footer" className="border-t border-border">
      <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 px-4 py-8 text-sm text-muted-foreground sm:flex-row sm:px-6">
        <p>&copy; {new Date().getFullYear()} LearnHub</p>
        <nav className="flex items-center gap-6">
          {footerLinks.map(({ label, href }) => (
            <Link key={label} href={href} className="transition-colors hover:text-foreground">
              {label}
            </Link>
          ))}
        </nav>
      </div>
    </footer>
  );
}
