import { serverApiFetch } from "@/lib/api/server";
import type { Conversation } from "@/types/messaging";

export async function getConversations(): Promise<Conversation[]> {
  const result = await serverApiFetch<Conversation[]>("/api/messaging/conversations");
  return result ?? [];
}
