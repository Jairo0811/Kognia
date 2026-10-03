# Phase 1 — Identity

Status: **In progress**

This phase introduces Kognia's authentication and authorization baseline.

## Scope

- ASP.NET Core Identity
- Student, Instructor and Administrator roles
- Registration
- Login
- JWT access tokens
- Rotating refresh tokens
- Email confirmation token flow
- Password reset token flow
- Authenticated `/api/auth/me` endpoint
- SQL Server persistence through the existing `KogniaDbContext`

## Security baseline

- Unique email addresses
- Confirmed email required before login
- Strong password policy
- Refresh tokens stored as SHA-256 hashes, not plaintext
- Refresh token rotation on use
- JWT issuer, audience, signature and lifetime validation
- Short-lived access tokens

## Development note

Confirmation and password-reset tokens are logged by the API during this phase so the flows can be exercised locally. A production email delivery provider will replace this diagnostic behavior before commercial release.

## Pending before phase closure

- EF Core Identity migration
- Frontend auth screens and auth state
- API integration tests
- CI validation
