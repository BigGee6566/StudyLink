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
StudyLink\                  The Web Forms site (StudyLink.Web.csproj) lives directly in this folder
├─ StudyLink.sln            Open this in Visual Studio 2022
├─ StudyLink.Web.csproj, Web.config, Global.asax, Default.aspx
├─ Account\ Public\ Catalogue\ Student\ Tutor\ Staff\ Operations\ Supplier\ Manager\ Admin\ Notifications\
│                           Pages, one folder per role
├─ MasterPages\ UserControls\ Handlers\ ErrorPages\ Infrastructure\
├─ Content\ Scripts\ Images\ App_Data\
├─ Services\                StudyLink.Services: business rules (services, policies, validation, security, DTOs)
├─ Data\                    StudyLink.Data: EF6 entities, configurations, migrations, lookup seed
├─ Tests\                   StudyLink.Tests: unit, SQL-backed integration and HTTP (role access + crawl) tests
├─ database\                StudyLink.sql: creates the database, tables and lookup data (generated)
├─ docs\                    Analysis, migration plan, testing plan, UAT checklist, demo walkthrough, mockups
├─ branding\                Source logo sheet and the script that cuts the logo, favicons and icons
├─ tools\                   sync_web_project.py (designer files + project items), precompile.ps1, build_sql.ps1
└─ legacy\TutorConnect\     The original VB.NET TutorConnect source, untouched (reference only)
```

The folders that are not part of the site (`Services`, `Data`, `Tests`, `legacy`, `docs`, `database`, `tools`)
are hidden segments in `Web.config`, so IIS Express never serves them.

## Run it (Visual Studio 2022)

1. **Prerequisites:** Visual Studio 2022 with the *ASP.NET and web development* workload, and SQL Server
   (Express) reachable as `.\SQLEXPRESS`. To use another server, change the `StudyLink` connection string in
   `Web.config`.
2. **Local settings:** copy `App_Data\local.settings.config.sample` to
   `local.settings.config` in the same folder, and choose your own `Seed:DevPassword`. This file is git-ignored.
   Keep `Seed:Demo` set to `true` if you want demo data.
3. Open `StudyLink.sln`, set **StudyLink.Web** as the startup project and press **F5** (IIS Express).
   * On first start the site creates or updates the `StudyLink` database from the EF migrations, seeds the
     lookups (campuses, roles, subjects, levels, categories, settings) and, in Debug builds with
     `Seed:Demo=true`, creates the demo accounts and sample activity. The first request takes a little while.
   * To create the database yourself, run `database\StudyLink.sql` in SSMS (or
     `sqlcmd -S .\SQLEXPRESS -E -i database\StudyLink.sql`) and point the `StudyLink` connection string in
     `Web.config` at that server. The header of the script walks through both steps. After adding a migration,
     regenerate it with `powershell -File tools\build_sql.ps1`.
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

Emails (receipts, reset links, set-password links) are written as `.eml` files to `App_Data\MailDrop`
in development. Set `StudyLink:MailMode` to `Smtp` and configure `<mailSettings>` to send real mail.

## Tests

```bash
dotnet test Tests/StudyLink.Tests.csproj
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
  turns it off at start-up, and `database\StudyLink.sql` does the same. To fix by hand:
  `ALTER DATABASE StudyLink SET AUTO_CLOSE OFF;`
* **The error page shows a reference code:** the full error for that reference is in `App_Data\Logs\errors-<date>.log`.
* **A page compiles in Visual Studio but fails at runtime:** build, then precompile all markup to find the error:
  `powershell -File tools\precompile.ps1`. It copies just the site (without `legacy`, `Data`, `Tests`, …) to
  `%TEMP%` and runs `aspnet_compiler` there, because `legacy\TutorConnect` has its own application-level `Web.config`.

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
