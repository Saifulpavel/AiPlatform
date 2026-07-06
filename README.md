# AiPlatform

Production-grade full-stack AI platform — .NET 10 (Clean Architecture) backend + React/TS frontend.
Chat, structured analyzer, RAG (keyword/embedding/chunking/Qdrant), multi-provider LLM, JWT auth.

> Status: **Phase 0 — foundation** (skeleton only, no features yet). Full plan: [PLAN.md](PLAN.md).

## Prerequisites
- .NET 10 SDK
- (later phases) Docker Desktop — SQL Server, Redis, Qdrant

## Structure
```
src/Api             — controllers, DI wiring, Swagger, versioning, middleware
src/Application     — CQRS handlers, MediatR, validators, interfaces
src/Domain          — entities, value objects (no dependencies)
src/Infrastructure  — EF Core, Redis, Qdrant, LLM providers (implementations)
tests/Application.Tests — xUnit
```

## Run
```bash
dotnet build AiPlatform.slnx
dotnet run --project src/Api
```
- Swagger UI: http://localhost:5076/swagger
- Health:     http://localhost:5076/health
- Sample:     http://localhost:5076/api/v1/ping

## Config & secrets
- Local: `dotnet user-secrets` (API keys, connection strings) — **কখনো কোডে/appsettings-এ নয়**।
- Copy `.env.example` → `.env` for docker/frontend values.
