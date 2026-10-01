# Testing Plan

StudyLink is tested at four levels. The automated suites live in `tests\StudyLink.Tests`; the manual levels are
`docs\UAT-Checklist.md` and `docs\DemoWalkthrough.md`.

| Level | What | How to run |
|---|---|---|
| Unit | Pure business rules (no database) | `dotnet test tests/StudyLink.Tests --filter "FullyQualifiedName~Unit"` |
| Integration | Services against a real SQL Server database built from the migrations | `dotnet test tests/StudyLink.Tests --filter "FullyQualifiedName~Integration"` |
| HTTP | The running site: sign-in per role, role access matrix, full link crawl | Start the site (F5), then `dotnet test tests/StudyLink.Tests --filter TestCategory=Http` |
| UAT / demo | People walking through each role's scenarios | `UAT-Checklist.md`, `DemoWalkthrough.md` |

## 1. Unit tests (`Unit\PolicyTests.cs`)

| Area | Tests |
|---|---|
| Scheduling | half-open overlap rules, different days never overlap, slot validation (future, minimum length, maximum length), fee = rate × length, cancellation cut-off, weeks start on Monday, utilisation |
| Rentals | due dates and overdue days, overdue fine = days × rate capped at the book price, renewal rules |
| Stock | stock derived from copy statuses (available, reserved, rented, overdue, low stock) |
| Money | card validation (Luhn, expiry, CVV, brand), payment allocations must equal the payment exactly |
| Security | PBKDF2 hashing round-trips and salts, password rules, the role–permission matrix, only Students/Tutors self-register and account creators are limited |

## 2. Integration tests (`Integration\`)

Each run builds a throwaway `StudyLink_Test` database from the same migrations as production, so the filtered
unique indexes, CHECK constraints and the `UPDLOCK/HOLDLOCK` paths are exercised for real. A `FakeClock` makes
time-based rules deterministic. The database is created on first use and dropped afterwards (set
`STUDYLINK_KEEP_TEST_DB=1` to keep it).

| Scenario | Rule proven |
|---|---|
| Six students reserve the same slot in parallel | Exactly one wins (no double booking) |
| Five students rent the last copy in parallel | Exactly one gets it |
| Student books two overlapping sessions | Refused; back-to-back sessions are fine |
| Unverified tutor publishes availability | Refused, and the tutor is not listed in search |
| Tutor publishes overlapping slots | Refused |
| Pay → tutor confirms | Booking moves AwaitingPayment → AwaitingTutor → Confirmed |
| Tutor declines a paid booking | Slot released and the fee refunded |
| Unpaid reservation passes its hold | The sweep expires it and frees the slot |
| Student cancels inside the cut-off | Refused; a tutor cancellation is allowed and refunds |
| Session completed | Attendance recorded, timesheet entry created, review possible |
| Postponement approved | Booking moves; the original time stays in history |
| Rent → pay → return late | Overdue fine created and payable; copy back in stock |
| Renewals | At most two, never while someone is on the waitlist |
| Checkout submitted twice | Charged once (idempotent token) |
| Declined card | Failure recorded; the fee stays unpaid |
| Supplier order → dispatch → receive | Stock rises only on receipt |
| Reports | Totals match the data created by the test |
| All six dashboards and every report | Build from live data. Their queries must translate to SQL; EF6 rejects some constructs only at run time. |
| Registration | Duplicate username rejected; a new tutor starts pending |
| Five bad passwords | Account locked, with the same message as a wrong password |
| Manager creates accounts | Only Staff, Supplier and Student |
| Password reset | Needs staff approval; the emailed token works once only |
| Last System Admin | Cannot be deactivated; a Student cannot call admin services |

## 3. HTTP tests (`Http\SiteTests.cs`)

These run against the running site with the demo data. They sign in through the real login form (ViewState
round-trip), so they cover all three access layers: web.config folder rules, the page guard and the services.

* Public pages load for visitors; every role area sends visitors to the login page.
* Each demo account lands on its own dashboard.
* **Role access matrix:** every role opens all of its pages (200), and is redirected away from a list of other
  roles' pages (to Login or Access Denied).
* **Crawl:** for each role, every in-app link reachable from its pages is followed (up to 120 pages), detail
  pages included. Any 5xx fails the test.
* A wrong password issues no auth cookie. A tampered postback (modified ViewState) is rejected, while an
  honest signed-in postback is accepted (postbacks are bound to the session).

## Bugs these tests caught

| Found by | Bug | Fix |
|---|---|---|
| Integration | Start-up crashed on the empty default backup-folder setting | `Value` allows empty strings |
| Integration | Three tables were linked to the wrong user column | Explicit EF configuration |
| Integration | Tutor search failed when loading ratings | Ratings aggregated in SQL |
| HTTP | Every request returned 500.19 (duplicate `App_Data` hidden segment) | Removed from `web.config` |
| HTTP | Logins and pages timed out (SQL Express `AUTO_CLOSE ON`) | Turned off at start-up and in the schema script |
| HTTP crawl | Manager dashboard failed (`DateTime.AddDays` inside an EF query) | Date computed before the query |
| Browser | Every signed-in postback failed MAC validation (SessionID changed until the session held data) | Session pinned before `ViewStateUserKey` is set |

## 4. Build checks

* `MSBuild StudyLink.sln` must report no errors or warnings.
* `aspnet_compiler -v / -p src\StudyLink.Web <out>` precompiles every page, master, user control and handler,
  catching markup and data-binding mistakes that MSBuild can't see.

## 5. Browser checks

The landing page, login and each role's dashboard are checked in a browser at desktop width and at 375 px
(phone). That covers the sidebar drawer, the bottom tab bar and the stacked tables.
