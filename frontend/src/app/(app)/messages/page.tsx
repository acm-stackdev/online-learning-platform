import { redirect } from "next/navigation";
import type { Metadata } from "next";

import { MessagesView } from "@/components/messaging/MessagesView";
import { getConversations } from "@/lib/api/messaging";
import { getCurrentUser } from "@/lib/api/me";

export const metadata: Metadata = {
  title: "Messages — LearnHub",
};

export default async function MessagesPage() {
  const [user, conversations] = await Promise.all([getCurrentUser(), getConversations()]);
  if (!user) redirect("/login");

  return (
    <div className="flex flex-1 flex-col">
      <MessagesView
        initialConversations={conversations}
        currentUserId={user.id}
        initialPresence={user.presenceStatus}
      />
    </div>
  );
}
