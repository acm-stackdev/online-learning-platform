"use client";

import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

import type { Message, MessagesReadPayload, Presence } from "@/types/messaging";

export function useMessagingConnection({
  onReceiveMessage,
  onMessagesRead,
  onPresenceChanged,
}: {
  onReceiveMessage: (message: Message) => void;
  onMessagesRead: (payload: MessagesReadPayload) => void;
  onPresenceChanged: (presence: Presence) => void;
}) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/messaging`, {
        withCredentials: true,
        // The initial /negotiate handshake is a plain cookie-authenticated
        // POST request, so it's subject to the same CSRF guard as every
        // other mutating request in the app. This header doesn't carry over
        // to the WebSocket upgrade itself, but that's fine — the upgrade is
        // a GET request, which the CSRF guard doesn't check anyway.
        headers: { "X-Requested-With": "LearnHub" },
      })
      .withAutomaticReconnect()
      .build();

    connection.on("ReceiveMessage", onReceiveMessage);
    connection.on("MessagesRead", onMessagesRead);
    connection.on("PresenceChanged", onPresenceChanged);

    // React Strict Mode (on by default in Next.js dev) mounts effects twice —
    // mount, cleanup, mount again — to surface exactly this class of bug.
    // Calling connection.stop() while start() is still negotiating throws
    // "The connection was stopped during negotiation." Waiting for start()
    // to fully settle before stopping avoids it, at the cost of the first
    // (discarded) connection briefly completing its handshake before it's
    // torn down — harmless, and only happens in dev.
    const startPromise = connection
      .start()
      .then(() => setIsConnected(true))
      .catch(() => setIsConnected(false));

    connectionRef.current = connection;

    return () => {
      connectionRef.current = null;
      startPromise.finally(() => connection.stop());
    };
    // Callers pass stable (useCallback, functional-update-only) handlers, so
    // this connection is meant to be created exactly once per mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function sendMessage(enrollmentId: number, content: string) {
    await connectionRef.current?.invoke("SendMessage", { enrollmentId, content });
  }

  async function markRead(conversationId: number) {
    await connectionRef.current?.invoke("MarkRead", conversationId);
  }

  async function setPresence(status: "Online" | "Busy") {
    await connectionRef.current?.invoke("SetPresence", status);
  }

  return { isConnected, sendMessage, markRead, setPresence };
}
