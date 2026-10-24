# Employee Self-Service API

ASP.NET Core 8 Web API (controllers, EF Core, SQL Server, JWT). Base route `/api/v1`. Use Swagger at `/swagger` (Development).

## Prerequisites
- .NET 8 SDK, SQL Server / SQL Express (or LocalDB)

## Setup
1. **Connection string** – edit `ConnectionStrings:DefaultConnection` in `appsettings.json` (or override with user-secrets).
2. **Secrets (never committed)** – from the project folder:
   ```
   dotnet user-secrets set "Jwt:Key" "<random string, 32+ characters>"
   dotnet user-secrets set "SmtpMail:Username" "you@gmail.com"
   dotnet user-secrets set "SmtpMail:Password" "<gmail app password>"
   dotnet user-secrets set "SmtpMail:SenderEmail" "you@gmail.com"
   ```
   Defaults for the other SMTP values (`smtp.gmail.com`, port 587, STARTTLS) are in `appsettings.json`. Port 465 uses implicit TLS automatically.
3. **Database** – the app applies migrations at startup. To do it manually: `dotnet ef database update`.
   The migration history was reset to a single `InitialCreate`; **drop any old `EmployeeSelfServiceDb` database first.**
4. **Run** – `dotnet run`, then open `https://localhost:<port>/swagger`. Click *Authorize* and paste the JWT from `POST /api/v1/auth/login`.

## Sample accounts (Development only, `Seed:Enabled`)
Password: `Password123!` (or `Seed:DefaultPassword`). Admin email: `Seed:AdminEmail` (default `admin@example.com`; set it to a real mailbox to receive mail).

| Email | Roles |
|---|---|
| admin@example.com | Admin |
| manager@example.com | Manager (reports for staff1/staff2) |
| hr1@example.com, hr2@example.com | HR |
| finance1@example.com, finance2@example.com | Finance |
| staff1@example.com, staff2@example.com | Staff |

Every active employee is implicitly Staff. Seed also loads estacode rates (GB, US, AE, ZA × grades G1–G5) and one open KPI review period.

## Testing email delivery
1. Log in as admin, `POST /api/v1/employees` with a **real email address** you can read.
2. `GET /api/v1/email-queue` shows the row (`Pending` → `Sent`, with attempt count / last error). In Development the worker polls every 20 s; the default elsewhere is every 2 minutes (`EmailWorker:*`).
3. The email contains the account-setup token; use it in `POST /api/v1/auth/activate`.

## KPI workflow
HR creates a Draft plan (`POST /kpi/plans`, never for themselves), edits targets/weights while Draft, then activates it (`POST /kpi/plans/{id}/activate`; weights must total exactly 100.00; reviewer defaults to the employee's reporting manager). The employee records actuals and evidence comments (`PUT /kpi/plans/{id}/progress`) while Active/Returned until the end of the deadline day (Africa/Lagos), then submits. The assigned reviewer approves (finalizes; the server calculates the score) or returns with a comment. HR can reopen an Active/Returned plan with a reason and a future deadline; the original deadline and each reopening are kept.

## Postman and tests
- Import `EmployeeSelfService.postman_collection.json`, set `baseUrl`, run folder **0 - Sign in** first (stores a JWT per seeded role), then follow folders 1-7 (workflows and the rejected actions from the brief). File-upload requests need a file chosen in Postman.
- Automated tests: `dotnet test EmployeeServiceApi.Tests` (project `EmployeeServiceApi.Tests`, xunit + EF Core InMemory). They cover KPI calculations and workflow/access rules and opportunity duplicate/closing/eligibility/status rules.

## Business-rule assumptions
- Errors use `{ responseCode, responseMessage, errors:[{field,message}] }`; lists use `{ data, pagination }`.
- Record exists but isn't yours → 403; doesn't exist → 404; duplicates / invalid transitions → 409; validation → 400.
- Login lockout: 5 failures in 15 min locks for 15 min. JWT lifetime 60 min; account status is re-checked on every request.
- Reminders run hourly and are de-duplicated per recipient/item/type/Africa-Lagos date.

## Email delivery guarantee
Delivery is **at-least-once, not exactly-once**: if the process crashes after SMTP accepts a message but before the row is marked `Sent`, the message can be sent again. Rows are claimed with row locks so overlapping runs do not double-send; failures retry at 5, 15 and 60 minutes, then become `Failed`.
