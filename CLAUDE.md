# CLAUDE.md — AiPlatform

## Stack
- Backend: .NET 10 Web API, Clean Architecture (Api → Application → Domain; Infrastructure → Application/Domain).
- CQRS + MediatR, FluentValidation, Serilog, ProblemDetails, Swagger, API versioning (`/api/v{n}`).
- Frontend (Phase 4+): React + TS + Vite, Tailwind + shadcn/ui, react-hook-form + zod, framer-motion, sonner. **State/data: Redux Toolkit + RTK Query** (server-state + client-state). Silent refresh via RTK Query custom baseQuery (re-auth wrapper); access token in memory (Redux slice), never localStorage.
- Data: SQL Server + EF Core, Redis, Qdrant. Solution file is `AiPlatform.slnx` (not .sln).

## Repo structure (from Phase 4)
- `backend/` — .NET solution (`src/`, `tests/`, `AiPlatform.slnx`). Run: `dotnet run --project backend/src/Api`.
- `frontend/` — React app, feature-based (`src/features/{auth,chat,rag,documents}`), mirrors backend feature organization.
- Root: `docs/`, `docker-compose.yml`, `PLAN.md`, `TODO.md`, `CLAUDE.md`.

## Run
- Build: `dotnet build backend/AiPlatform.slnx`
- API: `dotnet run --project backend/src/Api` → http://localhost:5076 (swagger `/swagger`, health `/health`)
- Test: `dotnet test backend/AiPlatform.slnx`
- Frontend: `cd frontend && npm run dev` → http://localhost:5173
- EF migration: `dotnet ef ... --project backend/src/Infrastructure --startup-project backend/src/Api`

## Conventions
- প্রতিটা capability = আলাদা, স্বাধীনভাবে-testable feature (`Application/Features/<Name>`)।
- **এক type = এক ফাইল।** Command/Query, Validator, Handler আলাদা ফাইলে (এক ফাইলে জড়ো নয়)।
- **LLM prompt = hybrid:** instruction/structure **English** (format/JSON বেশি reliable), কিন্তু explicit বলো output **বাংলায়** দিতে (free-text field: summary, answer ইত্যাদি বাংলায়)। enum value English। analyzer/RAG/future সব prompt এই নিয়মে।
- Generic reusable code ও app-specific/demo code আলাদা রাখো (boilerplate goal — PLAN §4)।
- Interface Application-এ, implementation Infrastructure-এ (IChatProvider, IVectorStore…)।
- নতুন external service DI: `Infrastructure/DependencyInjection.cs`।
- **নতুন API endpoint বানালেই `src/Api/AiPlatform.http` update করো** — প্রতিটার জন্য valid + invalid payload + `# Expected:` comment (কী response ও কেন)। এটাই live test-kit + doc।

## Error handling — layered, no try/catch spam
- Expected/business failure (user-not-found, bad-credentials) → **Result pattern**, throw করবে না।
- Invalid input → MediatR `ValidationBehavior` (FluentValidation) → 400, handler-এর আগেই।
- Unexpected (bug/infra) → global `IExceptionHandler` → ProblemDetails; type→status map (Validation→400, NotFound→404, else 500 + log, internal detail leak না)।
- try/catch শুধু সত্যিকার recover-able জায়গায় (LLM/DB call retry) — প্রতি block-এ নয়।

## Don'ts
- কোনো API key/secret কোডে বা appsettings-এ hardcode করবে না — user-secrets / env only.
- Domain কোনো package/layer-এর উপর নির্ভর করবে না।
- over-engineer না — সবচেয়ে সরল যেটা কাজ করে (PLAN থিম)। phase-এর বাইরে scope নেবে না।

## Quality bar — FAANG-level, non-negotiable
সব code/query/function industry-standard, production-grade হতে হবে:
- সরল ও readable > clever; boring code জেতে। over-engineer = junior signal।
- সঠিক naming, single responsibility, small functions, testable units।
- Error handling + input validation trust boundary-তে; edge case ভাবা।
- Security ও performance সচেতন, কিন্তু premature optimization না।
- প্রতি non-trivial logic-এ test। magic number/string না।
- এটা "simplest thing that works" থিমের সাথে সাংঘর্ষিক নয় — একই জিনিস।

## Guide style
- ব্যাখ্যা বাংলায়, beginner-friendly; কোড/identifier/commit ইংরেজিতে।
- ছোট increment, প্রতি phase checkpoint-এ থেমে user-এর "go" নাও।

## Plan
পূর্ণ roadmap: `PLAN.md`. এখন শেষ: **Phase 0 (foundation)**.
