import { Award, Download } from "lucide-react";

import { Card } from "@/components/ui/card";
import { buttonVariants } from "@/components/ui/button";
import { UnenrolButton } from "@/components/dashboard/UnenrolButton";
import type { Certificate } from "@/types/certificate";

export function CertificateCard({ certificate }: { certificate: Certificate }) {
  return (
    <Card className="flex-row items-center gap-4 p-4">
      <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-accent text-accent-foreground">
        <Award className="size-5" />
      </span>

      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{certificate.courseTitle}</p>
        <p className="text-xs text-muted-foreground">
          Issued {new Date(certificate.issuedAt).toLocaleDateString()}
        </p>
      </div>

      <div className="flex shrink-0 items-center gap-1">
        <UnenrolButton
          enrollmentId={certificate.enrollmentId}
          courseTitle={certificate.courseTitle}
          hasCertificate
        />
        <a
          href={certificate.certificateUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={buttonVariants({ variant: "outline", size: "sm" })}
        >
          <Download className="size-4" />
          Download
        </a>
      </div>
    </Card>
  );
}
