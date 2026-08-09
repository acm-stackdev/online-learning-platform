"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { Sparkles, X } from "lucide-react";
import Markdown from "react-markdown";
import type { Components } from "react-markdown";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { askCourseTutor } from "@/lib/api/chatbot";
import { ApiError } from "@/lib/api/client";
import { cn } from "@/lib/utils";
import type { ChatMessage } from "@/types/chatbot";

const HISTORY_LIMIT = 20;
const BUTTON_BASE_BOTTOM = 16; // matches bottom-4
const PANEL_BASE_BOTTOM = 80; // matches bottom-20

// Keeps the tutor's markdown replies readable at chat-bubble scale instead of the
// browser's default article-sized headings/lists/spacing.
const markdownComponents: Components = {
  p: ({ children }) => <p className="mb-2 last:mb-0">{children}</p>,
  ul: ({ children }) => <ul className="mb-2 list-disc pl-4 last:mb-0">{children}</ul>,
  ol: ({ children }) => <ol className="mb-2 list-decimal pl-4 last:mb-0">{children}</ol>,
  li: ({ children }) => <li className="mb-0.5">{children}</li>,
  h1: ({ children }) => <p className="mt-2 mb-1 font-semibold first:mt-0">{children}</p>,
  h2: ({ children }) => <p className="mt-2 mb-1 font-semibold first:mt-0">{children}</p>,
  h3: ({ children }) => <p className="mt-2 mb-1 font-semibold first:mt-0">{children}</p>,
  a: ({ children, href }) => (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className="underline underline-offset-2"
    >
      {children}
    </a>
  ),
  code: ({ children }) => (
    <code className="rounded bg-foreground/10 px-1 py-0.5 text-xs">{children}</code>
  ),
  pre: ({ children }) => (
    <pre className="mb-2 overflow-x-auto rounded bg-foreground/10 p-2 text-xs last:mb-0">
      {children}
    </pre>
  ),
};

export function CourseTutorWidget({
  courseId,
  courseTitle,
  isLoggedIn,
}: {
  courseId: number;
  courseTitle: string;
  isLoggedIn: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [footerOffset, setFooterOffset] = useState(0);
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight });
  }, [messages]);

  // Shift the widget up when the page footer scrolls into view so it never overlaps it.
  // Not every page that mounts this widget has a footer (e.g. the lesson player) —
  // the observer just never fires in that case, and footerOffset stays 0.
  useEffect(() => {
    const footer = document.getElementById("site-footer");
    if (!footer) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        setFooterOffset(entry.isIntersecting ? footer.getBoundingClientRect().height : 0);
      },
      { threshold: 0 }
    );
    observer.observe(footer);
    return () => observer.disconnect();
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const message = draft.trim();
    if (!message || sending) return;

    const history = messages.slice(-HISTORY_LIMIT);
    setMessages((prev) => [...prev, { role: "user", content: message }]);
    setDraft("");
    setSending(true);
    setError(null);

    try {
      const result = await askCourseTutor(courseId, { message, history });
      setMessages((prev) => [...prev, { role: "model", content: result.reply }]);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setSending(false);
    }
  }

  return (
    <>
      {open ? (
        <div
          className="fixed right-4 z-50 flex h-96 w-80 flex-col rounded-lg border border-border bg-popover text-popover-foreground shadow-lg sm:w-96"
          style={{ bottom: PANEL_BASE_BOTTOM + footerOffset }}
        >
          <div className="flex items-center justify-between border-b border-border px-4 py-3">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">Ask about this course</p>
              <p className="truncate text-xs text-muted-foreground">{courseTitle}</p>
            </div>
            <Button variant="ghost" size="icon-sm" onClick={() => setOpen(false)}>
              <X />
            </Button>
          </div>

          {!isLoggedIn ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-3 p-4 text-center">
              <p className="text-sm text-muted-foreground">
                Log in to ask questions about this course.
              </p>
              <Link href="/login" className="text-sm font-medium text-primary hover:underline">
                Log in
              </Link>
            </div>
          ) : (
            <>
              <div ref={listRef} className="flex-1 space-y-3 overflow-y-auto p-4">
                {messages.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    Ask anything about this course — what it covers, how it&apos;s taught, or
                    whether it&apos;s a good fit for you.
                  </p>
                ) : (
                  messages.map((m, i) => (
                    <div
                      key={i}
                      className={cn("flex", m.role === "user" ? "justify-end" : "justify-start")}
                    >
                      <div
                        className={cn(
                          "max-w-xs rounded-lg px-3 py-2 text-sm sm:max-w-sm",
                          m.role === "user"
                            ? "bg-primary text-primary-foreground"
                            : "bg-muted text-foreground"
                        )}
                      >
                        {m.role === "model" ? (
                          <Markdown components={markdownComponents}>{m.content}</Markdown>
                        ) : (
                          m.content
                        )}
                      </div>
                    </div>
                  ))
                )}
                {sending ? (
                  <p className="text-xs text-muted-foreground">Thinking...</p>
                ) : null}
              </div>

              {error ? <p className="px-4 pb-2 text-xs text-destructive">{error}</p> : null}

              <form onSubmit={handleSubmit} className="flex gap-2 border-t border-border p-3">
                <Input
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  placeholder="Ask a question..."
                  maxLength={2000}
                  disabled={sending}
                  className="flex-1"
                />
                <Button type="submit" disabled={!draft.trim() || sending}>
                  Send
                </Button>
              </form>
            </>
          )}
        </div>
      ) : null}

      <Button
        size="icon-lg"
        className="fixed right-4 z-50 rounded-full shadow-lg"
        style={{ bottom: BUTTON_BASE_BOTTOM + footerOffset }}
        onClick={() => setOpen((o) => !o)}
        aria-label="Ask about this course"
      >
        {open ? <X /> : <Sparkles />}
      </Button>
    </>
  );
}
