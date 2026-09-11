# Kognia Architecture

## Architectural style

Kognia uses **Clean Architecture** to keep business rules independent from infrastructure concerns.

## Backend projects

- `Kognia.Domain`: entities, enums, value objects, domain rules.
- `Kognia.Application`: use cases, DTOs, interfaces, validation, application services.
- `Kognia.Infrastructure`: EF Core, SQL Server, Identity, external services, storage, payments and email.
- `Kognia.Api`: HTTP entry point, controllers/endpoints, middleware, dependency injection and API configuration.

Dependency direction:

```text
Kognia.Api ───────────────┐
                         v
Kognia.Infrastructure -> Kognia.Application -> Kognia.Domain
```

The Domain project must not depend on infrastructure or presentation concerns.

## Frontend architecture

The React application is feature-oriented:

```text
src/
├── app/
├── assets/
├── components/
├── features/
│   ├── auth/
│   ├── courses/
│   ├── learning/
│   ├── instructor/
│   ├── subscriptions/
│   └── admin/
├── layouts/
├── pages/
├── routes/
├── services/
├── hooks/
├── types/
└── utils/
```

## Core domains

- Identity & Profiles
- Courses & Catalog
- Enrollments
- Learning Progress
- Assessments
- Certificates
- Subscriptions & Payments
- Reviews & Favorites
- Notifications
- Analytics

## Data platform

- SQL Server
- Entity Framework Core
- Database migrations managed from Infrastructure

## Authentication

Planned authentication stack:

- ASP.NET Core Identity
- JWT access tokens
- Refresh tokens
- Email confirmation
- Password reset
- Role-based authorization

Initial roles:

- Student
- Instructor
- Administrator

## Non-functional priorities

1. Security by default.
2. Accessibility and keyboard usability.
3. Responsive UI.
4. Explicit validation and error handling.
5. Observability and structured logging.
6. Maintainable modular boundaries.
7. Automated tests for critical business rules.
