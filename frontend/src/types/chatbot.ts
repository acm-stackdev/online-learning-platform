// "model" matches Gemini's own role vocabulary (not "assistant") — the backend
// passes this straight through to the Gemini API.
export interface ChatMessage {
  role: "user" | "model";
  content: string;
}

export interface ChatResponse {
  reply: string;
}
