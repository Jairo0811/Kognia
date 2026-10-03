# Block 1 — Identity, Course Catalog and Instructor CMS

Block 1 combines the first three functional phases of Kognia and is functionally complete.

## Phase 1 — Identity

Completed:

- ASP.NET Core Identity
- Student, Instructor and Administrator roles
- Registration and login
- Email confirmation flow
- Forgot/reset password flow
- JWT access tokens
- Rotating refresh tokens stored as SHA-256 hashes
- Authenticated `/api/auth/me`
- Frontend login, registration and forgot-password screens

## Phase 2 — Course Catalog

Completed:

- Category, Course, CourseSection and Lesson domain models
- Public category endpoint
- Public published-course catalog endpoint
- Search by title/summary
- Filtering hooks for category and level
- Public course detail endpoint
- Seed categories for Programming, Digital Marketing and Personal Development
- Frontend course catalog screen

## Phase 3 — Instructor CMS

Completed:

- Instructor/Administrator authorization policy
- Instructor course list
- Instructor course detail endpoint with sections and lessons
- Create and edit course metadata endpoints
- Create sections and lessons
- Frontend content editor for sections and lessons
- Draft/published lifecycle
- Publish validation requiring at least one section and one lesson
- Instructor ownership enforcement

## Persistence

Completed:

- Initial EF Core migration for ASP.NET Core Identity, refresh tokens and course catalog schema
- SQL Server schema startup now uses `Database.MigrateAsync()` outside the integration-test environment
- Integration tests continue to use the EF Core InMemory provider with `EnsureCreatedAsync()`
- Identity roles and initial catalog categories are seeded after database initialization

## Validation

Completed through GitHub Actions:

- Backend restore
- Backend build
- Architecture tests
- API integration tests
- Frontend install
- Frontend lint
- Frontend production build

Integration coverage includes:

- Health endpoint
- Student registration
- Public categories/catalog access
- Instructor endpoint authentication requirement

## Deferred release hardening

The following items are intentionally assigned to later hardening phases rather than Block 1 functional scope:

- Replace development confirmation/reset-token logging with a production email-delivery provider
- Remediate dependency security advisories reported by NuGet audit
- Add broader end-to-end browser coverage and production observability

## Status

**Block 1 complete — Phases 1, 2 and 3 are ready for integration into `dev` once the final CI run for this branch is green.**
