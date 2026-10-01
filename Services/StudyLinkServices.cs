using System;
using StudyLink.Data;
using StudyLink.Services.Implementations;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Interfaces;

namespace StudyLink.Services
{
    /// <summary>
    /// Composition root. Web Forms has no built-in dependency injection, so each request (and each
    /// test) creates one of these around one DbContext; services are built lazily through their
    /// constructors, and every service depends only on interfaces. Swapping the payment gateway or
    /// the email sender is a change here and nowhere else.
    /// </summary>
    public sealed class StudyLinkServices : IDisposable
    {
        private readonly Lazy<ISettingsService> _settings;
        private readonly Lazy<IAuditService> _audit;
        private readonly Lazy<INotificationService> _notifications;
        private readonly Lazy<ILookupService> _lookups;
        private readonly Lazy<IAuthService> _auth;
        private readonly Lazy<IRegistrationService> _registration;
        private readonly Lazy<IPasswordResetService> _passwordResets;
        private readonly Lazy<IUserManagementService> _users;
        private readonly Lazy<IProfileService> _profiles;
        private readonly Lazy<ITutorVerificationService> _verification;
        private readonly Lazy<ISubjectService> _subjects;
        private readonly Lazy<IAvailabilityService> _availability;
        private readonly Lazy<IReviewService> _reviews;
        private readonly Lazy<ITutorSearchService> _tutorSearch;
        private readonly Lazy<IFeeService> _fees;
        private readonly Lazy<IPaymentService> _payments;
        private readonly Lazy<IBookingService> _bookings;
        private readonly Lazy<IPostponementService> _postponements;
        private readonly Lazy<ITimesheetService> _timesheets;
        private readonly Lazy<ICatalogueService> _catalogue;
        private readonly Lazy<IInventoryService> _inventory;
        private readonly Lazy<IRentalService> _rentals;
        private readonly Lazy<IFineService> _fines;
        private readonly Lazy<ISupplierOrderService> _supplierOrders;
        private readonly Lazy<IReportService> _reports;
        private readonly Lazy<IDashboardService> _dashboards;
        private readonly Lazy<ISupportService> _support;
        private readonly Lazy<IBackupService> _backups;
        private readonly Lazy<IMaintenanceService> _maintenance;

        public StudyLinkServices(StudyLinkDbContext db, IClock clock, IPaymentGateway gateway, IEmailSender email, IFileStore files)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
            Clock = clock ?? new SystemClock();
            Gateway = gateway ?? new SimulatedCardGateway(Clock);
            Email = email;
            Files = files;

            _audit = new Lazy<IAuditService>(() => new AuditService(Db, Clock));
            _settings = new Lazy<ISettingsService>(() => new SettingsService(Db, Audit, Clock));
            _notifications = new Lazy<INotificationService>(() => new NotificationService(Db, Clock));
            _lookups = new Lazy<ILookupService>(() => new LookupService(Db));
            _auth = new Lazy<IAuthService>(() => new AuthService(Db, Settings, Audit, Clock));
            _registration = new Lazy<IRegistrationService>(() => new RegistrationService(Db, Audit, Notifications, Clock));
            _passwordResets = new Lazy<IPasswordResetService>(() => new PasswordResetService(Db, Audit, Notifications, Email, Settings, Clock));
            _users = new Lazy<IUserManagementService>(() => new UserManagementService(Db, Audit, Notifications, PasswordResets, Clock));
            _profiles = new Lazy<IProfileService>(() => new ProfileService(Db, Audit, Notifications, Clock));
            _verification = new Lazy<ITutorVerificationService>(() => new TutorVerificationService(Db, Audit, Notifications, Clock));
            _subjects = new Lazy<ISubjectService>(() => new SubjectService(Db, Audit, Clock));
            _availability = new Lazy<IAvailabilityService>(() => new AvailabilityService(Db, Settings, Clock));
            _reviews = new Lazy<IReviewService>(() => new ReviewService(Db, Audit, Notifications, Clock));
            _tutorSearch = new Lazy<ITutorSearchService>(() => new TutorSearchService(Db, Availability, Reviews, Clock));
            _fees = new Lazy<IFeeService>(() => new FeeService(Db, Clock));
            _payments = new Lazy<IPaymentService>(() => new PaymentService(Db, Gateway, Settings, Audit, Notifications, Email, Clock));
            _bookings = new Lazy<IBookingService>(() => new BookingService(Db, Settings, Audit, Notifications, Payments, Clock));
            _postponements = new Lazy<IPostponementService>(() => new PostponementService(Db, Settings, Audit, Notifications, Clock));
            _timesheets = new Lazy<ITimesheetService>(() => new TimesheetService(Db, Settings, Audit, Notifications, Clock));
            _catalogue = new Lazy<ICatalogueService>(() => new CatalogueService(Db, Settings, Clock));
            _inventory = new Lazy<IInventoryService>(() => new InventoryService(Db, Audit, Notifications, Clock));
            _rentals = new Lazy<IRentalService>(() => new RentalService(Db, Settings, Audit, Notifications, Clock));
            _fines = new Lazy<IFineService>(() => new FineService(Db, Audit, Notifications, Clock));
            _supplierOrders = new Lazy<ISupplierOrderService>(() => new SupplierOrderService(Db, Audit, Notifications, Files, Clock));
            _reports = new Lazy<IReportService>(() => new ReportService(Db, Settings, Clock));
            _dashboards = new Lazy<IDashboardService>(() => new DashboardService(Db, Bookings, Rentals, Fees, Timesheets, Users, Inventory,
                Verification, SupplierOrders, Audit, Backups, Settings, Clock));
            _support = new Lazy<ISupportService>(() => new SupportService(Db, Notifications, Email, Clock));
            _backups = new Lazy<IBackupService>(() => new BackupService(Db, Settings, Audit, Clock));
            _maintenance = new Lazy<IMaintenanceService>(() => new MaintenanceService(Db, Bookings, Rentals, Notifications, Clock));
        }

        public StudyLinkDbContext Db { get; }
        public IClock Clock { get; }
        public IPaymentGateway Gateway { get; }
        public IEmailSender Email { get; }
        public IFileStore Files { get; }

        public ISettingsService Settings => _settings.Value;
        public IAuditService Audit => _audit.Value;
        public INotificationService Notifications => _notifications.Value;
        public ILookupService Lookups => _lookups.Value;
        public IAuthService Auth => _auth.Value;
        public IRegistrationService Registration => _registration.Value;
        public IPasswordResetService PasswordResets => _passwordResets.Value;
        public IUserManagementService Users => _users.Value;
        public IProfileService Profiles => _profiles.Value;
        public ITutorVerificationService Verification => _verification.Value;
        public ISubjectService Subjects => _subjects.Value;
        public IAvailabilityService Availability => _availability.Value;
        public IReviewService Reviews => _reviews.Value;
        public ITutorSearchService TutorSearch => _tutorSearch.Value;
        public IFeeService Fees => _fees.Value;
        public IPaymentService Payments => _payments.Value;
        public IBookingService Bookings => _bookings.Value;
        public IPostponementService Postponements => _postponements.Value;
        public ITimesheetService Timesheets => _timesheets.Value;
        public ICatalogueService Catalogue => _catalogue.Value;
        public IInventoryService Inventory => _inventory.Value;
        public IRentalService Rentals => _rentals.Value;
        public IFineService Fines => _fines.Value;
        public ISupplierOrderService SupplierOrders => _supplierOrders.Value;
        public IReportService Reports => _reports.Value;
        public IDashboardService Dashboards => _dashboards.Value;
        public ISupportService Support => _support.Value;
        public IBackupService Backups => _backups.Value;
        public IMaintenanceService Maintenance => _maintenance.Value;

        public void Dispose() => Db.Dispose();
    }
}
