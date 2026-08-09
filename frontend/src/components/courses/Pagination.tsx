import Link from "next/link";

import { cn } from "@/lib/utils";
import { buttonVariants } from "@/components/ui/button";

function buildHref(basePath: string, page: number, search?: string) {
  const params = new URLSearchParams({ page: String(page) });
  if (search) params.set("search", search);
  return `${basePath}?${params.toString()}`;
}

export function Pagination({
  basePath = "/courses",
  page,
  pageSize,
  totalCount,
  search,
}: {
  basePath?: string;
  page: number;
  pageSize: number;
  totalCount: number;
  search?: string;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalPages <= 1) return null;

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);

  return (
    <nav className="flex items-center justify-center gap-1">
      <Link
        href={buildHref(basePath, Math.max(1, page - 1), search)}
        aria-disabled={page <= 1}
        className={cn(
          buttonVariants({ variant: "outline", size: "icon-sm" }),
          page <= 1 && "pointer-events-none opacity-50"
        )}
      >
        &lsaquo;
      </Link>

      {pages.map((p) => (
        <Link
          key={p}
          href={buildHref(basePath, p, search)}
          className={buttonVariants({
            variant: p === page ? "default" : "outline",
            size: "icon-sm",
          })}
        >
          {p}
        </Link>
      ))}

      <Link
        href={buildHref(basePath, Math.min(totalPages, page + 1), search)}
        aria-disabled={page >= totalPages}
        className={cn(
          buttonVariants({ variant: "outline", size: "icon-sm" }),
          page >= totalPages && "pointer-events-none opacity-50"
        )}
      >
        &rsaquo;
      </Link>
    </nav>
  );
}
