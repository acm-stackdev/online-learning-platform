"use client";

import { cn } from "@/lib/utils";
import { buttonVariants } from "@/components/ui/button";
import { Role } from "@/types/auth";

const options: { role: Role; label: string; description: string }[] = [
  { role: Role.Student, label: "Learn", description: "Enrol in courses" },
  { role: Role.Instructor, label: "Teach", description: "Apply as instructor" },
];

export function RoleToggle({
  value,
  onChange,
}: {
  value: Role | undefined;
  onChange: (role: Role) => void;
}) {
  return (
    <div className="grid grid-cols-2 gap-2">
      {options.map((option) => (
        <button
          key={option.role}
          type="button"
          onClick={() => onChange(option.role)}
          className={cn(
            buttonVariants({
              variant: value === option.role ? "default" : "outline",
            }),
            "h-auto flex-col items-start gap-0.5 px-3 py-2"
          )}
        >
          <span className="text-sm font-medium">{option.label}</span>
          <span
            className={cn(
              "text-xs font-normal",
              value === option.role
                ? "text-primary-foreground/80"
                : "text-muted-foreground"
            )}
          >
            {option.description}
          </span>
        </button>
      ))}
    </div>
  );
}
