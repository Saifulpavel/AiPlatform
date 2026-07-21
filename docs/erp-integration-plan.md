# ERP AI Integration Plan

> **2026-07-20 — কাজ শুরু হলো।** Ollama integration শেষ, hybrid routing live। schema চূড়ান্ত (নিচে §Schema), `ErpDbContext` + entity বানানো চলছে।



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

## Schema — চূড়ান্ত (2026-07-20, ১৫ table)

sample ERP = **IT পণ্যের ব্যবসা** (computer, laptop, accessories)। multi-tenant (Company → Location), approval-driven stock.

### Convention
- সব PK/FK = `bigint identity`; টাকা = `decimal(18,2)`
- প্রতি table-এ `CreatedAt datetime`, `CreatedBy bigint` (ErpUser-এর id, plain column — ১৫টা cascade path এড়াতে FK নয়)
- **`Code` শুধু**: Company, Location, Supplier, Customer, ErpUser, `PurchaseOrder.PurchaseOrderNo`, `SalesOrder.SalesOrderNo`, `SalesOrder.InvoiceNo` — Product/detail/serial-এ Code নেই
- Code pattern: `SUP-{Company.Code}-{Location.Code}-{Year}-{000001}` — Company+Location wise unique
- Detail/Serial table তাদের header থেকে tenancy পায় (নিজেদের CompanyId/LocationId নেই)
- **Product location-wise আলাদা row** — একই laptop HO আর CB-তে দুই ProductId

### Table তালিকা
| # | Table | ভূমিকা |
|---|---|---|
| 1 | Company | tenant root |
| 2 | Location | branch (HO/MB/CB) |
| 3 | ErpUser | ERP-র কর্মী; `ApprovedBy`/`CreatedBy` reference (auth নেই) |
| 4 | Product | IT পণ্য, `IsSerialized` flag |
| 5 | Supplier | Purchase-এর party |
| 6 | Customer | Sales-এর party |
| 7-8-9 | PurchaseOrder → Detail → Serial | stock **IN** |
| 10-11-12 | SalesOrder → Detail → Serial | stock **OUT** |
| 13 | CurrentStock | **ledger** — প্রতি approved movement-এ এক row |
| 14 | CurrentStockDetail | গুদামে এই মুহূর্তে থাকা প্রতিটা serial |
| 15 | StockBalance | **summary** — Product+Location wise চলতি ব্যালেন্স |

### Stock movement (শুধু approval-এ ঘটে)
```
Purchase approved:  INSERT CurrentStock (Quantity = +qty, Cost = UnitPrice, ReferenceNo = PurchaseOrderNo)
                    StockBalance.QuantityOnHand += qty
                    প্রতি PurchaseOrderSerial -> INSERT CurrentStockDetail
Sales approved:     InvoiceNo assign
                    INSERT CurrentStock (Quantity = -qty, Cost = কেনার খরচ, ReferenceNo = SalesOrderNo)
                    StockBalance.QuantityOnHand -= qty
                    প্রতি SalesOrderSerial -> DELETE CurrentStockDetail (serial গুদাম ছাড়ল)
Non-serialized:     শুধু quantity +/- , serial row নেই
Unapproved:         stock-এ কোনো প্রভাব নেই
```
- invariant: `StockBalance.QuantityOnHand == SUM(CurrentStock.Quantity)` (একই Product+Location)
- **`CurrentStock.Cost` সবসময় কেনার খরচ** (বিক্রয়মূল্য নয়) — এই column stock **valuation**-এর জন্য। বিক্রয়মূল্য `SalesOrderDetail.UnitPrice`-এ আছে; লাভ = দুইয়ের পার্থক্য। বিক্রয়মূল্য বসালে গুদাম খালি হয়েও stock value **negative** দেখাত।
- একই পণ্য ভিন্ন দামে কেনা হলে: **serialized** → `CurrentStockDetail` দিয়ে ঐ serial-এর হুবহু cost; **non-serialized** → average cost

## সিদ্ধান্ত: ErpUser ↔ AiPlatform AppUser (2026-07-20)

**hard FK কখনোই নয়** — (ক) আলাদা DbContext, EF cross-context FK enforce করতে পারে না; (খ) পরে আসল ERP DB বসালে তার টেবিলে আমাদের Identity-র FK যোগ করা যাবে না (swap-ability নষ্ট); (গ) ERP-র বেশিরভাগ কর্মী AiPlatform-এ login-ই করে না।

**link লাগবে write tool-এ** (`create_invoice`) — তখন `CreatedBy`/`ApprovedBy` কে বসবে + ঐ ব্যক্তি কোন Company/Location দেখতে পাবে, দুটোই জানা লাগবে।

**পরিকল্পিত সমাধান: `AppUser.ErpUserId` (nullable long, FK constraint ছাড়া)** — নিজের user table-এ নিজের column, ERP schema অক্ষত। *ERP টেবিলে `AppUserId` বসানো হবে না।*

⏳ **এখন বানানো হয়নি — ইচ্ছাকৃত।** read tool-এ ("জুলাইয়ের বিক্রি") "কে করল" লাগে না। step 4 (write tool)-এ এক column + এক migration দিলেই হবে; এখন বানালে অব্যবহৃত পড়ে থাকত।

## নীতি (মনে রাখা)
- Ollama integration **আগে** শেষ হবে, তারপর এটা
- read tool আগে, write tool শেষে (data নিরাপত্তা)
- write action = সবসময় confirm (irreversible)
- ছোট step, কিছু ভাঙা যাবে না — Ollama-র মতোই
