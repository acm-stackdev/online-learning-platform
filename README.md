# LearnHub — Online Learning Platform

A cloud-native online learning platform built for a Westminster University final-year project. Students browse and enrol in free courses, instructors create and manage course content, and admins govern the platform.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core 8 (C#) |
| Database | PostgreSQL — [Neon](https://neon.tech) (serverless) |
| ORM | Entity Framework Core 8 + Npgsql |
| Auth | JWT in `httpOnly` cookies + Google OAuth 2.0 |
| Real-time | SignalR (messaging, presence) |
| AI | Google Gemini API (`gemini-2.5-flash`) — course tutor chatbot |
| Media | Cloudinary |
| PDF | PdfSharpCore (certificate generation from a template) |
| Email | MailKit/SMTP (verification, password reset) |
| Logging | Serilog (console sink) |
| Testing | xUnit + FluentAssertions + Moq, EF Core InMemory provider |
| CI/CD | GitHub Actions (`.github/workflows/backend-ci.yml`) |
| Frontend | Next.js 16 (App Router) + TypeScript |
| Frontend styling | Tailwind CSS v4 + shadcn/ui |
| Frontend forms | react-hook-form + zod |
| Frontend Google sign-in | `@react-oauth/google` |
| Frontend real-time | `@microsoft/signalr` |
| Frontend media upload | Direct browser → Cloudinary (unsigned preset), same convention as the backend |
| Frontend theming | `next-themes` — system-aware dark/light mode toggle |

---

## Roles & Permissions

| Role | Notes |
|---|---|
| **Student** | Browse catalogue, enrol in free courses, track lesson progress, download certificates, message their instructor, apply to become an Instructor. |
| **Instructor** | Everything a Student can do (a superset — can enrol in *other* instructors' courses), plus create/edit courses, upload lesson content, submit courses for review, and view an instructor dashboard. |
| **Admin** | Manage all users (search, role changes, suspend/reinstate), approve/reject courses and instructor applications, force-unpublish any course, view platform-wide stats. Cannot enrol in courses. Cannot self-register — the only way to create an Admin is an existing Admin promoting a user, or the dev-only seeded account (see [Getting Started](#getting-started)). |

Registration only ever accepts `Student` or `Instructor` — `Role` is validated server-side and admin self-registration is explicitly rejected.

---

## Features Implemented

- **Auth** — email/password registration with verification email, Google OAuth login, JWT access/refresh token rotation via `httpOnly` cookies, forgot/reset password, self-service profile update (username/avatar URL) and password change.
- **Courses** — catalogue with search/filter, CRUD (Instructor-owned), submit-for-review → Admin approve/reject workflow, unpublish (self) and force-unpublish (Admin, with a dedicated "Published courses" admin tab to find and act on any live course). Category is picked from a small set of presets (Development/Design/Business/Marketing) or a free-text "Custom..." option.
- **Sections & Lessons** — CRUD with reordering; lesson video/document content is uploaded **through the API** as `multipart/form-data` (up to 500MB) and pushed to Cloudinary server-side. This is different from avatars and course thumbnails, which are plain URL strings — the client uploads those directly to Cloudinary itself and only sends the resulting URL to the API.
- **Enrollment & progress tracking** — free enrolment, per-lesson completion tracking, course-completion detection.
- **Course preview for Admin/owner** — the course detail page and lesson player distinguish three viewer states returned by the API (`isEnrolled`/`isOwner`, plus the client's own `isAdmin` check): an enrolled Student sees the normal "Continue" experience with progress tracking, the owning Instructor sees an "Edit course" shortcut instead, and an Admin gets a clearly-labelled read-only preview of the actual video/PDF content (for moderation) with no enrolment, progress tracking, or certificate implied.
- **Certificates** — PDF certificate auto-issued on course completion (PdfSharpCore + a template asset).
- **Messaging** — real-time, SignalR-based, scoped per enrolment; live presence status (Online/Busy/Offline).
- **Instructor application workflow** — a Student can apply to become an Instructor; an Admin approves or rejects. If an Admin later reverts a promoted user back to Student via the Users tab, the become-instructor page correctly shows "instructor access removed" rather than a stale "approved" message, and lets them re-apply.
- **Admin panel** — user management, role changes, suspend/reinstate, course review (approve/reject) plus a separate published-courses view for force-unpublishing a live course, platform stats.
- **Dashboards** — a consolidated "my stuff" endpoint for Students, and a separate one for Instructors (courses they own).
- **AI course tutor chatbot** — Gemini-backed, per-course, stateless (the client resends recent conversation turns each request; nothing is persisted server-side). Scoped to the course's owner, an Admin, or an enrolled student — the same access rule that already gates lesson content.
- **CSRF protection** — CORS locked to a single configurable frontend origin, plus a custom-header guard middleware on cookie-authenticated mutating requests.
- **Theming** — system-aware dark/light mode toggle in both navbars (`next-themes`).

**Explicitly out of scope:** payment processing (all courses are free) and Assignments/Grading (cut from scope during development to keep the project focused).

---

## Project Structure

```
backend/
├── LearnHub/                      # ASP.NET Core Web API
│   ├── Controllers/                # HTTP endpoints, delegate to Services
│   ├── Models/
│   │   ├── Entities/                # EF Core entities
│   │   └── DTOs/                    # Request/response shapes, grouped by feature
│   ├── Services/                   # Business logic, one service per domain
│   ├── Data/                       # AppDbContext + EF Core Migrations
│   ├── Middleware/                 # CSRF guard middleware
│   ├── Helpers/                    # JWT helper, claims extensions, PDF generator
│   ├── Hubs/                       # SignalR MessagingHub
│   ├── Assets/                     # Certificate PDF template
│   └── Program.cs                  # DI registration, middleware pipeline
└── LearnHub.Tests/                 # xUnit test project

frontend/
├── src/
│   ├── app/                        # Next.js App Router routes, split into route groups by chrome/auth level
│   │   ├── (public)/                # No auth required — landing, course catalogue, course detail
│   │   ├── (auth)/                  # Login/register/forgot-password/reset-password/verify-email — no navbar
│   │   ├── (app)/                   # Authenticated shell (AppNavbar + session auto-refresh) — dashboard,
│   │   │                            #   my-courses, account, messages, become-instructor, instructor/*, admin/*
│   │   ├── (learn)/                 # The lesson player — its own minimal chrome, not the full app navbar
│   │   ├── @modal/                  # Parallel route: login/register render as a shadcn Dialog when linked to
│   │   │                            #   from anywhere in the app, and as full pages on direct visit/refresh
│   │   │                            #   (Next.js "intercepting routes")
│   │   └── proxy.ts                 # Auth guard for protected routes — checks the session, silently
│   │                                #   refreshes an expired-but-still-valid one before redirecting to /login
│   ├── components/
│   │   ├── ui/                     # shadcn/ui primitives
│   │   ├── theme-provider.tsx       # next-themes wrapper (wraps the root layout)
│   │   ├── theme-toggle.tsx         # Dark/light mode toggle button, used in both navbars
│   │   ├── layout/                 # PublicNavbar, AppNavbar, UserMenu, Footer
│   │   ├── landing/                # Landing page sections
│   │   ├── auth/                   # Auth forms, Google sign-in, session-expiry refresher
│   │   ├── courses/                # Catalogue search/pagination, curriculum display
│   │   ├── learn/                  # Lesson player, curriculum rail
│   │   ├── dashboard/               # Stat tiles, continue-learning/certificate cards
│   │   ├── messaging/               # Conversation list, chat thread (SignalR-backed)
│   │   ├── instructor/              # Course builder (details + curriculum editor), lesson upload dialog
│   │   ├── admin/                   # Review-queue rows, user management row
│   │   └── shared/                  # Cross-feature pieces (e.g. the Cloudinary ImageUpload widget)
│   ├── lib/
│   │   ├── api/                    # Typed fetch wrappers per backend feature area (server- vs client-only
│   │   │                           #   split deliberately maintained — see note below)
│   │   ├── signalr/                 # SignalR connection hook for messaging
│   │   └── cloudinary.ts            # Direct-to-Cloudinary upload helper (unsigned preset)
│   └── types/                      # TypeScript types mirroring backend DTOs
└── ...

.github/workflows/backend-ci.yml    # CI: restore, build, test on every push/PR to main
```

**Frontend status:** feature-complete across all four tiers — Public (landing, catalogue, course detail with a distinct preview experience for Admin/course-owner), Student (dashboard, my courses, lesson player with progress tracking, certificates, account settings, messaging, instructor application), Instructor (dashboard, course builder with curriculum + file upload, roster), and Admin (overview stats, course review queue, published-courses management, instructor application review, user management).

**`lib/api/` server/client split**, worth knowing before adding to it: functions using `serverApiFetch` (forwards cookies via `next/headers`, Server-Component-only) and functions using `apiFetch` (plain `fetch`, client-safe) are kept in **separate files** even when they cover the same feature area (e.g. `messaging.ts` vs a client-side messages helper, `my-enrollments.ts` vs `enrollments.ts`). Mixing them in one file risks a hard Next.js build error if that file is ever imported from a `"use client"` component.

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- A [Neon](https://neon.tech) Postgres database (connection string)
- A Cloudinary account (Cloud Name / API Key / Secret)
- A Google OAuth Client ID (for Google sign-in)
- SMTP credentials (e.g. a Gmail account with an App Password) for verification/reset emails
- A Google Gemini API key (for the AI course tutor)

### 1. Configure environment

```bash
cp backend/LearnHub/.env.example backend/LearnHub/.env
```

Fill in the real values — see [Environment Variables](#environment-variables) below.

### 2. Run

```bash
cd backend/LearnHub
dotnet restore
dotnet ef database update    # applies EF Core migrations to your Neon database
dotnet run
```

Swagger UI is available at `/swagger` in Development.

### 3. Dev-only admin account

On first run in Development, if no `Admin` user exists yet, one is seeded automatically:

```
Email:    admin@learnhub.local
Password: Admin123!
```

(the credentials are also printed to the console log). Override with `Admin__Email`/`Admin__Password` in `.env` if you want different ones. This seeding never runs outside `Development`.

### Testing with Swagger/Postman

Auth uses `httpOnly` cookies, not a bearer token in the response body — logging in via Swagger or Postman automatically stores the cookie for that tool, and subsequent requests reuse it with no extra setup. Because of the CSRF guard, any mutating request (`POST`/`PUT`/`PATCH`/`DELETE`) sent while that cookie is present must also include the header:

```
X-Requested-With: LearnHub
```

(Postman: set this once as a collection-level default header. Swagger: add it per-request via the header field.)

### 4. Run the frontend

```bash
cp frontend/.env.example frontend/.env.local
cd frontend
npm install
npm run dev
```

Runs at `http://localhost:3000`. Needs the backend running too (`NEXT_PUBLIC_API_URL`, default `http://localhost:5073`). `NEXT_PUBLIC_GOOGLE_CLIENT_ID` should be the same value as the backend's `GOOGLE__CLIENTID` — that OAuth client's authorized JavaScript origins need `http://localhost:3000` added in Google Cloud Console for Google sign-in to work locally.

Avatar and course-thumbnail uploads (`/account`, the course builder) go straight from the browser to Cloudinary using an **unsigned upload preset** — the API secret never touches the frontend. Set `NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME` (same as the backend's `CLOUDINARY__CLOUDNAME`) and `NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET` to a preset you've created for your Cloudinary account (Console → Settings → Upload → Upload presets → Add upload preset, mode **Unsigned**). Without this, those two upload buttons will fail — everything else in the app works fine regardless.

### 5. Dev-only demo course data

Alongside the admin seed, if no courses exist yet, two published demo courses (with real playable sample video/PDF lessons) are seeded automatically, along with a demo Instructor and Student account so the whole enrol → learn → certificate loop can be tried immediately without registering or building a course by hand:

```
Instructor: daniel@learnhub.local / Instructor123!
Student:    priya@learnhub.local  / Student123!
```

Same `Development`-only, seed-if-empty pattern as the admin account (`Program.cs`).

---

## Environment Variables

Loaded via [`DotNetEnv`](https://www.nuget.org/packages/DotNetEnv) (`DotNetEnv.Env.Load()` in `Program.cs`). Key casing doesn't matter and `__` maps to `:` in ASP.NET Core's config binder.

```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=your-neon-host.neon.tech;Port=5432;Database=learnhub;Username=your_username;Password=your_password;SSL Mode=Require;Trust Server Certificate=true

CLOUDINARY__CLOUDNAME=your_cloud_name
CLOUDINARY__APIKEY=your_api_key
CLOUDINARY__APISECRET=your_api_secret

JWT__SECRET=generate_a_random_32+_char_secret
JWT__ISSUER=learnhub-api
JWT__AUDIENCE=learnhub-client
JWT__EXPIRYMINUTES=60

GOOGLE__CLIENTID=your_google_oauth_client_id

GEMINI__APIKEY=your_gemini_api_key

SMTP__HOST=smtp.gmail.com
SMTP__PORT=587
SMTP__USERNAME=your_gmail_address@gmail.com
SMTP__PASSWORD=your_gmail_app_password
SMTP__FROMEMAIL=your_gmail_address@gmail.com
SMTP__FROMNAME=LearnHub

FRONTEND__BASEURL=http://localhost:3000
```

**Neon connection string format:** Npgsql needs the classic `Key=Value;` ADO.NET format, not Neon's default `postgresql://user:pass@host/db?sslmode=require&channel_binding=require` URI — Npgsql doesn't parse that URI scheme or recognize `channel_binding`. Convert it to:
```
Host=<neon-host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

**`FRONTEND__BASEURL`** is also the CORS-allowed origin — set it to wherever the frontend actually runs (`http://localhost:3000` for local dev; the real deployed frontend URL in production).

---

## API Overview

All routes are prefixed `/api`. "Auth" reflects the effective requirement per endpoint (action-level `[Authorize]` overrides the controller's class-level default where noted).

### Auth (`/api/auth`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `register` | None | Register (Student or Instructor only) |
| POST | `login` | None | Log in, sets auth cookies |
| POST | `google` | None | Google OAuth login/registration |
| POST | `verify-email` | None | Verify email via token |
| POST | `forgot-password` | None | Request a password reset email |
| POST | `reset-password` | None | Reset password via token |
| POST | `refresh` | None (refresh cookie) | Rotate access/refresh tokens |
| POST | `logout` | None (refresh cookie) | Revoke refresh token, clear cookies |
| GET | `me` | Authenticated | Current user's profile |
| PUT | `me` | Authenticated | Update username/avatar URL |
| POST | `change-password` | Authenticated | Change password (while logged in) |

### Courses (`/api/courses`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `` | Public | Catalogue (search/category/pagination) |
| GET | `pending` | Admin | Courses awaiting approval |
| GET | `{id}` | Public (richer detail if enrolled/owner/admin) | Course detail — includes `isEnrolled`/`isOwner` so the client can tell an enrolled Student, the owning Instructor, and an Admin apart |
| POST | `` | Instructor | Create course |
| PUT | `{id}` | Instructor | Update course |
| DELETE | `{id}` | Instructor | Delete course |
| POST | `{id}/submit-for-review` | Instructor | Submit for Admin review |
| PUT | `{id}/unpublish` | Instructor | Unpublish own course |
| PUT | `{id}/force-unpublish` | Admin | Force-unpublish any course |
| POST | `{id}/approve` | Admin | Approve a pending course |
| POST | `{id}/reject` | Admin | Reject a pending course |
| POST | `{id}/chat` | Authenticated (owner/enrolled/admin) | Ask the AI course tutor |

### Sections (`/api/sections`) — Instructor-only
| Method | Route | Description |
|---|---|---|
| POST | `` | Create section |
| PUT | `reorder` | Reorder sections |
| PUT | `{id}` | Update section |
| DELETE | `{id}` | Delete section |

### Lessons (`/api/lessons`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Instructor | Create lesson (multipart upload, ≤500MB) |
| PUT | `reorder` | Instructor | Reorder lessons |
| PUT | `{id}` | Instructor | Update lesson (multipart upload) |
| DELETE | `{id}` | Instructor | Delete lesson |
| PUT | `{id}/progress` | Student, Instructor | Mark/update a student's lesson progress |

### Enrollments (`/api/enrollments`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Student, Instructor | Enrol in a course |
| DELETE | `{id}` | Owner, owning instructor, Admin | Remove an enrollment |
| GET | `` | Student, Instructor | Current user's enrolments |
| GET | `{id}/progress` | Student, Instructor | Progress for one enrolment |
| GET | `course/{courseId}` | Instructor, Admin | Enrolment roster for a course |

### Certificates (`/api/certificates`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `{enrollmentId}` | Owner or Admin | Download completion certificate PDF |

### Messaging (`/api/messaging`)
| Method | Route | Description |
|---|---|---|
| GET | `conversations` | Current user's conversations |
| GET | `conversations/{conversationId}/messages` | Paginated message history |

Sending messages and read receipts happen over the SignalR hub, not REST — connect to **`/hubs/messaging`**.

### Instructor Applications (`/api/instructor-applications`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Student | Submit an application |
| GET | `mine` | Student | View own application |
| GET | `` | Admin | List all applications (filterable by status) |
| POST | `{id}/approve` | Admin | Approve → promotes user to Instructor |
| POST | `{id}/reject` | Admin | Reject |

### Admin (`/api/admin`) — Admin-only
| Method | Route | Description |
|---|---|---|
| GET | `users` | List/search users |
| PUT | `users/{id}/role` | Change a user's role |
| POST | `users/{id}/suspend` | Suspend a user |
| POST | `users/{id}/reinstate` | Reinstate a user |
| GET | `stats` | Platform-wide statistics |

### Dashboard (`/api/dashboard`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `student` | Authenticated (any role) | Student's enrolments, applications, certificates |
| GET | `instructor` | Instructor | Instructor's own courses summary |

---

## Testing & CI/CD

### Why unit testing was added

Before this work, the only way to check that login, registration, or logout behaved correctly was to run the whole application and test it by hand — slow, and easy to forget to re-check old features after a new change. Unit tests solve this: small automated checks that run in seconds and verify a specific piece of code still behaves as expected, every time the project is built.

### What's tested

Testing started with the authentication feature (`AuthController`/`AuthService`) since every other feature depends on a user being correctly identified, then expanded to cover every feature as it was built — courses, lessons, enrolment, progress, certificates, messaging, instructor applications, the admin panel, dashboards, the AI chatbot, and the CSRF guard middleware.

Two layers are tested separately per feature:
- **Services** — the business logic (e.g. "logging in with the wrong password is rejected", "an expired verification link is rejected").
- **Controllers** — the HTTP layer on top (correct status codes, correctly shaped responses, cookies set with the right security options).

Tooling:
- **xUnit** — runs the tests and reports pass/fail.
- **Moq** — fakes dependencies that shouldn't really run during a test (e.g. a fake email sender, a fake Gemini client, so tests never hit a real API).
- **FluentAssertions** — makes assertions easier to read.
- **EF Core's InMemory provider** — a fake database, so tests run instantly with no external dependency.

As of the latest count, 219 tests cover expected successful behavior and expected failure behavior alike (wrong password, duplicate email, expired tokens, unauthorized access, and so on).

### A real bug found during testing

While writing a test for the logout endpoint, the tests revealed an actual bug, not just a hypothetical one. On login, the server stores a "refresh token" cookie scoped to the path `/api/auth`. On logout, the code meant to delete that cookie was accidentally targeting a different path, `/api/auth/refresh`. Browsers only delete a cookie when the path matches exactly, so logging out did not actually remove the cookie from the browser.

This was confirmed independently by simulating a real browser's cookie storage (via .NET's `CookieContainer`, which follows the same cookie rules browsers use) and checking whether the cookie survived logout — it did, proving the bug. The fix was a one-line change to make both paths match. A good example of unit testing catching a real, user-facing issue that wasn't obvious from reading the code casually.

### CI/CD with GitHub Actions

`.github/workflows/backend-ci.yml` runs on every push and pull request to `main` that touches the backend:
1. Installs the .NET SDK on a temporary GitHub-hosted machine.
2. Builds the solution (`LearnHub.sln`), catching compile errors.
3. Runs the full test suite, catching behavioral errors.

If any step fails, GitHub marks the change with a red cross and shows exactly which test failed, before the code is ever merged.

### Running tests locally

```bash
dotnet test LearnHub.sln
```

---

## Deployment

Not yet deployed. `GET /health` (real DB-connectivity check against Neon via `AspNetCore.HealthChecks.NpgSql`, `app.MapHealthChecks("/health")` in `Program.cs`) is already implemented and manually verified against the live database — it just isn't wired into any hosting yet. Still queued: a `Dockerfile`, a Render service, and an UptimeRobot monitor pointed at `/health`.

---

## License

Academic project — Westminster University Final Project 2025/2026.
