# Testing & CI/CD — What We Did

This document records the unit testing and CI/CD work done for the backend, for use in the project report.

## Why unit testing was added

Before this work, the only way to check that login, registration, or logout behaved correctly was to run the whole application and test it by hand. This is slow, and it is easy to forget to re-check old features after making a new change. Unit tests solve this: they are small automated checks that run in seconds and verify that a specific piece of code still behaves as expected, every time the project is built.

## What was tested

The authentication feature (`AuthController` and `AuthService`) was chosen first, since it is the most critical part of the backend — every other feature depends on a user being correctly identified.

Two layers were tested separately:

- **`AuthService`** — the business logic: registering a user, logging in, refreshing tokens, logging out, and verifying an email address. Tests here check things like "logging in with the wrong password is rejected" or "an expired verification link is rejected."
- **`AuthController`** — the HTTP layer sitting on top of it: does the API return the correct status code, are login cookies set with the correct security settings, and is the response shaped correctly.

Testing was done using three tools:
- **xUnit** — runs the tests and reports pass/fail.
- **Moq** — creates fake versions of dependencies that should not really run during a test (for example, a fake email sender, so tests don't actually send emails).
- **FluentAssertions** — makes the checks inside each test easier to read.

A fake, in-memory database (provided by Entity Framework Core) was used instead of the real database, so tests run instantly and do not depend on any external service.

In total, 35 tests were written, covering both the expected successful behavior and the expected failure behavior (wrong password, duplicate email, expired tokens, and so on).

## A real bug found during testing

While writing a test for the logout endpoint, the tests revealed an actual bug in the code, not just a hypothetical one. When a user logs in, the server stores a "refresh token" cookie scoped to the path `/api/auth`. When the user logs out, the code that was supposed to delete that cookie was accidentally targeting a different path, `/api/auth/refresh`. Browsers only delete a cookie when the path matches exactly, so logging out did not actually remove the cookie from the browser.

This was confirmed independently by simulating a real browser's cookie storage (using .NET's built-in `CookieContainer`, which follows the same cookie rules browsers use) and checking whether the cookie survived logout — it did, which proved the bug. The fix was a one-line change to make both paths match. This is a good example of unit testing catching a real, user-facing issue that was not obvious from reading the code casually.

## CI/CD with GitHub Actions

To make sure these tests are actually run — not just written once and forgotten — a GitHub Actions workflow (`.github/workflows/backend-ci.yml`) was added. It automatically runs on every push and pull request to the main branch that touches the backend code. The pipeline does three things:

1. Installs the .NET SDK on a temporary cloud machine provided by GitHub.
2. Builds the project, which catches compile errors.
3. Runs all 35 tests, which catches behavior errors.

If any step fails, GitHub marks the change with a red cross and shows exactly which test failed, before the code is ever merged. This removes the need to remember to test manually before every change.

## How to run the tests locally

```bash
dotnet test LearnHub.sln
```
