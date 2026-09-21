<div align="center">

# Kognia

<img src="https://img.shields.io/badge/UNAPEC-ISO--700-003B70?style=for-the-badge" alt="UNAPEC ISO-700" />
<img src="https://img.shields.io/badge/Versión-0.1.0-2563EB?style=for-the-badge" alt="Versión 0.1.0" />
<img src="https://img.shields.io/badge/Estado-Fase%200%20completada-14B8A6?style=for-the-badge" alt="Estado: Fase 0 completada" />
<img src="https://img.shields.io/badge/Tipo-SaaS%20%7C%20Portafolio-6F42C1?style=for-the-badge" alt="SaaS y proyecto de portafolio" />

<br/><br/>

<a href="https://github.com/Jairo0811/Kognia/actions/workflows/ci.yml">
  <img src="https://github.com/Jairo0811/Kognia/actions/workflows/ci.yml/badge.svg" alt="CI" />
</a>

<br/><br/>

**Aprende. Avanza. Domina.**

*Learning paths, assessments, certifications and measurable progress.*

</div>

## 📌 Descripción

**Kognia** es una plataforma moderna de aprendizaje concebida como evolución de un proyecto académico de e-learning. Su visión de producto contempla cursos, rutas de aprendizaje, evaluaciones, certificados, suscripciones y seguimiento medible del progreso del estudiante.

La reconstrucción de 2026 parte desde una fundación técnica nueva basada en **.NET 10, React 19, TypeScript y SQL Server**, con Clean Architecture, Docker Compose y CI desde la primera fase.

> 🎓 **Origen académico:** Kognia evoluciona a partir del proyecto final de **Gestión de Sitios Web (ISO-700)** de la **Universidad APEC (UNAPEC)**, desarrollado durante el período **Mayo - Agosto 2024**.

---

## 🎓 Información académica

| Información | Detalle |
|---|---|
| 📖 Asignatura | **Gestión de Sitios Web (ISO-700)** |
| 👨‍🏫 Profesor | **Delby Acosta Taveras** |
| 🏫 Institución | **Universidad APEC (UNAPEC)** |
| 📅 Período académico | **Mayo - Agosto 2024** |
| 📁 Tipo de entrega | **Proyecto Final** |

### 👥 Equipo académico original

| 👤 Integrante | 🆔 Matrícula |
|---|---|
| Francis Jairo Matias Rosario | A00115261 |
| Eliandres Rodriguez Cepeda | A00112070 |
| Ramon Rosario Rodriguez | A00110961 |

La implementación actual es una reconstrucción posterior del concepto académico original, con arquitectura, identidad de producto y base técnica propias.

La trazabilidad histórica ampliada se encuentra en [`docs/original-unapec-project.md`](docs/original-unapec-project.md).

---

## 🧭 Continuidad académica

### 👥 Continuidad por estudiante

**Eliandres Rodriguez Cepeda (A00112070)** participó junto a Francis Jairo Matias Rosario en **Kognia**, correspondiente a **Gestión de Sitios Web (ISO-700)** durante **Mayo - Agosto 2024**. Posteriormente, ambos vuelven a coincidir en [**SOAForge**](https://github.com/Jairo0811/SOA-Forge) durante **Septiembre - Diciembre 2026**, donde Eliandres participa exclusivamente en **Integración de Aplicaciones con Tecnología Open Source (ISO-815)**.

| Orden | Asignatura | Proyecto | Período |
|---:|---|---|---|
| 1 | Gestión de Sitios Web (ISO-700) | **Kognia** | Mayo - Agosto 2024 |
| 2 | Integración de Aplicaciones con Tecnología Open Source (ISO-815) | [**SOAForge**](https://github.com/Jairo0811/SOA-Forge) | Septiembre - Diciembre 2026 |

La continuidad se documenta por coincidencia verificable de **nombre completo y matrícula**. Kognia y SOAForge son proyectos independientes y no existe dependencia técnica entre ellos.

---

## 🎯 Visión del producto

Kognia busca consolidar en una sola plataforma:

- catálogo de cursos y categorías;
- rutas de aprendizaje;
- experiencia de estudio por lecciones;
- evaluaciones y calificaciones;
- progreso y reanudación del aprendizaje;
- certificados verificables;
- paneles para estudiante, instructor y administración;
- suscripciones Free/Premium;
- analítica de aprendizaje y métricas comerciales;
- accesibilidad, seguridad y UX como requisitos de producto.

La monetización, pagos y funcionalidades comerciales pertenecen a fases posteriores; **Fase 0** establece únicamente la fundación técnica y arquitectónica.

---

## 🧱 Stack tecnológico

### ⚙️ Backend

<p>
  <img src="https://skillicons.dev/icons?i=dotnet,cs" alt=".NET y C#" />
</p>

- **.NET 10**;
- **ASP.NET Core Web API**;
- **C#**;
- **Entity Framework Core**;
- Clean Architecture;
- endpoint base de health check.

### 🎨 Frontend

<p>
  <img src="https://skillicons.dev/icons?i=react,ts,vite" alt="React, TypeScript y Vite" />
</p>

- **React 19.1**;
- **TypeScript 5.9**;
- **Vite 7**;
- React Router 7;
- TanStack Query 5;
- ESLint.

### 🗄️ Datos

<p>
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" alt="Microsoft SQL Server" width="52" height="52" />
</p>

- Microsoft SQL Server;
- Entity Framework Core;
- entorno de desarrollo mediante Docker Compose.

### 🧰 DevOps y calidad

<p>
  <img src="https://skillicons.dev/icons?i=docker,git,github,githubactions" alt="Docker, Git, GitHub y GitHub Actions" />
</p>

- Docker / Docker Compose;
- Git / GitHub;
- GitHub Actions;
- xUnit / Architecture Tests;
- lint y build del frontend en CI.

---

## 🏗️ Arquitectura

Kognia utiliza una **Clean Architecture pragmática** con separación clara de dominio, aplicación, infraestructura y presentación.

```text
Kognia/
├── backend/
│   ├── Kognia.slnx
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

Flujo de dependencias esperado:

```text
Domain ← Application ← Infrastructure ← API
React Web ----------------------------→ API
```

La capa de dominio no debe depender de persistencia, infraestructura ni presentación.

Documentación técnica ampliada: [`docs/architecture.md`](docs/architecture.md).

---

## 🌿 Flujo de desarrollo

```text
main
└── dev
    └── feature/*
```

- `main`: base estable;
- `dev`: integración;
- `feature/*`: trabajo incremental por capacidad.

---

## 🚀 Desarrollo local

### Base de datos

```bash
docker compose up -d
```

### Backend

```bash
cd backend
dotnet restore Kognia.slnx
dotnet build Kognia.slnx
dotnet test Kognia.slnx
dotnet run --project src/Kognia.Api
```

### Frontend

```bash
cd frontend
npm install
npm run lint
npm run build
npm run dev
```

---

## 🔄 Integración continua

El workflow [`ci.yml`](.github/workflows/ci.yml) valida actualmente:

**Backend**

- restore de la solución;
- build Release;
- pruebas automatizadas.

**Frontend**

- instalación de dependencias;
- lint;
- build de producción.

El pipeline se ejecuta sobre `main`, `dev` y pull requests asociados a ambas ramas.

---

## 🗺️ Roadmap

| Fase | Alcance | Estado |
|---:|---|:---:|
| 0 | Foundation | ✅ |
| 1 | Identity | ⏳ |
| 2 | Course Catalog | ⏳ |
| 3 | Instructor CMS | ⏳ |
| 4 | Learning Experience | ⏳ |
| 5 | Assessments | ⏳ |
| 6 | Certificates | ⏳ |
| 7 | Subscriptions & Payments | ⏳ |
| 8 | Student Dashboard | ⏳ |
| 9 | Instructor Dashboard | ⏳ |
| 10 | Admin Portal | ⏳ |
| 11 | Engagement | ⏳ |
| 12 | Analytics | ⏳ |
| 13 | Accessibility & UX Hardening | ⏳ |
| 14 | Security & Commercial Hardening | ⏳ |
| 15 | Release Candidate | ⏳ |

El detalle de cada fase se mantiene en [`docs/roadmap.md`](docs/roadmap.md).

---

## 📊 Estado actual

**Fase 0 — Foundation: completada.**

La base actual ya incluye estructura de repositorio, Clean Architecture, frontend React, backend .NET, persistencia SQL Server, Docker Compose, GitHub Actions, routing, TanStack Query, lint y health check.

El siguiente hito es **Fase 1 — Identity**.

Consulta también [`docs/phase-0-foundation.md`](docs/phase-0-foundation.md).

---

<p align="center">
  <strong>Kognia · Aprende. Avanza. Domina.</strong><br/>
  Universidad APEC (UNAPEC) · Gestión de Sitios Web (ISO-700)
</p>
