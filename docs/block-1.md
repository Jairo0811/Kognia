# Block 1 — Identity, Course Catalog and Instructor CMS

Block 1 combines the first three functional phases of Kognia.

## Phase 1 — Identity

Implemented baseline:

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

Implemented baseline:

- Category, Course, CourseSection and Lesson domain models
- Public category endpoint
- Public published-course catalog endpoint
- Search by title/summary
- Filtering hooks for category and level
- Public course detail endpoint
- Seed categories for Programming, Digital Marketing and Personal Development
- Frontend course catalog screen

## Phase 3 — Instructor CMS

Implemented baseline:

- Instructor/Administrator authorization policy
- Instructor course list
- Create and edit course metadata
- Create sections and lessons
- Publish validation requiring at least one section and lesson
- Draft/published lifecycle
- Instructor ownership enforcement
- Basic frontend instructor workspace for course creation and listing

## Remaining hardening before Block 1 closure

- Generate and validate the EF Core migration containing Identity + catalog schema
- Add API integration tests for identity, catalog and instructor authorization
- Expand frontend instructor editing for sections and lessons
- Validate CI and resolve any compile/lint/test regressions
- Replace development token logging with an email delivery abstraction before production

Block 1 must not be promoted to `dev` until CI is green and the remaining hardening items required for closure are resolved.
