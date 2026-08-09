import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
import "@testing-library/jest-dom/vitest";

// RTL's automatic afterEach cleanup only self-registers when it detects a global
// `afterEach` (e.g. via Vitest's `globals: true`). This project uses explicit
// `import { ... } from "vitest"` instead, so cleanup has to be wired up by hand.
afterEach(() => {
  cleanup();
});
