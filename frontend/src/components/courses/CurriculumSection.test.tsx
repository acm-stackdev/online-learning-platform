import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { CurriculumSection } from "@/components/courses/CurriculumSection";
import { ContentType, type Section } from "@/types/course";

const section: Section = {
  id: 1,
  title: "Getting started",
  order: 1,
  lessons: [
    { id: 1, title: "Locked video", contentType: ContentType.Video, contentUrl: null, duration: 120, order: 1 },
    { id: 2, title: "Unlocked video", contentType: ContentType.Video, contentUrl: "https://example.com/v.mp4", duration: 180, order: 2 },
    { id: 3, title: "Unlocked PDF", contentType: ContentType.Pdf, contentUrl: "https://example.com/d.pdf", duration: 60, order: 3 },
  ],
};

describe("CurriculumSection", () => {
  it("stays closed by default and opens on click", async () => {
    const user = userEvent.setup();
    render(<CurriculumSection section={section} />);

    expect(screen.queryByText("Locked video")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /getting started/i }));

    expect(screen.getByText("Locked video")).toBeInTheDocument();
  });

  it("renders open by default when defaultOpen is true", () => {
    render(<CurriculumSection section={section} defaultOpen />);

    expect(screen.getByText("Locked video")).toBeInTheDocument();
  });

  it("shows a lock icon for a lesson with no contentUrl, and the right icon otherwise", () => {
    const { container } = render(<CurriculumSection section={section} defaultOpen />);

    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(3);

    expect(items[0].querySelector(".lucide-lock")).toBeInTheDocument();
    expect(items[1].querySelector(".lucide-circle-play")).toBeInTheDocument();
    expect(items[2].querySelector(".lucide-file-text")).toBeInTheDocument();
    // Sanity check the lock icon truly doesn't leak into an unlocked lesson.
    expect(container.querySelectorAll(".lucide-lock")).toHaveLength(1);
  });
});
