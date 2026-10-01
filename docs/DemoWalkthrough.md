# Demo Walkthrough – a day at Forte Books & Tuition Hub

A 15–20 minute script that shows all six roles working together on the demo data. Use two browser windows
(one private) so two people can be signed in at once. Every account uses your `Seed:DevPassword`.

**What the demo seed has already done**
* Tutors **Sipho Dlamini** (Alice) and **Ayanda Khumalo** (East London) are verified and have published 10 days of slots.
* **Lerato Mahlangu** (`tutor.pending`) has applied and is waiting for verification.
* **Thando** has a confirmed session with Sipho, an unpaid reservation and an active rental. **Olona** has a session
  waiting for Sipho and an **overdue** rental. **Aphiwe** bought a book and has asked for a password reset.
* There is an open purchase order with **Academic Books Direct (demo)** and a support message from a Bisho student.
  *Calculus* has only one copy, for the low-stock and waitlist demo.

---

## 1. Visitor → Student (SC1 → SC5 → SC6)

1. Open the site. Show the landing page: logo, tagline, campus selector, Log in, and Register as Student / Tutor.
2. Pick **Alice**, then *Browse books at this campus*: live stock badges per title (SC11).
3. Log in as `student.thando`. You land on the **Student dashboard** (SC6): greeting, balance card, upcoming
   session and the book on loan.

## 2. Student books a tutor (SC7 → SC8 → SC9 → SC16)

1. **Tutoring:** only verified tutors appear. Filter by *Mathematics*.
2. **Book** Sipho → SC8: choose a date pill, then a time. The summary shows time, campus, duration and fee.
3. **Continue to payment** → SC9: the slot is now held (see the hold time). Pay with `4111 1111 1111 1111`.
4. SC16 **Booking confirmed** → status *Awaiting tutor*. Open the receipt (SC17).
5. *(Optional)* In the second window, sign in as `student.olona` and try the same slot: it is gone (no double booking).

## 3. Tutor confirms and teaches (SC10)

1. Second window: sign in as `tutor.dlamini` → **Tutor dashboard** (SC10): sessions today, utilisation,
   *Awaiting your response*.
2. **Sessions:** confirm Thando's new booking. Decline Olona's with a reason; Olona is refunded automatically
   (show it later under her My Fees receipts).
3. **Attendance:** nothing is past yet. Point out that completing a session adds it to the **Timesheet**.
4. **Availability:** publish a new slot for next week, repeated for 4 weeks.
5. **Earnings:** last week's demo session, the commission and the net amount.

## 4. Student rents a book and pays fees (SC11 → SC12 → SC13, SC14 → SC15 → SC17)

1. As Thando: **Books** → *Principles of Information Systems* → SC12 shows the rental fee, rental period, due date
   and late fee per day.
2. **Rent this book** → SC9 → pay → SC13 **Rental confirmed** (collect at Alice, due date).
3. **Fees & Payments** (SC14): the unpaid reservation from the seed is listed with its pay-by time.
4. **Pay fees** (SC15): select it, pay once → SC17 receipt. *Print* gives a clean printed receipt.

## 5. Staff runs the counter (SC19, SC22, SC27, returns)

1. Sign in as `staff` → **Staff dashboard** (SC19).
2. **Tutor Verification** (SC22): open Lerato, tick *Identity* and *Qualification*, approve. She can now publish
   slots and appears in search.
3. **Returns:** open the overdue list → Olona's rental → *Receive book*. The overdue fine is calculated
   automatically (days × late fee) and appears on Olona's My Fees.
4. **Password Resets** (SC27): verify Aphiwe's request and approve. A single-use reset link is emailed
   (open the `.eml` in `App_Data\MailDrop`). No password is ever shown.
5. **Support:** reply to the Bisho student's question and resolve it.

## 6. Supplier delivers stock (SC18 ↔ SC26)

1. Sign in as `supplier` → **Supplier dashboard** (SC18): the open order and their low-stock titles.
2. Open the order → **Acknowledge**, then **Dispatch** (e.g. 4 of the 6 *Calculus* copies) with a waybill note.
3. Upload a PDF invoice for the order.
4. Back as `staff` → **Deliveries**: receive the delivery. Stock rises only now. Show *Calculus* availability
   jump in Books.

## 7. Manager oversees the business (SC20, SC24, SC25, SC26)

1. Sign in as `manager` → **Manager dashboard** (SC20): revenue trend, KPIs, low stock, recent payments.
2. **Books → Inventory** (SC25): stock per title by status (available, on loan, overdue, sold, lost).
   Open a title and add 2 copies at Bisho.
3. **Suppliers** (SC26): the order now shows *Partially delivered*. On the **Invoices** tab, approve the invoice,
   then *Mark paid*.
4. **Payments:** find Thando's rental payment and refund it with a reason, to show the refund path.
5. **Reports** (SC24): *Revenue & receipts* for the last 30 days, *Overdue books*, *Tutor utilisation*. Download CSV.
6. **Staff:** create a new Staff account. The person receives a set-password link.

## 8. System Admin governs (SC21, SC23, SC28)

1. Sign in as `admin` → **Admin dashboard** (SC21): accounts, roles, database size, last backup, recent audit activity.
2. **Users** (SC23): open Lerato, assign or remove a role, then deactivate and reactivate.
3. **System Settings:** change *Rental · Period days* from 7 to 10. Book Details now shows the new due date.
4. **Security:** lockout policy. Show that 5 wrong passwords lock an account, then unlock it here.
5. **Backups:** *Back up now*.
6. **Audit Logs** (SC28): filter by *Tutor verified*, *Payment recorded* or *Payment refunded* to replay the whole
   demo, with who did what, when and from where.

---

**Closing points:**
* Every rule shown (double booking, stock, due dates and fines, payments and receipts, timesheets) is enforced
  in the services and backed by database constraints.
* Each role sees only its own menu and pages, and is refused elsewhere. Everything shown is audited.
