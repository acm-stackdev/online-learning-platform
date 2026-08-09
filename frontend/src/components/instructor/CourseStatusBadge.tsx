import { Badge } from "@/components/ui/badge";
import { CourseStatus } from "@/types/course";

const config: Record<CourseStatus, { label: string; variant: "default" | "secondary" | "outline" | "destructive" }> = {
  [CourseStatus.Draft]: { label: "Draft", variant: "outline" },
  [CourseStatus.PendingApproval]: { label: "Pending review", variant: "secondary" },
  [CourseStatus.Published]: { label: "Published", variant: "default" },
  [CourseStatus.Rejected]: { label: "Rejected", variant: "destructive" },
};

export function CourseStatusBadge({ status }: { status: CourseStatus }) {
  const { label, variant } = config[status];
  return <Badge variant={variant}>{label}</Badge>;
}
