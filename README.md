# FamilyHub

A full-stack starter project. This repository contains the scaffolding only — the
projects build and run, but no business features are implemented yet.

## Tech Stack

### Frontend — `frontend/familyhub-web`
- [React](https://react.dev/) + [TypeScript](https://www.typescriptlang.org/)
- [Vite](https://vite.dev/) (build tool / dev server)
- [React Router](https://reactrouter.com/) — client-side routing
- [Tailwind CSS](https://tailwindcss.com/) (v4, via `@tailwindcss/vite`) — styling
- [Axios](https://axios-http.com/) — HTTP client
- [React Hook Form](https://react-hook-form.com/) — forms
- [i18next](https://www.i18next.com/) / react-i18next — internationalization

### Backend — `backend/FamilyHub.Api`
- [ASP.NET Core 8](https://learn.microsoft.com/aspnet/core) Web API (`net8.0`)
- [Entity Framework Core 8](https://learn.microsoft.com/ef/core) + SQL Server provider
- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [JWT Bearer authentication](https://learn.microsoft.com/aspnet/core/security/authentication/jwt)
- [Swagger / Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Serilog](https://serilog.net/) — structured logging

## Repository Structure

```
FamilyHub/
├── frontend/
│   └── familyhub-web/          # React + TypeScript + Vite app
├── backend/
│   ├── FamilyHub.sln
│   ├── global.json             # pins the .NET 8 SDK
│   └── FamilyHub.Api/          # ASP.NET Core 8 Web API
├── database/
│   └── scripts/                # SQL scripts / EF migration output
├── docs/                       # project documentation
├── .gitignore
└── README.md
```

## Prerequisites
- [Node.js](https://nodejs.org/) 20+ and npm
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or full) — required once data access is wired up

## Getting Started

### Backend
```bash
cd backend
dotnet restore
dotnet build
dotnet run --project FamilyHub.Api
```
Swagger UI is served at `https://localhost:<port>/swagger` in Development.

### Frontend
```bash
cd frontend/familyhub-web
npm install
npm run dev      # start the Vite dev server
npm run build    # type-check and produce a production build
```

## Status
Scaffolding complete — dependencies installed and both the frontend and backend
build successfully. Business features are intentionally not implemented yet.
