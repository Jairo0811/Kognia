# Kognia

**Kognia** is a modern learning SaaS platform focused on courses, learning paths, assessments, certifications, subscriptions, and measurable learner progress.

> Aprende. Avanza. Domina.

## Origins

Kognia was originally conceived in 2024 as a final project for the **Web Site Management** course at Universidad APEC. In 2026, the original academic e-learning concept was rebuilt from the ground up as a production-oriented SaaS platform.

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

See [`docs/roadmap.md`](docs/roadmap.md) for the complete product roadmap.
