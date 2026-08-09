import { serverApiFetch } from "@/lib/api/server";
import type { Certificate } from "@/types/certificate";

export function getCertificate(enrollmentId: number) {
  return serverApiFetch<Certificate>(`/api/certificates/${enrollmentId}`);
}
