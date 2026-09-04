# LearnHub — Cloud-Native Online Learning Platform

> MSc Software Engineering Final Project — University of Westminster 2025/26
> 
> **Student:** Aung Chan Myae | **Supervisor:** Dr. David Huang

LearnHub is a full-stack, cloud-native Learning Management System providing role-based learning experiences for Students, Instructors, and Administrators. Students browse and enrol in free courses, track lesson progress, chat with an AI tutor, message their instructor in real time, and download auto-generated certificates. Instructors create and manage course content. Admins govern the platform.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Roles and Permissions](#roles-and-permissions)
- [Features](#features)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
  - [Demo Accounts](#demo-accounts)
- [Environment Variables](#environment-variables)
- [Running Tests](#running-tests)
- [API Overview](#api-overview)
- [Deployment](#deployment)

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend API** | ASP.NET Core .NET 8 (C#) |
| **Database** | PostgreSQL — [Neon](https://neon.tech) serverless |
| **ORM** | Entity Framework Core 8 + Npgsql |
| **Authentication** | JWT in `httpOnly` cookies + Google OAuth 2.0 |
| **Real-Time** | SignalR (messaging and presence tracking) |
| **AI Chatbot** | Google Gemini API (`gemini-flash-latest`) |
| **Media Storage** | Cloudinary CDN |
| **PDF Generation** | PdfSharpCore (certificate generation from a template asset) |
| **Email** | MailKit / SMTP (verification and password reset) |
| **Logging** | Serilog (console sink) |
| **Backend Testing** | xUnit + FluentAssertions + Moq + EF Core InMemory |
| **CI/CD** | GitHub Actions (path-filtered workflows) |
| **Frontend** | Next.js 16 (App Router) + TypeScript |
| **Frontend Styling** | Tailwind CSS + shadcn/ui |
| **Frontend Forms** | react-hook-form + zod |
| **Frontend Real-Time** | `@microsoft/signalr` |
| **Frontend Testing** | Vitest + React Testing Library |

---

## Roles and Permissions

| Role | Description |
|---|---|
| **Student** | Browse catalogue, enrol in free courses, track lesson progress, download certificates, message instructors, apply to become an Instructor. |
| **Instructor** | Everything a Student can do (Instructor is a superset — can enrol in other instructors' courses), plus create/edit courses, upload lesson content, submit courses for admin review, and view an instructor dashboard. |
| **Admin** | Manage all users (search, role changes, suspend/reinstate), approve/reject courses and instructor applications, force-unpublish any course, view platform-wide statistics. Cannot enrol in courses. Cannot self-register — must be promoted by an existing Admin or seeded via the dev account. |

> **Note:** Registration only accepts `Student` or `Instructor` roles. Admin self-registration is explicitly rejected server-side.

---

## Features

- **Authentication** — Email/password registration with verification email, Google OAuth 2.0 login, JWT access/refresh token rotation via `httpOnly` cookies, password reset, profile update (username and avatar).
- **Courses** — Catalogue with search and category filter, full CRUD for instructors, submit-for-review → Admin approve/reject workflow, unpublish (self) and force-unpublish (Admin).
- **Sections and Lessons** — CRUD with drag-and-drop reordering. Video and PDF content uploaded through the API as `multipart/form-data` (up to 500MB) and stored on Cloudinary server-side.
- **Enrolment and Progress** — Free enrolment, per-lesson completion tracking, course-completion detection, and unenrolment with cascade-delete of progress, certificate, and message history.
- **Certificates** — PDF certificate auto-issued on course completion using PdfSharpCore.
- **Real-Time Messaging** — SignalR-based messaging scoped per enrolment with live presence status (Online / Busy / Offline).
- **AI Course Tutor** — Gemini-backed chatbot on course detail and lesson player pages. Stateless — conversation history is resent each request, nothing persisted server-side.
- **Instructor Application Workflow** — Student applies → Admin approves or rejects → role promoted automatically on approval.
- **Admin Panel** — User management, course review queue, published-course moderation, instructor application review, platform statistics.
- **CSRF Protection** — CORS locked to a single configured frontend origin plus a custom-header guard middleware on all cookie-authenticated mutating requests.
- **Theming** — System-aware dark/light mode toggle using `next-themes`.

**Out of scope:** Payment processing (all courses are free) and Assignments/Grading.

---

## Project Structure

```
learnhub/
├── backend/
│   ├── LearnHub/                   # ASP.NET Core Web API
│   │   ├── Controllers/            # HTTP endpoints — delegate to Services
│   │   ├── Models/
│   │   │   ├── Entities/           # EF Core entity classes
│   │   │   └── DTOs/               # Request/response shapes grouped by feature
│   │   ├── Services/               # Business logic — one service per domain
│   │   ├── Data/                   # AppDbContext + EF Core Migrations
│   │   ├── Middleware/             # CSRF guard middleware
│   │   ├── Helpers/                # JWT helper, claims extensions, PDF generator
│   │   ├── Hubs/                   # SignalR MessagingHub
│   │   ├── Assets/                 # Certificate PDF template
│   │   └── Program.cs              # DI registration + middleware pipeline
│   └── LearnHub.Tests/             # xUnit test project (287 tests)
│
├── frontend/
│   └── src/
│       ├── app/                    # Next.js App Router routes
│       │   ├── (public)/           # Landing, catalogue, course detail
│       │   ├── (auth)/             # Login, register, forgot/reset password
│       │   ├── (app)/              # Authenticated shell — dashboard, courses, messaging
│       │   └── (learn)/            # Lesson player with minimal chrome
│       ├── components/             # UI components grouped by feature
│       ├── lib/
│       │   ├── api/                # Typed fetch wrappers per backend feature
│       │   ├── signalr/            # SignalR connection hook
│       │   └── cloudinary.ts       # Direct-to-Cloudinary upload helper
│       └── types/                  # TypeScript types mirroring backend DTOs
│
└── .github/workflows/
    ├── backend-ci.yml              # Build + test backend on push to main
    └── frontend-ci.yml             # Lint + test + build frontend on push to main
```

---

## Getting Started

### Prerequisites

Before running the project, make sure you have the following installed and configured:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- A [Neon](https://neon.tech) PostgreSQL database (free tier works)
- A [Cloudinary](https://cloudinary.com) account (free tier works)
- A [Google Cloud](https://console.cloud.google.com) OAuth 2.0 Client ID
- SMTP credentials (e.g. Gmail with an App Password)
- A [Google Gemini API](https://aistudio.google.com/app/apikey) key (free tier works)

---

### Backend Setup

**Step 1 — Copy the environment file:**

```bash
cp backend/LearnHub/.env.example backend/LearnHub/.env
```

**Step 2 — Fill in your environment values** (see [Environment Variables](#environment-variables) below).

**Step 3 — Restore packages and apply database migrations:**

```bash
cd backend/LearnHub
dotnet restore
dotnet ef database update
```

> This applies all EF Core migrations to your Neon PostgreSQL database.

**Step 4 — Run the API:**

```bash
dotnet run
```

The API runs at `http://localhost:5073` by default. Swagger UI is available at `http://localhost:5073/swagger` in Development mode.

> **Admin seed account:** On first run in Development, if no Admin user exists, one is seeded automatically:
> ```
> Email:    admin@learnhub.local
> Password: Admin123!
> ```
> The credentials are also printed to the console log. Override with `Admin__Email` and `Admin__Password` in `.env`.

---

### Frontend Setup

**Step 1 — Copy the environment file:**

```bash
cp frontend/.env.example frontend/.env.local
```

**Step 2 — Fill in your environment values** (see [Environment Variables](#environment-variables) below).

**Step 3 — Install dependencies and run:**

```bash
cd frontend
npm install
npm run dev
```

The frontend runs at `http://localhost:3000`.

> **Important:** The backend must be running before the frontend for API calls to work.

> **Google OAuth local setup:** Add `http://localhost:3000` to the **Authorized JavaScript origins** in your Google Cloud Console OAuth 2.0 credentials. Without this, Google sign-in will be blocked.

> **Cloudinary unsigned upload preset:** Avatar and thumbnail uploads go directly from the browser to Cloudinary using an unsigned upload preset. Create one in your Cloudinary Console under Settings → Upload → Upload presets → Add preset → set mode to **Unsigned**. Set `NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET` to its name. Without this, avatar and thumbnail uploads will fail — everything else works fine.

---

### Demo Accounts

On first run in Development, two published demo courses with real video and PDF lessons are seeded automatically, along with demo accounts so you can try the full enrol → learn → certificate flow immediately:

| Role | Email | Password |
|---|---|---|
| Admin | admin@learnhub.local | Admin123! |
| Instructor | daniel@learnhub.local | Instructor123! |
| Student | priya@learnhub.local | Student123! |

---

### Testing with Swagger or Postman

Auth uses `httpOnly` cookies — logging in via Swagger or Postman automatically stores the cookie for subsequent requests. Because of the CSRF guard, **every mutating request** (`POST`, `PUT`, `PATCH`, `DELETE`) sent while a cookie is present must also include this header:

```
X-Requested-With: LearnHub
```

- **Postman:** Set this once as a collection-level default header.
- **Swagger:** Add it per-request via the header field.

---

## Environment Variables

### Backend (`backend/LearnHub/.env`)

```env
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=<neon-host>;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true

# JWT
JWT__SECRET=your_jwt_secret_key_min_32_chars
JWT__ISSUER=learnhub-api
JWT__AUDIENCE=learnhub-client
JWT__EXPIRYMINUTES=15

# Google OAuth
GOOGLE__CLIENTID=your_google_oauth_client_id

# Gemini AI
GEMINI__APIKEY=your_gemini_api_key

# Cloudinary
CLOUDINARY__CLOUDNAME=your_cloud_name
CLOUDINARY__APIKEY=your_api_key
CLOUDINARY__APISECRET=your_api_secret

# SMTP (e.g. Gmail with App Password)
SMTP__HOST=smtp.gmail.com
SMTP__PORT=587
SMTP__USERNAME=your_gmail@gmail.com
SMTP__PASSWORD=your_gmail_app_password
SMTP__FROMEMAIL=your_gmail@gmail.com
SMTP__FROMNAME=LearnHub

# Frontend origin (CORS allowed origin)
FRONTEND__BASEURL=http://localhost:3000

# Admin seed (optional override)
ADMIN__EMAIL=admin@learnhub.local
ADMIN__PASSWORD=Admin123!
```

> **Neon connection string format:** Neon's dashboard gives you a URI like `postgresql://user:pass@host/db?sslmode=require`. Npgsql does not parse this format — convert it to the `Key=Value` ADO.NET format shown above.

### Frontend (`frontend/.env.local`)

```env
NEXT_PUBLIC_API_URL=http://localhost:5073
NEXT_PUBLIC_GOOGLE_CLIENT_ID=your_google_oauth_client_id
NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=your_cloud_name
NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=your_unsigned_upload_preset
```

---

## Running Tests

### Backend (287 xUnit tests)

```bash
# From the repository root
dotnet test LearnHub.sln

# Or from the test project directory
cd backend/LearnHub.Tests
dotnet test
```

### Frontend (26 Vitest tests)

```bash
cd frontend
npm test           # single run
npm run test:watch  # watch mode
```

---

## API Overview

All routes are prefixed `/api`.

### Auth (`/api/auth`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `register` | Public | Register (Student or Instructor only) |
| POST | `login` | Public | Log in — sets `httpOnly` auth cookies |
| POST | `google` | Public | Google OAuth login or registration |
| POST | `verify-email` | Public | Verify email via token |
| POST | `forgot-password` | Public | Request a password reset email |
| POST | `reset-password` | Public | Reset password via token |
| POST | `refresh` | Refresh cookie | Rotate access and refresh tokens |
| POST | `logout` | Refresh cookie | Revoke refresh token and clear cookies |
| GET | `me` | Authenticated | Current user profile |
| PUT | `me` | Authenticated | Update username or avatar |
| POST | `change-password` | Authenticated | Change password while logged in |

### Courses (`/api/courses`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `` | Public | Catalogue with search, category, and pagination |
| GET | `pending` | Admin | Courses awaiting approval |
| GET | `{id}` | Public | Course detail — includes `isEnrolled` and `isOwner` |
| POST | `` | Instructor | Create course |
| PUT | `{id}` | Instructor | Update course |
| DELETE | `{id}` | Instructor | Delete course (must be unpublished first) |
| POST | `{id}/submit-for-review` | Instructor | Submit for Admin review |
| PUT | `{id}/unpublish` | Instructor | Unpublish own course |
| PUT | `{id}/force-unpublish` | Admin | Force-unpublish any published course |
| POST | `{id}/approve` | Admin | Approve a pending course |
| POST | `{id}/reject` | Admin | Reject a pending course |
| POST | `{id}/chat` | Authenticated | Ask the AI course tutor |

### Sections (`/api/sections`) — Instructor only
| Method | Route | Description |
|---|---|---|
| POST | `` | Create section |
| PUT | `reorder` | Reorder sections |
| PUT | `{id}` | Update section |
| DELETE | `{id}` | Delete section |

### Lessons (`/api/lessons`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Instructor | Create lesson (multipart upload, up to 500MB) |
| PUT | `reorder` | Instructor | Reorder lessons |
| PUT | `{id}` | Instructor | Update lesson (multipart upload) |
| DELETE | `{id}` | Instructor | Delete lesson |
| PUT | `{id}/progress` | Student, Instructor | Update lesson progress |

### Enrollments (`/api/enrollments`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Student, Instructor | Enrol in a course |
| DELETE | `{id}` | Owner, Instructor, Admin | Remove an enrolment |
| GET | `` | Student, Instructor | Current user's enrolments |
| GET | `{id}/progress` | Student, Instructor | Progress for one enrolment |
| GET | `course/{courseId}` | Instructor, Admin | Enrolment roster for a course |

### Certificates (`/api/certificates`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `{enrollmentId}` | Owner or Admin | Certificate metadata with Cloudinary PDF URL |

### Messaging (`/api/messaging`)
| Method | Route | Description |
|---|---|---|
| GET | `conversations` | Current user's conversations |
| GET | `conversations/{conversationId}/messages` | Paginated message history |

> **Note:** Sending messages and read receipts happen over the SignalR hub at `/hubs/messaging`, not via REST.

### Instructor Applications (`/api/instructor-applications`)
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `` | Student | Submit an application |
| GET | `mine` | Student | View own application status |
| GET | `` | Admin | List all applications (filterable by status) |
| POST | `{id}/approve` | Admin | Approve — promotes user to Instructor |
| POST | `{id}/reject` | Admin | Reject application |

### Admin (`/api/admin`) — Admin only
| Method | Route | Description |
|---|---|---|
| GET | `users` | List and search users |
| PUT | `users/{id}/role` | Change a user's role |
| POST | `users/{id}/suspend` | Suspend a user |
| POST | `users/{id}/reinstate` | Reinstate a suspended user |
| GET | `stats` | Platform-wide statistics |

### Dashboard (`/api/dashboard`)
| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `student` | Authenticated | Enrolments, applications, certificates |
| GET | `instructor` | Instructor | Own courses summary |

---

## Deployment

Target architecture: backend on **Render** (Docker web service), frontend on **Vercel**, database on **Neon**.

The backend is container-ready:

- `backend/LearnHub/Dockerfile` — multi-stage .NET 8 build. Listens on port `8080` via `ASPNETCORE_HTTP_PORTS`.
- `Program.cs` calls `UseForwardedHeaders` before `UseHttpsRedirection` to correctly trust Render's `X-Forwarded-Proto` header.
- `Program.cs` calls `Database.MigrateAsync()` on startup — no manual migration step needed after deploy.
- `GET /health` — real DB connectivity check via `AspNetCore.HealthChecks.NpgSql`, ready for Render's health check.

**To deploy:**

1. Create a Render web service pointing to your repository — select Docker as the environment and set `ASPNETCORE_HTTP_PORTS=8080`.
2. Add all backend environment variables in the Render dashboard.
3. Create a Vercel project pointing to the `frontend/` directory.
4. Add all frontend environment variables in the Vercel dashboard.
5. Set `FRONTEND__BASEURL` on Render to your Vercel deployment URL.
6. Add your Vercel URL to the Google OAuth Client's **Authorized JavaScript origins** in Google Cloud Console.

> **Known limitation:** The `httpOnly` cookie authentication approach requires the frontend and backend to share a domain or be routed through a reverse proxy. When hosted on separate Render and Vercel domains, some browsers' enhanced tracking protection may block cross-site cookies. The system works correctly in local Docker Compose where both services share `localhost`.

---

## License

Academic project — University of Westminster Final Project 2025/26.