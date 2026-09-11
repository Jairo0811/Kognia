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

## Dependency injection

Phase 0 establishes explicit composition modules:

- `Kognia.Application.AddApplication()`
- `Kognia.Infrastructure.AddInfrastructure(configuration)`

The API acts as the composition root and wires both modules during startup.

## Persistence

SQL Server is the primary relational database and Entity Framework Core is the persistence layer. The foundation includes `KogniaDbContext`, SQL Server registration and automatic discovery of entity configurations from the Infrastructure assembly.

Database migrations will begin when the first persistent Identity model is introduced in Phase 1.

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

The Phase 0 shell includes React Router and TanStack Query providers. Feature folders are introduced incrementally as modules are implemented.

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

## Testing and CI

Backend test projects live under `backend/tests`. The foundation includes an initial architecture smoke test.

GitHub Actions validates pull requests targeting `dev` or `main` by running backend restore/build/tests and frontend install/lint/build.

## Non-functional priorities

1. Security by default.
2. Accessibility and keyboard usability.
3. Responsive UI.
4. Explicit validation and error handling.
5. Observability and structured logging.
6. Maintainable modular boundaries.
7. Automated tests for critical business rules.
