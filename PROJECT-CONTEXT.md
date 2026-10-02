# StudyLink – Project Context

## System
StudyLink – Books, Tutoring & Academic Resource Hub
Developer: Nova Tech Developers
Environment: Forte Books & Tuition Hub
Campuses: Alice, East London, Bisho

## Roles
STUDENT, TUTOR, STAFF, SUPPLIER, MANAGER, SYSTEM ADMIN
- Self-register: STUDENT, TUTOR only
- TUTOR requires STAFF verification
- All other roles created by MANAGER or SYSTEM ADMIN

## Tech
ASP.NET Web Forms (C# 7.3, .NET Framework 4.8.1), Entity Framework 6 Code First + migrations, SQL Server
(Express), Bootstrap 5 grid + the StudyLink CSS design system, vanilla JavaScript, Visual Studio 2022.
Open `StudyLink.sln`; the web project sits at the repository root, with `Data`, `Services` and `Tests` beside it.
(Decision D-1: Web Forms, not ASP.NET Core MVC.)

## Demo
- Build: Debug
- Database: fresh `database\StudyLink.sql` + seed data (the demo seed runs on first start when
  `App_Data\local.settings.config` sets `Seed:Demo=true`)
- Internet: available (not required: covers ship in `Images\covers`)
- Watermark: kept
- Avatars: kept

## Non-negotiables
- Student number = S + 9 digits, 10 characters (e.g. S202600001)
- Cover required server-side for new books (seeder override only)
- Start-up data changes wrapped in their own try/catch
- Ui.Empty changed via overload, never signature change
- Avatar access matrix enforced, 404 for hidden photos
- Footer uses real contact details, no "To be confirmed"

## Consent
Photo and POPIA consent granted for uploaded images.

## Contacts
Email: support@studylink.ac.za
Phone: +27 (0)40 602 2000
