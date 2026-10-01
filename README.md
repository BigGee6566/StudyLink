# StudyLink – Books, Tutoring & Academic Resource Hub

*Bringing students and tutors together.*

StudyLink is the web system for **Forte Books & Tuition Hub** (campuses: Alice, East London, Bisho), built by
**Nova Tech Developers** (UFH IFS303). It grows the team's earlier tutoring system, TutorConnect, into a six-role
platform for tutoring, textbook rentals and sales, fees and card payments, suppliers, operations and reporting.

| | |
|---|---|
| Platform | ASP.NET Web Forms, C# 7.3, .NET Framework 4.8.1 (Visual Studio 2022) |
| Data | SQL Server 2022 (Express is fine) via Entity Framework 6.5 Code First + migrations |
| UI | Bootstrap 5.3 grid + the StudyLink design system (`Content/css/studylink.css`) |
| Roles | Student, Tutor, Staff, Supplier, Manager, System Admin |

## Folder structure

```
StudyLink\
├─ StudyLink.sln
├─ src\
│  ├─ StudyLink.Web\        Web Forms site: pages per role folder, master pages, handlers, design system
│  ├─ StudyLink.Services\   Business rules (services, policies, validation, security, DTOs)
│  └─ StudyLink.Data\       EF6 entities, configurations, migrations, lookup seed
├─ tests\StudyLink.Tests\   Unit, SQL-backed integration and HTTP (role access + crawl) tests
├─ database\                01_CreateSchema.sql (generated from the migrations), 02_SeedLookups.sql
├─ docs\                    Analysis, migration plan, testing plan, UAT checklist, demo walkthrough, mockups
├─ branding\                Source logo sheet and the script that cuts the logo, favicons and icons
├─ tools\                   sync_web_project.py (designer files + project items for the Web Forms project)
└─ legacy\TutorConnect\     The original VB.NET TutorConnect source, untouched (reference only)
```

## Run it (Visual Studio 2022)

1. **Prerequisites:** Visual Studio 2022 with the *ASP.NET and web development* workload, and SQL Server
   (Express) reachable as `.\SQLEXPRESS`. To use another server, change the `StudyLink` connection string in
   `src\StudyLink.Web\Web.config`.
2. **Local settings:** copy `src\StudyLink.Web\App_Data\local.settings.config.sample` to
   `local.settings.config` in the same folder, and choose your own `Seed:DevPassword`. This file is git-ignored.
   Keep `Seed:Demo` set to `true` if you want demo data.
3. Open `StudyLink.sln`, set **StudyLink.Web** as the startup project and press **F5** (IIS Express).
   * On first start the site creates or updates the `StudyLink` database from the EF migrations, seeds the
     lookups (campuses, roles, subjects, levels, categories, settings) and, in Debug builds with
     `Seed:Demo=true`, creates the demo accounts and sample activity. The first request takes a little while.
   * If you prefer to create the database by hand, run `database\01_CreateSchema.sql` and then
     `database\02_SeedLookups.sql` in SSMS, and set `StudyLink:MigrateOnStart` to `false` in `Web.config`.
4. Without demo data, the first visit goes to **Account/Setup** to create the first System Admin.

### Demo accounts

All demo accounts use the password you set in `Seed:DevPassword`.

| Username | Role | Notes |
|---|---|---|
| `admin` | System Admin | Users, roles, settings, security, backups, audit log |
| `manager` | Manager | Books and inventory, payments, supplier orders, reports, staff |
| `staff` | Staff | Tutor verification, returns, fines, support, password resets |
| `supplier` | Supplier | *Academic Books Direct (demo)*: orders, deliveries, invoices |
| `tutor.dlamini`, `tutor.khumalo` | Tutor | Verified, with published slots |
| `tutor.pending` | Tutor | Pending verification, for the SC22 demo |
| `student.thando`, `student.olona`, `student.aphiwe` | Student | Bookings, rentals (Olona's is overdue), a purchase |

### Test cards (simulated gateway)

Payments go through a simulated card gateway (`IPaymentGateway`). Only the card brand and last four digits are
stored.

* `4111 1111 1111 1111` with any future expiry and a 3-digit CVV is **approved**.
* `4000 0000 0000 0002` is **declined**.

Emails (receipts, reset links, set-password links) are written as `.eml` files to `src\StudyLink.Web\App_Data\MailDrop`
in development. Set `StudyLink:MailMode` to `Smtp` and configure `<mailSettings>` to send real mail.

## Tests

```bash
dotnet test tests/StudyLink.Tests/StudyLink.Tests.csproj
```

* **Unit** (`Unit\`): scheduling and overlap rules, due dates and fines, stock maths, card validation, the
  role–permission matrix, password hashing.
* **Integration** (`Integration\`): run against a throw-away `StudyLink_Test` database on `.\SQLEXPRESS`, built
  from the migrations. They include parallel races (six students booking one slot, five renting the last copy),
  refunds on decline, returns with fines, supplier deliveries entering stock, password resets and reports.
* **HTTP** (`Http\`, category `Http`): start the site first (F5). The tests then sign in as every demo account
  through the real login form and check three things: each role reaches all its pages, each role is kept out of
  other roles' pages, and crawling every link finds no server errors. They are skipped when the site isn't running.
  `STUDYLINK_BASEURL` overrides the address.

## Working on the Web project

`StudyLink.Web` is a classic (non-SDK) Web Application project, so after adding, renaming or changing the
markup of a page, run:

```bash
python tools/sync_web_project.py
```

It regenerates the `.designer.cs` files and the project's item list, the work Visual Studio's designer
normally does.

## Troubleshooting

* **Errors mention "Connection Timeout Expired … post-login phase":** SQL Express creates databases with
  `AUTO_CLOSE ON`, so the database shuts down between requests and reopening it is slow on a busy PC. The site
  turns it off at start-up, and `01_CreateSchema.sql` does the same. To fix by hand:
  `ALTER DATABASE StudyLink SET AUTO_CLOSE OFF;`
* **The error page shows a reference code:** the full error for that reference is in `src\StudyLink.Web\App_Data\Logs\errors-<date>.log`.
* **A page compiles in Visual Studio but fails at runtime:** precompile all markup to find the error:
  `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe -v / -p src\StudyLink.Web %TEMP%\slpc`
  (the output path must be short).

## Key decisions

| # | Decision |
|---|---|
| D-1 | ASP.NET Web Forms in C# on .NET 4.8.1. Each brief "controller + view" is an `.aspx` page with a thin code-behind calling a service. |
| D-2 | TutorConnect's own authentication, ported: PBKDF2-SHA256 (100k iterations), lockout after repeated failures, Users/Roles/UserRoles, Forms ticket. |
| D-3 | EF6 Code First with migrations. Integrity backstops (filtered unique indexes, CHECKs) are part of the migration. |
| D-4 | Booking flow: pay to reserve the slot, then the tutor confirms. A decline refunds in full. |
| D-5 | The original TutorConnect source is kept unchanged under `legacy\TutorConnect`. |
| D-6 | Simulated card gateway behind `IPaymentGateway`. Only the brand and last four digits are stored. |

See `docs\01-Analysis.md` and `docs\02-MigrationPlan.md` for the full analysis and design.
