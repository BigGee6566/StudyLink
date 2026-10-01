# 01 – Analysis: TutorConnect → StudyLink

Nova Tech Developers · IFS303 · Forte Books & Tuition Hub

## 1. Purpose

StudyLink (*Books, Tutoring & Academic Resource Hub*) must serve students, tutors, staff, suppliers, managers
and a system administrator across the Alice, East London and Bisho campuses. The tutoring core already existed
in the team's earlier system, **TutorConnect**. This analysis records what TutorConnect actually contains (read
from its source, now kept under `legacy\TutorConnect`), where the StudyLink brief and the source disagree,
and what had to be newly designed.

## 2. What TutorConnect is (verified from source)

* **A VB.NET ASP.NET Web Forms application** on .NET Framework 4.8.1: 30 `.aspx` pages, ADO.NET repositories
  and a service layer. It is **not** a desktop or WinForms application.
* **Roles:** Student, Tutor, Administrator, held in a single `Role` column on `Person`.
* **Security:** PBKDF2 password hashing, lockout after repeated failed sign-ins, a registration rate limiter,
  `IsActive` re-checked on every request, and a first-run setup page that creates the first administrator.
* **Tutoring rules (BR-01…BR-27):** subjects and levels, tutor availability slots, session requests that a tutor
  accepts into bookings, double-booking prevention (SERIALIZABLE transaction with `UPDLOCK, HOLDLOCK`),
  cancellation, postponement requests (one pending at a time, filtered unique index), completion and reviews
  (one per completed booking).
* **Notifications and an append-only audit log.**
* **Design language:** navy sidebar, top bar, cards, pill badges, Source Sans 3, breakpoints at 1023 and 767 px.

## 3. Where the brief and the source disagree

| # | Brief | Source | Resolution |
|---|---|---|---|
| A-1 | "Reuse TutorConnect's Payments" | No payments, fees, books, rentals, inventory or suppliers. The only money field is `Tutor.RatePerSession`, shown but never charged. | Fees, payments, refunds and receipts are **new** (D-4, D-6). |
| A-2 | Screens SC1–SC17 are "TutorConnect screens" | TutorConnect has no SC numbers. SC1–SC17 are the **Nova Tech Sprint 1** screen designs. | Sprint 1 §3.2 is the canonical list (below). The Sprint 1 System Matrix numbers screens differently and conflicts with its own UCD, so it was not used. |
| A-3 | ASP.NET Core MVC ("Controller + Razor view") | The team's projects, TutorConnect included, are Web Forms. | **D-1:** Web Forms in C#. Each "controller + view" becomes an `.aspx` page with a thin code-behind that calls a service. |
| A-4 | "Administrator" | One admin role | Split into **Staff**, **Manager** and **System Admin** as the brief's six roles require. |
| A-5 | Free-text `Location` | `Person.Location` text | Replaced by a `Campuses` table (Alice, East London, Bisho). |

### Canonical screen list (Sprint 1 §3.2)

SC1 Landing · SC2 Register · SC3 Role Selection · SC4 Registration Confirmation · SC5 Welcome Back (Login) ·
SC6 Student Dashboard · SC7 Book a Session · SC8 Date & Time Slot · SC9 Payment · SC10 Tutor Dashboard ·
SC11 Textbook Rentals · SC12 Book Details · SC13 Rental Confirmed · SC14 My Fees · SC15 Pay Fees ·
SC16 Booking Confirmed · SC17 Payment Complete.

New in StudyLink: SC18 Supplier Dashboard · SC19 Staff Dashboard · SC20 Manager Dashboard · SC21 Admin Dashboard ·
SC22 Tutor Verification · SC23 User Management · SC24 Reports · SC25 Book Management · SC26 Supplier Orders ·
SC27 Password Reset Requests · SC28 Audit Logs.

The Sprint 1 mockups are in `docs\reference\sprint1-mockups`.

## 4. Inventory: what is reused, reshaped or new

| TutorConnect component | Purpose | In StudyLink |
|---|---|---|
| `Person` + Role column | Account, hash, lockout | `Users` + `Roles` + `UserRoles` (a user can hold several roles), with `CampusId` |
| `Student`, `Tutor` (`IsApproved`) | Profiles | `Students`; `Tutors` with `VerificationStatus` (Pending / Verified / Rejected / Suspended), plus a `TutorVerifications` history |
| `Subject`, `StudyLevel`, `StudentSubject`, `TutorSubject` | Catalogue | Reused, with the same uniqueness rules (BR-05) |
| `TutorAvailability` | Slots | Reused, plus a campus |
| `SessionRequest` + `Booking` | Request → accept | Merged into `Bookings` with D-4's flow: AwaitingPayment → AwaitingTutor → Confirmed → Completed, or Cancelled / Declined / Expired |
| `TutoringSession` | Delivery record | Reused, with attendance |
| `PostponementRequest` | Student proposes, tutor decides | Ported 1:1 |
| `Review` | One per completed booking | Ported. Hidden reviews are also excluded from averages, fixing a legacy statistics bug. |
| `Notification`, `AuditLog` | Bell, trail | Reused, with new types and actions |
| Auth service, password helper, rate limiter, base page | Security | Ported to C# (**D-2**) |
| Validators (email, SA cell `^(0\d{9}\|\+27\d{9})$`, password rules) | Input rules | Ported |
| `Account/Setup.aspx` | First administrator | Ported (creates the first System Admin) |
| Seed runner (debug only, password from git-ignored config) | Demo data | Ported as `DemoSeeder`: one demo account per role, plus activity |
| Design CSS | Look and feel | Kept, recoloured to the StudyLink logo (navy `#0C256C`, blue `#0F6AF8`) |

**Gaps in TutorConnect that StudyLink fills:**
* There was no administrator password reset. StudyLink adds SC27: staff verify the person, then the system
  emails a single-use link.
* There was no cancellation cut-off. StudyLink adds the `CancellationCutoffHours` setting.
* A change-password method existed but no page used it. StudyLink adds `Account/ChangePassword`.

**Removed:** the request/accept screens (`RequestSession`, `MyRequests`, `StudentRequests`). D-4 replaces them:
paying reserves the slot and the tutor confirms or declines.

## 5. New domains (not in TutorConnect)

* **Books:** categories, titles, physical copies per campus, rentals, renewals, sales, fines and a waitlist.
  Stock is never stored as a number; it is counted from each copy's status.
* **Money:** a fees ledger (one row per billable item), card payments, allocations, refunds and receipts.
* **Supply:** supplier accounts, purchase orders, deliveries (stock enters only when hub staff receive it) and
  supplier invoices (stored outside the web root).
* **Operations:** tutor verification, timesheets and earnings, a support inbox, profile-change requests,
  password-reset requests, reports, backups and system settings.

## 6. Environment found

* Visual Studio 2022, MSBuild 17, SQL Server 2022 Express (`.\SQLEXPRESS`), IIS Express 10.
* The project folder is synchronised by OneDrive. Build output for the class libraries is redirected to
  `%LOCALAPPDATA%\StudyLinkBuild` (`Directory.Build.props`) because building inside OneDrive was very slow.
