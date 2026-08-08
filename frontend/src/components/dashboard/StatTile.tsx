import { Card } from "@/components/ui/card";

export function StatTile({ label, value }: { label: string; value: number }) {
  return (
    <Card className="items-center gap-1 py-4 text-center">
      <p className="text-xs uppercase tracking-wide text-muted-foreground">
        {label}
      </p>
      <p className="text-2xl font-semibold">{value}</p>
    </Card>
  );
}
