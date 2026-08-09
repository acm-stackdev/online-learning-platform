import { describe, expect, it } from "vitest";

import { cn, formatDuration, initials } from "@/lib/utils";

describe("cn", () => {
  it("merges class names and resolves Tailwind conflicts", () => {
    expect(cn("px-2", "px-4")).toBe("px-4");
  });

  it("drops falsy values", () => {
    expect(cn("text-sm", false && "hidden", undefined, "font-medium")).toBe(
      "text-sm font-medium"
    );
  });
});

describe("initials", () => {
  it("uppercases the first two characters of a username", () => {
    expect(initials("daniel_okafor")).toBe("DA");
  });

  it("handles a single-character username", () => {
    expect(initials("x")).toBe("X");
  });
});

describe("formatDuration", () => {
  it("formats under an hour as minutes only", () => {
    expect(formatDuration(600)).toBe("10m");
  });

  it("formats an hour or more as hours and minutes", () => {
    expect(formatDuration(3900)).toBe("1h 5m");
  });

  it("rounds to the nearest minute", () => {
    expect(formatDuration(89)).toBe("1m");
  });
});
