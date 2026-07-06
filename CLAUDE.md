# CLAUDE.md — AiPlatform

## Stack
- Backend: .NET 10 Web API, Clean Architecture (Api → Application → Domain; Infrastructure → Application/Domain).
- CQRS + MediatR, FluentValidation, Serilog, ProblemDetails, Swagger, API versioning (`/api/v{n}`).
- Frontend (Phase 4+): React + TS + Vite + Tailwind + shadcn/ui.
- Data: SQL Server + EF Core, Redis, Qdrant. Solution file is `AiPlatform.slnx` (not .sln).

## Run
- Build: `dotnet build AiPlatform.slnx`
- API: `dotnet run --project src/Api` → http://localhost:5076 (swagger `/swagger`, health `/health`)
- Test: `dotnet test`

## Conventions
- প্রতিটা capability = আলাদা, স্বাধীনভাবে-testable feature (`Application/Features/<Name>`)।
- Generic reusable code ও app-specific/demo code আলাদা রাখো (boilerplate goal — PLAN §4)।
- Interface Application-এ, implementation Infrastructure-এ (IChatProvider, IVectorStore…)।
- নতুন external service DI: `Infrastructure/DependencyInjection.cs`।

## Don'ts
- কোনো API key/secret কোডে বা appsettings-এ hardcode করবে না — user-secrets / env only.
- Domain কোনো package/layer-এর উপর নির্ভর করবে না।
- over-engineer না — সবচেয়ে সরল যেটা কাজ করে (PLAN থিম)। phase-এর বাইরে scope নেবে না।

## Guide style
- ব্যাখ্যা বাংলায়, beginner-friendly; কোড/identifier/commit ইংরেজিতে।
- ছোট increment, প্রতি phase checkpoint-এ থেমে user-এর "go" নাও।

## Plan
পূর্ণ roadmap: `PLAN.md`. এখন শেষ: **Phase 0 (foundation)**.
