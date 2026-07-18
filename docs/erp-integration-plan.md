# ERP AI Integration Plan (পরে করব — reminder)

> User (2026-07-18) reminder: sample ERP database নিয়ে RAG + tool-calling **দুটোই** implement করব।
> লক্ষ্য উদাহরণ: "জুলাই মাসের মোট বিক্রি কত?" (tool → DB query) + "করিম ট্রেডার্সের জন্য ১০ বস্তা
> সিমেন্ট, ৳৮০০০ এর invoice বানাও" (tool → invoice create)। **এখন না — Ollama integration আগে।**

## কী লাগবে (AI ক্ষমতা বানানো নেই — সব আছে; কাজ = ERP-তে জোড়া)

RAG (Phase 3) + tool-calling agent (7h) + multi-tenancy (7d) + guardrails (6d) + memory (6f) — সব তৈরি।
নতুন কাজ = tool গুলো **আসল/sample ERP database-এ যুক্ত করা** + irreversible action-এ confirm।

## যেসব ফাইলে হাত পড়বে (কাজ শুরুর সময় এখান থেকে দেখব)

### A. RAG দিয়ে ERP data Q&A (নিয়ম/policy/manual — text)
- `backend/src/Infrastructure/Rag/KnowledgeBaseSeed.cs` — অথবা document upload দিয়ে ERP doc ঢুকবে (নতুন seed নয়, upload flow-ই যথেষ্ট)
- বিদ্যমান RAG pipeline অপরিবর্তিত — শুধু ERP ডকুমেন্ট feed
- ⚠️ structured row (salary/invoice টেবিল) RAG-এ দুর্বল → ওগুলো tool-calling-এ

### B. Tool-calling — ERP-র সংখ্যা ও কাজ (নতুন ITool impl)
নতুন tool ফাইল (demo tool-এর আয়নায় — `Infrastructure/Agent/CurrentTimeTool.cs`, `KnowledgeBaseSearchTool.cs` দেখো):
- `backend/src/Infrastructure/Agent/` — নতুন ERP tools:
  - `GetTotalSalesTool.cs` (month → DB SUM query) — read tool
  - `GetEmployeeSalaryTool.cs` (name/id → row lookup) — read tool, ⚠️ permission-scoped
  - `CreateInvoiceTool.cs` (customer, items, amount → insert) — **write tool, confirm লাগবে**
- প্রতিটা `ITool` implement করে (`Application/Common/Interfaces/ITool.cs`)
- DI register: `Infrastructure/DependencyInjection.cs` (demo tool যেভাবে আছে)
- MCP bridge (ঐচ্ছিক): `Api/Mcp/AiPlatformMcpTools`-এ যোগ (7h-ii প্যাটার্ন)

### C. Sample ERP database
- নতুন EF entity/DbContext বা আলাদা read-only connection — sample schema (customers, products, invoices, employees, salaries)
- সিদ্ধান্ত পরে: AiPlatform-এর AppDbContext-এ ঢুকবে নাকি আলাদা "ErpDbContext"/আলাদা DB (সম্ভবত আলাদা — ERP নিজের data, auth থেকে বিচ্ছিন্ন)
- Migration লাগবে; seed data (কিছু customer/invoice/employee)

### D. Guardrails (write action safety)
- irreversible tool (invoice create/delete) → agent execute করার আগে **user confirm** (frontend + backend flag)
- বিদ্যমান `IPromptGuard` (6d) input-এ; এটা **action-level** নতুন গার্ড
- audit log (7d-ii) — কোন AI-action কে করল

### E. Frontend
- চ্যাট UI-তে agent mode (7h `/agent` endpoint আছে) — ERP প্রশ্ন ওখানে
- tool-call চলাকালীন "invoice বানাচ্ছি... confirm?" UI

## ক্রম (পরে)
1. Sample ERP DB + entity (C)
2. Read tools আগে (get_total_sales, get_employee_salary) — নিরাপদ (B)
3. RAG-এ ERP doc feed (A)
4. Write tool + confirm guardrail (create_invoice) — শেষে, সাবধানে (B+D)
5. Frontend agent UX (E)

## নীতি (মনে রাখা)
- Ollama integration **আগে** শেষ হবে, তারপর এটা
- read tool আগে, write tool শেষে (data নিরাপত্তা)
- write action = সবসময় confirm (irreversible)
- ছোট step, কিছু ভাঙা যাবে না — Ollama-র মতোই
