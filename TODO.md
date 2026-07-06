# TODO — phase tracker (details: PLAN.md)

- [x] **Phase 0 — Foundation**: sln + 4 layers + tests, DI wiring, Serilog, ProblemDetails, Swagger(versioned), CORS, health, `/api/v1/ping`, .gitignore, .env.example, README, CLAUDE.md. ✔ build+run verified.
- [ ] Phase 1 — Auth (Identity + JWT + refresh rotation, Redis, SQL Server)
- [ ] Phase 2 — LLM core (chat + analyzer, provider abstraction, streaming)
- [ ] Phase 3 — RAG + Qdrant (upload + background ingestion + citations)
- [ ] Phase 4 — Frontend core (premium auth, silent refresh, chat/RAG UI)
- [ ] Phase 5 — Docker + load balancing (nginx, api×2, redis, qdrant, sqlserver)
- [ ] Phase 6 — AI-specific (semantic cache, routing, observability, guardrails, rerank, memory, evals)
- [ ] Phase 7 — Infra/product advanced (multi-tenancy, queue, dashboards, MCP)
- [ ] Phase 8 — Quality/DevEx (tests, CI/CD, OTel)
- [ ] Phase 9 — Boilerplate packaging (template, conventions, strip demo)
