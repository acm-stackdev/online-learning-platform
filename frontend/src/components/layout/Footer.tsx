const footerLinks = ["About", "Terms", "Privacy", "Contact"];

export function Footer() {
  return (
    <footer id="site-footer" className="border-t border-border">
      <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-4 px-4 py-8 text-sm text-muted-foreground sm:flex-row sm:px-6">
        <p>&copy; {new Date().getFullYear()} LearnHub</p>
        <nav className="flex items-center gap-6">
          {footerLinks.map((label) => (
            <a key={label} href="#" className="transition-colors hover:text-foreground">
              {label}
            </a>
          ))}
        </nav>
      </div>
    </footer>
  );
}
