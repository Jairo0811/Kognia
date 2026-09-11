# Kognia

**Kognia** is a modern learning SaaS platform focused on courses, learning paths, assessments, certifications, subscriptions, and measurable learner progress.

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
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Clean Architecture

### Frontend
- React 19
- TypeScript
- Vite
- React Router
- TanStack Query

### Tooling
- Docker Compose
- ESLint
- GitHub Actions
- xUnit

## Repository Structure

```text
Kognia/
├── backend/
│   ├── src/
│   │   ├── Kognia.Domain/
│   │   ├── Kognia.Application/
│   │   ├── Kognia.Infrastructure/
│   │   └── Kognia.Api/
│   └── tests/
│       └── Kognia.ArchitectureTests/
├── frontend/
├── docs/
├── .github/workflows/
└── docker-compose.yml
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

## Current Status

**Phase 0 — Foundation: complete.**

Next milestone: **Phase 1 — Identity**.

See [`docs/roadmap.md`](docs/roadmap.md) and [`docs/phase-0-foundation.md`](docs/phase-0-foundation.md) for project status and scope.
