# Kognia

**Kognia** is a modern learning SaaS platform focused on courses, learning paths, assessments, certifications, subscriptions, engagement and measurable learner progress.

> Aprende. Avanza. Domina.

## Origins

Kognia originated from a final project developed at **Universidad APEC (UNAPEC)** for the course **Gestión de Sitios Web (ISO-700)** during the **May–August 2024** academic period, under professor **Delby Acosta Taveras**.

The original team was:

- **Francis Jairo Matias Rosario** — A00115261
- **Eliandres Rodriguez Cepeda** — A00112070
- **Ramon Rosario Rodriguez** — A00110961

In 2026, the original academic e-learning concept was recovered and rebuilt from the ground up as a production-oriented SaaS platform under the name **Kognia**.

See [`docs/original-unapec-project.md`](docs/original-unapec-project.md) for the full academic project lineage.

## Stack

### Backend
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity + JWT/refresh tokens
- Clean Architecture

### Frontend
- React 19
- TypeScript
- Vite
- React Router
- TanStack Query

### Platform
- Docker / Docker Compose
- Nginx
- GitHub Actions
- xUnit integration and architecture tests
- ESLint

## Product Scope

Kognia includes identity, course catalog and authoring, enrollment and learning progress, quizzes, certificates, subscriptions and billing persistence, student/instructor/admin dashboards, reviews, favorites, notifications and role-aware analytics.

## Repository Structure

```text
Kognia/
├── backend/
│   ├── src/
│   │   ├── Kognia.Domain/
│   │   ├── Kognia.Application/
│   │   ├── Kognia.Infrastructure/
│   │   └── Kognia.Api/
│   ├── tests/
│   └── Dockerfile
├── frontend/
│   ├── src/
│   ├── Dockerfile
│   └── nginx.conf
├── docs/
├── .github/workflows/
├── docker-compose.yml
└── docker-compose.production.yml
```

## Development Flow

```text
main
└── dev
    └── feature/*
```

`main` represents stable releases. Integration happens in `dev`, and feature work is developed through short-lived `feature/*` branches.

## Local Development

### Database

```bash
docker compose up -d
```

### Backend

```bash
cd backend
dotnet restore Kognia.slnx
dotnet run --project src/Kognia.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Production Template

Set strong environment-owned values for `MSSQL_SA_PASSWORD`, `JWT_KEY` and `PUBLIC_ORIGIN`, then build/run with:

```bash
docker compose -f docker-compose.production.yml up -d --build
```

Never deploy using the committed development JWT key or development SQL password; the API rejects those defaults outside Development/Testing.

## Current Status

**Phases 0–16: implemented.**

Kognia is in **Release Candidate** state, gated by CI and the dedicated Release Candidate workflow. Production launch still requires infrastructure provisioning and real provider credentials/services where applicable.

See [`docs/roadmap.md`](docs/roadmap.md), [`docs/final-block.md`](docs/final-block.md) and the block documents under `docs/` for scope and validation history.
