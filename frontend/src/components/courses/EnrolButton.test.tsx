import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { EnrolButton } from "@/components/courses/EnrolButton";
import { ApiError } from "@/lib/api/client";
import { enrol } from "@/lib/api/enrollments";

const { refreshMock } = vi.hoisted(() => ({ refreshMock: vi.fn() }));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

vi.mock("@/lib/api/enrollments", () => ({
  enrol: vi.fn(),
}));

const baseProps = {
  courseId: 42,
  isLoggedIn: true,
  isEnrolled: false,
  isOwner: false,
  isAdmin: false,
};

describe("EnrolButton", () => {
  it("shows Continue linking to the lesson player when enrolled", () => {
    render(<EnrolButton {...baseProps} isEnrolled />);

    const link = screen.getByRole("link", { name: "Continue" });
    expect(link).toHaveAttribute("href", "/courses/42/learn");
  });

  it("shows Edit course for the course owner", () => {
    render(<EnrolButton {...baseProps} isOwner />);

    const link = screen.getByRole("link", { name: "Edit course" });
    expect(link).toHaveAttribute("href", "/instructor/courses/42/edit");
  });

  it("shows Preview content for Admin", () => {
    render(<EnrolButton {...baseProps} isAdmin />);

    const link = screen.getByRole("link", { name: "Preview content" });
    expect(link).toHaveAttribute("href", "/courses/42/learn");
  });

  it("shows a login link when not logged in", () => {
    render(<EnrolButton {...baseProps} isLoggedIn={false} />);

    const link = screen.getByRole("link", { name: "Enrol now" });
    expect(link).toHaveAttribute("href", "/login");
  });

  it("enrols and refreshes on success", async () => {
    const user = userEvent.setup();
    vi.mocked(enrol).mockResolvedValueOnce({ id: 1 });
    render(<EnrolButton {...baseProps} />);

    await user.click(screen.getByRole("button", { name: "Enrol now" }));

    expect(enrol).toHaveBeenCalledWith(42);
    await waitFor(() => expect(refreshMock).toHaveBeenCalled());
  });

  it("shows the error message and re-enables the button when enrolling fails", async () => {
    const user = userEvent.setup();
    vi.mocked(enrol).mockRejectedValueOnce(new ApiError("You already have a pending application.", 409));
    render(<EnrolButton {...baseProps} />);

    await user.click(screen.getByRole("button", { name: "Enrol now" }));

    expect(await screen.findByText("You already have a pending application.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Enrol now" })).not.toBeDisabled();
  });
});
