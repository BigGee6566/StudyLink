# User Acceptance Test Checklist

Run against a fresh demo database (see README). Sign in with the demo usernames; all use your
`Seed:DevPassword`. Tick each line when the expected result is seen. Approved test card: `4111 1111 1111 1111`,
any future expiry, any 3-digit CVV. Declined card: `4000 0000 0000 0002`.

## Visitor (not signed in)

- [ ] SC1 landing shows the logo, tagline, campus selector, Log in, Register as Student, Register as Tutor, and the Browse Books / Find a Tutor / How It Works / Contact links.
- [ ] Choosing **East London** and *Browse books at this campus* shows stock counted for East London.
- [ ] Browse Books and Find a Tutor work without signing in; *Log in to rent or buy* appears on a book.
- [ ] Contact / Support sends a message (it appears in Staff → Support).
- [ ] Opening `/Student/Dashboard.aspx` directly redirects to the login page.

## Student (`student.thando`)

- [ ] SC2–SC4: register a new student (wizard, then confirmation, then login).
- [ ] SC5: login lands on the **Student dashboard** (SC6) with a greeting, reminder and outstanding balance.
- [ ] SC7: Tutoring lists only **verified** tutors (`tutor.pending` is not shown); filters by subject, level and campus work.
- [ ] SC8: choosing a tutor shows open dates and times; picking a slot fills the summary (time, fee, campus).
- [ ] SC9: Continue holds the slot and opens Payment with the hold time; paying with the approved card succeeds.
- [ ] SC16: Booking confirmed shows *Awaiting tutor*, with a receipt link.
- [ ] Paying with the declined card shows the decline, and the fee stays unpaid under My Fees.
- [ ] Two browsers trying to book the same slot: the second gets *"Someone has just booked this time slot…"*.
- [ ] SC11/SC12: Books show live stock; book details show the rental fee, period, due date and late fee.
- [ ] Rent → SC9 → SC13 Rental confirmed (due date, collect-at campus).
- [ ] Buy a book → purchase confirmed.
- [ ] *Calculus* at a campus with no copies: **Join the waitlist** appears; joining shows in My Rentals → Waitlist.
- [ ] My Rentals: renew an active rental (fee added to My Fees); renewal is refused when overdue.
- [ ] SC14 My Fees: balance, unpaid items with "pay by" times, payments with receipts, fee history.
- [ ] SC15 Pay Fees: select several items and pay once; SC17 shows a receipt with every line; Print works.
- [ ] Booking details: cancel (outside the cut-off) refunds; postponement request sent; review after completion.
- [ ] Profile: update contact details; request a name change (goes to Staff).
- [ ] Notifications bell shows new items; *Mark all as read* clears the count.
- [ ] Opening `/Admin/Dashboard.aspx` or `/Operations/Users.aspx` is refused.

## Tutor (`tutor.dlamini`, plus `tutor.pending`)

- [ ] SC10 dashboard: sessions today, utilisation %, items awaiting response, earnings this month, timesheet week.
- [ ] Sessions (requests): confirm a paid booking; the student is notified.
- [ ] Decline a paid booking with a reason; the student is refunded (visible in their receipts).
- [ ] Approve or decline a postponement request.
- [ ] Attendance: complete a past session as Attended; it appears on the timesheet.
- [ ] Availability: publish a slot (optionally weekly); overlapping or past slots are refused; remove an open slot.
- [ ] Subjects: add and remove subjects (subjects with bookings cannot be removed).
- [ ] Timesheet: add other work, then submit; it locks until reviewed.
- [ ] Earnings: sessions, hours, fees, commission and net for a date range.
- [ ] `tutor.pending` sees the *Pending verification* banner and cannot publish availability.

## Staff (`staff`)

- [ ] SC19 dashboard: KPIs, tutors awaiting verification, overdue books, deliveries to receive.
- [ ] SC22: review `tutor.pending`, tick both checks and approve; the tutor can now publish slots and appears in search.
- [ ] SC23 (Students): capture a new student; the new user gets a set-password email (see `App_Data\MailDrop`).
- [ ] Returns: scan Olona's overdue copy barcode; receive it; an overdue fine is raised automatically.
- [ ] Mark a copy as lost; a replacement fine is raised.
- [ ] Fines: waive a fine with a reason.
- [ ] Support: respond to the demo message; approve or reject a profile change request.
- [ ] SC27: approve Aphiwe's reset request; an email with a single-use link appears in MailDrop; the link works once.
- [ ] Timesheets: approve a submitted timesheet, and send one back with a note.
- [ ] Deliveries: receive a supplier delivery; stock rises at the line's campus.
- [ ] SC24 Reports: Staff see the operational reports only; CSV download works.
- [ ] Opening `/Manager/Payments.aspx` or `/Admin/AuditLog.aspx` is refused.

## Supplier (`supplier`)

- [ ] SC18 dashboard: open orders, recent invoices, low-stock titles they supply.
- [ ] Orders → order: acknowledge, then dispatch part of the quantities with a note.
- [ ] Upload a PDF invoice (a non-PDF or an over-5 MB file is refused).
- [ ] Deliveries and Invoices show the hub's receipt and the review status.
- [ ] Stock shows hub stock for their titles.

## Manager (`manager`)

- [ ] SC20 dashboard: revenue trend, KPIs, low stock, recent payments.
- [ ] SC25: add a book (with cover), add copies at a campus, edit a copy's status; Inventory view shows stock by status.
- [ ] Categories: add, rename, retire.
- [ ] Payments: filter by date, type and status; open a payment and refund one item with a reason.
- [ ] SC26: place a purchase order, open it, cancel an unacknowledged order; approve, reject and mark invoices paid.
- [ ] Staff (SC23): create a Staff account and a Supplier account; cannot create a System Admin.
- [ ] SC24: all reports including revenue and tutor earnings; date ranges apply; CSV export.
- [ ] Subjects and Reviews moderation (hide a review: it disappears from the tutor's profile average).

## System Admin (`admin`)

- [ ] SC21 dashboard: accounts, roles, recent audit activity, database size, last backup, locked accounts.
- [ ] Users (SC23): create any account type, assign or remove roles, deactivate and reactivate; you cannot deactivate yourself.
- [ ] Roles: each role's permissions and member counts.
- [ ] System Settings: change the rental period; an invalid value is refused; the change is audited.
- [ ] Security: lock an account with 5 bad passwords, then unlock it here.
- [ ] Backups: *Back up now* creates a backup and logs it.
- [ ] SC28 Audit Logs: filter by action, record type, user and date; entries can't be edited.
- [ ] Opening `/Operations/Returns.aspx` or `/Manager/Payments.aspx` is refused.

## Cross-cutting

- [ ] Every page works at 375 px wide (menu drawer, bottom tab bar, stacked tables).
- [ ] Each role's sidebar menu matches the brief §8.
- [ ] Deactivating a signed-in user takes effect on their next click.
- [ ] Error pages: an unknown URL shows the friendly 404 page.
