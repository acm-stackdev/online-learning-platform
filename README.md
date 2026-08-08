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
- **Courses** — catalogue with search/filter, CRUD (Instructor-owned), submit-for-review → Admin approve/reject workflow, unpublish (self) and force-unpublish (Admin).
- **Sections & Lessons** — CRUD with reordering; lesson video/document content is uploaded **through the API** as `multipart/form-data` (up to 500MB) and pushed to Cloudinary server-side. This is different from avatars and course thumbnails, which are plain URL strings — the client uploads those directly to Cloudinary itself and only sends the resulting URL to the API.
- **Enrollment & progress tracking** — free enrolment, per-lesson completion tracking, course-completion detection.
- **Certificates** — PDF certificate auto-issued on course completion (PdfSharpCore + a template asset).
- **Messaging** — real-time, SignalR-based, scoped per enrolment; live presence status (Online/Busy/Offline).
- **Instructor application workflow** — a Student can apply to become an Instructor; an Admin approves or rejects.
- **Admin panel** — user management, role changes, suspend/reinstate, force-unpublish, platform stats.
- **Dashboards** — a consolidated "my stuff" endpoint for Students, and a separate one for Instructors (courses they own).
- **AI course tutor chatbot** — Gemini-backed, per-course, stateless (the client resends recent conversation turns each request; nothing is persisted server-side). Scoped to the course's owner, an Admin, or an enrolled student — the same access rule that already gates lesson content.
- **CSRF protection** — CORS locked to a single configurable frontend origin, plus a custom-header guard middleware on cookie-authenticated mutating requests.

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
│   ├── app/                        # Next.js App Router routes
│   │   ├── (auth)/                 # Route group: login, register, verify-email — no shared navbar/footer
│   │   └── page.tsx                # Landing page
│   ├── components/
│   │   ├── ui/                     # shadcn/ui primitives
│   │   ├── layout/                 # Navbar, Footer
│   │   ├── landing/                # Landing page sections
│   │   └── auth/                   # Auth forms and shared auth UI
│   ├── lib/api/                    # Typed fetch wrappers per backend feature area
│   └── types/                      # TypeScript types mirroring backend DTOs
└── ...

.github/workflows/backend-ci.yml    # CI: restore, build, test on every push/PR to main
```

**Frontend status:** built so far — landing page (`/`), login/register/verify-email (`(auth)` route group). Remaining pages follow the same build order as the rest of the app: Public → Student → Instructor → Admin.

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
| GET | `{id}` | Public (richer detail if enrolled/owner/admin) | Course detail |
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

Not yet deployed. Queued work: a `/health` endpoint (DB-connectivity check against Neon), a `Dockerfile`, a Render service, and an UptimeRobot monitor. None of this exists in the repo yet.

---

## License

Academic project — Westminster University Final Project 2025/2026.
