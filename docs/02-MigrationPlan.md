# 02 – Migration Plan and Design

How TutorConnect's proven rules were carried into StudyLink, and how StudyLink is put together.

## 1. Decisions

| # | Decision | Why |
|---|---|---|
| D-1 | ASP.NET Web Forms, C#, .NET Framework 4.8.1 | The team's existing projects and tooling. The brief's MVC "controller + view" maps to an `.aspx` page with a thin code-behind calling a service. |
| D-2 | Port TutorConnect's own authentication (PBKDF2-SHA256 at 100k iterations, lockout, rate limiting, Forms ticket) with Users / Roles / UserRoles | Proven code. No OWIN or Identity packages. |
| D-3 | EF6 Code First + migrations | One model drives both the database and `database\01_CreateSchema.sql`. |
| D-4 | Pay first, then the tutor confirms; a decline or a tutor cancellation refunds in full | The slot is held only for students who pay; the tutor still decides. |
| D-5 | Legacy source kept under `legacy\TutorConnect` | Reference and traceability. |
| D-6 | `IPaymentGateway` with a simulated card gateway; store only brand and last four | No real card data in a student project. A real provider is one new class. |

## 2. Architecture

```
Browser ──► StudyLink.Web (Web Forms)
              ├─ web.config <location> rules per role folder      ← access layer 1
              ├─ BasePage [RequirePermission] / [RequireSignIn]    ← access layer 2
              ├─ pages: thin code-behind, no business rules
              ▼
            StudyLink.Services (C#)
              ├─ services Demand(permission) + ownership checks    ← access layer 3
              ├─ policies (pure, unit-tested): scheduling, rentals, money, cards
              ▼
            StudyLink.Data (EF6) ──► SQL Server "StudyLink"
              └─ integrity backstops: filtered unique indexes + CHECK constraints
```

* **Composition root:** `StudyLinkServices` builds every service around one `DbContext` per HTTP request
  (`AppServices.Current`, disposed in `Application_EndRequest`). Tests construct the same class directly.
* **Single source of truth for access:** the `RolePermissions` matrix (`Services\Security\Permissions.cs`)
  drives the page guards, the navigation menus and the services' `Demand` checks.
* **Session security:** the Forms ticket carries only the user id. Roles and the active flag are re-read from
  the database on every request, so deactivation and role changes apply immediately (BR-25). `ViewStateUserKey`
  binds every postback to the signed-in session (CSRF). Security headers and a CSP are set in `web.config`.
* **Background sweep:** an hourly, idempotent `HostingEnvironment` timer. It expires unpaid holds, sends due-soon
  and overdue notices, raises low-stock alerts, refreshes tutor statistics and records the daily metrics snapshot.

## 3. Database (EF6 → SQL Server)

* **People and security:** Users, Roles, UserRoles, Campuses, Students, Tutors, TutorVerifications, Staff, Suppliers,
  Managers, Admins, PasswordResetRequests, PasswordResetTokens (hashed, single use, time limited),
  ProfileUpdateRequests, SupportMessages, SystemSettings.
* **Tutoring:** Subjects, StudyLevels, StudentSubjects, TutorSubjects, TutorAvailability, Bookings, TutoringSessions,
  PostponementRequests, Reviews, Timesheets, TimesheetEntries.
* **Books:** Categories, Books, BookCopies, Rentals, RentalRenewals, Sales, Fines, BookWaitlist.
* **Money:** Fees (the My Fees ledger, with exactly one source per fee enforced by CHECK), Payments,
  PaymentAllocations (which must sum exactly to the payment).
* **Supply:** SupplierOrders, SupplierOrderLines, Deliveries, DeliveryLines, SupplierInvoices.
* **Operations:** Notifications, AuditLogs, ReportRuns, DailyMetrics, TutorStats, BackupLogs.
* **Integrity backstops** (`Migrations\IntegrityConstraints.cs`):
  * one active booking per slot (`UX_Bookings_ActiveSlot`) and one active rental per copy;
  * one pending postponement per booking;
  * a unique checkout token per payment (no double charge);
  * CHECKs on statuses, times, ratings and amounts.

## 4. Screens → pages

| ID | Screen | Page |
|---|---|---|
| SC1 | Landing | `Default.aspx` |
| SC2–SC4 | Register, role selection, confirmation | `Account/Register.aspx` (wizard) |
| SC5 | Welcome back (login) | `Account/Login.aspx` (+ ForgotPassword, ResetPassword, ChangePassword, Setup) |
| SC6 | Student dashboard | `Student/Dashboard.aspx` |
| SC7 | Book a session (find a tutor) | `Catalogue/Tutors.aspx`, `Catalogue/TutorProfile.aspx` |
| SC8 | Date & time slot | `Student/Tutoring/SelectSlot.aspx` |
| SC9 | Payment | `Student/Payments/Checkout.aspx` |
| SC10 | Tutor dashboard | `Tutor/Dashboard.aspx` (+ Schedule, SessionDetails, Availability, Subjects, Timesheet, Earnings, Reviews) |
| SC11 | Textbook rentals | `Catalogue/Books.aspx` |
| SC12 | Book details | `Catalogue/BookDetails.aspx` |
| SC13 | Rental confirmed | `Student/Books/RentalConfirmed.aspx` (+ `Student/Books/MyRentals.aspx`) |
| SC14 | My fees | `Student/Fees/MyFees.aspx` |
| SC15 | Pay fees | `Student/Fees/PayFees.aspx` |
| SC16 | Booking confirmed | `Student/Tutoring/BookingConfirmed.aspx` (+ MyBookings, BookingDetails) |
| SC17 | Payment complete / receipt | `Student/Fees/PaymentComplete.aspx` |
| SC18 | Supplier dashboard | `Supplier/Dashboard.aspx` (+ Orders, OrderDetails, Deliveries, Invoices, Stock) |
| SC19 | Staff dashboard | `Staff/Dashboard.aspx` |
| SC20 | Manager dashboard | `Manager/Dashboard.aspx` |
| SC21 | Admin dashboard | `Admin/Dashboard.aspx` (+ Roles, Settings, Security, Backups) |
| SC22 | Tutor verification | `Operations/TutorVerification.aspx` |
| SC23 | User management | `Operations/Users.aspx` |
| SC24 | Reports | `Operations/Reports.aspx` (CSV export, print) |
| SC25 | Book management | `Manager/Books.aspx` (`?view=inventory`), `Manager/BookEdit.aspx`, `Manager/Categories.aspx` |
| SC26 | Supplier orders | `Manager/SupplierOrders.aspx` (orders, new order, invoices) ↔ `Supplier/OrderDetails.aspx` |
| SC27 | Password reset requests | `Operations/PasswordResets.aspx` |
| SC28 | Audit logs | `Admin/AuditLog.aspx` |

Shared pages: `Account/Profile.aspx`, `Notifications/Index.aspx`, `Public/HowItWorks.aspx`, `Public/Contact.aspx`,
`Public/Privacy.aspx`. Operations queues: Returns, Fines, Support (with profile-change requests), Timesheets,
Deliveries, Subjects, Reviews. Handlers: `Handlers/BookCover.ashx` and `Handlers/InvoiceFile.ashx` (authorised,
streamed from `App_Data`).

## 5. Navigation (brief §8)

`Infrastructure\NavigationMap.cs` holds each role's menu exactly as the brief lists it, plus a "More" section for
the extra operational screens, and the four-item mobile tab bar from the Sprint 1 mockups. Menus only hide links;
pages and services enforce access themselves.

| Role | Menu |
|---|---|
| Student | Home · Books · Tutoring · My Bookings · My Rentals · Fees & Payments · Profile |
| Tutor | Home · Schedule · Availability · Sessions · Attendance · Timesheets · Earnings · Profile |
| Staff | Home · Tutor Verification · Students · Returns · Fines · Support · Profile |
| Supplier | Home · Orders · Deliveries · Invoices · Stock · Profile |
| Manager | Home · Books · Inventory · Payments · Rentals · Reports · Staff · Suppliers · Profile |
| System Admin | Home · Users · Roles · System Settings · Audit Logs · Security · Backups · Profile |

## 6. Business rules (all enforced in the services)

* **Accounts:** only Students and Tutors self-register. Tutors start as *Pending verification*: they can sign in
  and complete their profile, but cannot publish slots or appear in search until Staff or a Manager verifies them.
  Managers create Staff, Supplier and Student accounts; the System Admin creates any type and assigns roles.
  Created users receive a single-use link to set their own password. The last active System Admin cannot be
  deactivated.
* **Booking (BR-10/12/14 + D-4):** the slot must be open and in the future, and the tutor verified and active.
  Neither the tutor nor the student may already have an overlapping session. Reservation runs in a SERIALIZABLE
  transaction with `UPDLOCK, HOLDLOCK`, with the filtered unique index as a backstop. Unpaid holds expire after
  `BookingHoldMinutes`. The fee is the hourly rate × duration. Students cancel only outside the
  `CancellationCutoffHours` window. A tutor decline or cancellation refunds in full.
* **Rentals:** a copy is claimed with `UPDLOCK, READPAST` at the chosen campus (Available → Reserved → Rented).
  The due date is the rental date plus `RentalPeriodDays`. A rental can be renewed if it isn't overdue, has
  renewals left (`MaxRenewals`) and nobody is waiting for the title at that campus. Students with an overdue book
  cannot rent another.
* **Returns and fines:** the overdue fine is days late × the title's late fee, capped at its sale price. Staff can
  add a damage fine or mark a copy lost (replacement fine), and can waive a fine with a reason. When a copy comes
  back, the waitlist is notified.
* **Payments:** one card payment can settle several fees; allocations must equal the payment exactly. A checkout
  token makes double submits idempotent. Every payment gets an `SL-PAY-…` receipt, emailed and printable.
  Declines are recorded without settling anything.
* **Tutors:** timesheets are filled automatically from attended sessions, then the tutor submits and Staff or a
  Manager approves. Earnings are completed session fees less `TutorCommissionPercent`.
* **Supply:** a delivery adds stock only when hub staff receive it, line by line. Invoices must be real PDFs of
  5 MB or less and move Submitted → Approved/Rejected → Paid.
* **Password resets:** request, then staff verification, then an emailed single-use link that expires after
  `ResetTokenMinutes`. No temporary password is ever shown.
* **Audit:** every state change above is written to the append-only audit log in the same transaction.

## 7. Implementation slices (as delivered)

1. **Setup:** folder reorganisation, legacy import, mockups, branding assets, solution.
2. **Data:** entities, configurations, `InitialCreate` migration, integrity constraints, SQL scripts, lookup seed.
3. **Services:** accounts, tutoring, money, books, supply, reporting, maintenance, plus unit and integration tests.
4. **Web shell:** config, masters, auth, base page, navigation, design system, landing and account pages.
5. **Role pages:** catalogue, student, tutor, staff, operations, supplier, manager, admin, notifications, handlers.
6. **Verification:** HTTP tests (role matrix and crawl), browser checks, documentation.
