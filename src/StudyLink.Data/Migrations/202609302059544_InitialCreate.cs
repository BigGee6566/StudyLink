namespace StudyLink.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Admins",
                c => new
                    {
                        AdminId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.AdminId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        UserName = c.String(nullable: false, maxLength: 50),
                        Email = c.String(nullable: false, maxLength: 100),
                        FirstName = c.String(nullable: false, maxLength: 50),
                        LastName = c.String(nullable: false, maxLength: 50),
                        Gender = c.Int(nullable: false),
                        CellNumber = c.String(nullable: false, maxLength: 15),
                        CampusId = c.Int(nullable: false),
                        PasswordHash = c.Binary(nullable: false, maxLength: 64),
                        PasswordSalt = c.Binary(nullable: false, maxLength: 32),
                        HashIterations = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        FailedLoginCount = c.Int(nullable: false),
                        LockoutEnd = c.DateTime(precision: 0, storeType: "datetime2"),
                        MustChangePassword = c.Boolean(nullable: false),
                        LastLoginAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        UpdatedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CreatedByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.Users", t => t.CreatedByUserId)
                .Index(t => t.UserName, unique: true)
                .Index(t => t.Email, unique: true)
                .Index(t => t.CampusId)
                .Index(t => t.CreatedByUserId);
            
            CreateTable(
                "dbo.Campuses",
                c => new
                    {
                        CampusId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CampusId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        RoleId = c.Int(nullable: false),
                        AssignedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        AssignedByUserId = c.Int(),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.Users", t => t.AssignedByUserId)
                .ForeignKey("dbo.Roles", t => t.RoleId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.RoleId)
                .Index(t => t.AssignedByUserId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        RoleId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 30),
                        Description = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.RoleId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        AuditId = c.Long(nullable: false, identity: true),
                        ActorUserId = c.Int(),
                        ActorRole = c.String(nullable: false, maxLength: 30),
                        Action = c.String(nullable: false, maxLength: 50),
                        EntityType = c.String(nullable: false, maxLength: 40),
                        EntityId = c.String(nullable: false, maxLength: 40),
                        PreviousState = c.String(maxLength: 1000),
                        NewState = c.String(maxLength: 1000),
                        Details = c.String(maxLength: 500),
                        IpAddress = c.String(maxLength: 45),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.AuditId)
                .ForeignKey("dbo.Users", t => t.ActorUserId)
                .Index(t => t.ActorUserId)
                .Index(t => t.Action, name: "IX_AuditLog_Action")
                .Index(t => new { t.EntityType, t.EntityId }, name: "IX_AuditLog_Entity")
                .Index(t => t.CreatedAt, name: "IX_AuditLog_Created");
            
            CreateTable(
                "dbo.BackupLogs",
                c => new
                    {
                        BackupId = c.Int(nullable: false, identity: true),
                        FilePath = c.String(nullable: false, maxLength: 400),
                        Status = c.Int(nullable: false),
                        StartedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        CompletedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        SizeBytes = c.Long(),
                        RequestedByUserId = c.Int(nullable: false),
                        Message = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.BackupId)
                .ForeignKey("dbo.Users", t => t.RequestedByUserId)
                .Index(t => t.RequestedByUserId);
            
            CreateTable(
                "dbo.BookCopies",
                c => new
                    {
                        CopyId = c.Int(nullable: false, identity: true),
                        BookId = c.Int(nullable: false),
                        CampusId = c.Int(nullable: false),
                        Barcode = c.String(nullable: false, maxLength: 30),
                        Condition = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        AcquiredAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        DeliveryLineId = c.Int(),
                        Notes = c.String(maxLength: 200),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.CopyId)
                .ForeignKey("dbo.Books", t => t.BookId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.DeliveryLines", t => t.DeliveryLineId)
                .Index(t => t.BookId)
                .Index(t => t.CampusId)
                .Index(t => t.Barcode, unique: true)
                .Index(t => t.DeliveryLineId);
            
            CreateTable(
                "dbo.Books",
                c => new
                    {
                        BookId = c.Int(nullable: false, identity: true),
                        Isbn = c.String(nullable: false, maxLength: 20),
                        Title = c.String(nullable: false, maxLength: 200),
                        Authors = c.String(nullable: false, maxLength: 200),
                        Edition = c.String(maxLength: 30),
                        Publisher = c.String(maxLength: 100),
                        PublishedYear = c.Int(),
                        CategoryId = c.Int(nullable: false),
                        SubjectId = c.Int(),
                        Description = c.String(maxLength: 1000),
                        CoverImage = c.Binary(),
                        CoverContentType = c.String(maxLength: 50),
                        SalePrice = c.Decimal(nullable: false, precision: 12, scale: 2),
                        WeeklyRentalFee = c.Decimal(nullable: false, precision: 12, scale: 2),
                        LateFeePerDay = c.Decimal(nullable: false, precision: 12, scale: 2),
                        ReorderLevel = c.Int(nullable: false),
                        IsRentable = c.Boolean(nullable: false),
                        IsForSale = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        PreferredSupplierId = c.Int(),
                        CreatedByManagerId = c.Int(),
                        UpdatedByManagerId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        UpdatedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.BookId)
                .ForeignKey("dbo.Categories", t => t.CategoryId)
                .ForeignKey("dbo.Managers", t => t.CreatedByManagerId)
                .ForeignKey("dbo.Suppliers", t => t.PreferredSupplierId)
                .ForeignKey("dbo.Subjects", t => t.SubjectId)
                .ForeignKey("dbo.Managers", t => t.UpdatedByManagerId)
                .Index(t => t.Isbn, unique: true)
                .Index(t => t.CategoryId)
                .Index(t => t.SubjectId)
                .Index(t => t.PreferredSupplierId)
                .Index(t => t.CreatedByManagerId)
                .Index(t => t.UpdatedByManagerId);
            
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        CategoryId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 80),
                        Description = c.String(maxLength: 300),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.CategoryId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.Managers",
                c => new
                    {
                        ManagerId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        EmployeeNumber = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.ManagerId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true)
                .Index(t => t.EmployeeNumber, unique: true);
            
            CreateTable(
                "dbo.Suppliers",
                c => new
                    {
                        SupplierId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        CompanyName = c.String(nullable: false, maxLength: 120),
                        ContactPerson = c.String(nullable: false, maxLength: 100),
                        VatNumber = c.String(maxLength: 20),
                        Phone = c.String(nullable: false, maxLength: 20),
                        Address = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.SupplierId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true);
            
            CreateTable(
                "dbo.Subjects",
                c => new
                    {
                        SubjectId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.SubjectId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.DeliveryLines",
                c => new
                    {
                        DeliveryLineId = c.Int(nullable: false, identity: true),
                        DeliveryId = c.Int(nullable: false),
                        OrderLineId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        QuantityAccepted = c.Int(),
                    })
                .PrimaryKey(t => t.DeliveryLineId)
                .ForeignKey("dbo.Deliveries", t => t.DeliveryId)
                .ForeignKey("dbo.SupplierOrderLines", t => t.OrderLineId)
                .Index(t => t.DeliveryId)
                .Index(t => t.OrderLineId);
            
            CreateTable(
                "dbo.Deliveries",
                c => new
                    {
                        DeliveryId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 20),
                        OrderId = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        DispatchedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        DispatchedByUserId = c.Int(nullable: false),
                        ReceivedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ReceivedByUserId = c.Int(),
                        Note = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.DeliveryId)
                .ForeignKey("dbo.Users", t => t.DispatchedByUserId)
                .ForeignKey("dbo.SupplierOrders", t => t.OrderId)
                .ForeignKey("dbo.Users", t => t.ReceivedByUserId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.OrderId)
                .Index(t => t.DispatchedByUserId)
                .Index(t => t.ReceivedByUserId);
            
            CreateTable(
                "dbo.SupplierOrders",
                c => new
                    {
                        OrderId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 20),
                        SupplierId = c.Int(nullable: false),
                        CreatedByManagerId = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        OrderedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        ExpectedDate = c.DateTime(storeType: "date"),
                        AcknowledgedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        Notes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.OrderId)
                .ForeignKey("dbo.Managers", t => t.CreatedByManagerId)
                .ForeignKey("dbo.Suppliers", t => t.SupplierId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.SupplierId)
                .Index(t => t.CreatedByManagerId);
            
            CreateTable(
                "dbo.SupplierOrderLines",
                c => new
                    {
                        LineId = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        BookId = c.Int(nullable: false),
                        CampusId = c.Int(nullable: false),
                        QuantityOrdered = c.Int(nullable: false),
                        QuantityDelivered = c.Int(nullable: false),
                        UnitCost = c.Decimal(nullable: false, precision: 12, scale: 2),
                    })
                .PrimaryKey(t => t.LineId)
                .ForeignKey("dbo.Books", t => t.BookId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.SupplierOrders", t => t.OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.BookId)
                .Index(t => t.CampusId);
            
            CreateTable(
                "dbo.Bookings",
                c => new
                    {
                        BookingId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 20),
                        StudentId = c.Int(nullable: false),
                        TutorId = c.Int(nullable: false),
                        TutorSubjectId = c.Int(nullable: false),
                        AvailabilityId = c.Int(nullable: false),
                        CampusId = c.Int(nullable: false),
                        SessionDate = c.DateTime(nullable: false, storeType: "date"),
                        StartTime = c.Time(nullable: false, precision: 0),
                        EndTime = c.Time(nullable: false, precision: 0),
                        Fee = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Status = c.Int(nullable: false),
                        StudentMessage = c.String(maxLength: 500),
                        HoldExpiresAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        PaidAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        TutorRespondedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        DeclineReason = c.String(maxLength: 200),
                        CancelledAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CancelledByUserId = c.Int(),
                        CancelReasonCategory = c.Int(),
                        CancelReason = c.String(maxLength: 200),
                        CompletedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.BookingId)
                .ForeignKey("dbo.TutorAvailability", t => t.AvailabilityId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.Users", t => t.CancelledByUserId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .ForeignKey("dbo.TutorSubjects", t => t.TutorSubjectId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.StudentId)
                .Index(t => t.TutorId)
                .Index(t => t.TutorSubjectId)
                .Index(t => t.AvailabilityId)
                .Index(t => t.CampusId)
                .Index(t => t.CancelledByUserId);
            
            CreateTable(
                "dbo.TutorAvailability",
                c => new
                    {
                        AvailabilityId = c.Int(nullable: false, identity: true),
                        TutorSubjectId = c.Int(nullable: false),
                        CampusId = c.Int(nullable: false),
                        AvailabilityDate = c.DateTime(nullable: false, storeType: "date"),
                        StartTime = c.Time(nullable: false, precision: 0),
                        EndTime = c.Time(nullable: false, precision: 0),
                        IsBooked = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.AvailabilityId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.TutorSubjects", t => t.TutorSubjectId)
                .Index(t => t.TutorSubjectId)
                .Index(t => t.CampusId);
            
            CreateTable(
                "dbo.TutorSubjects",
                c => new
                    {
                        TutorSubjectId = c.Int(nullable: false, identity: true),
                        TutorId = c.Int(nullable: false),
                        SubjectId = c.Int(nullable: false),
                        StudyLevelId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.TutorSubjectId)
                .ForeignKey("dbo.StudyLevels", t => t.StudyLevelId)
                .ForeignKey("dbo.Subjects", t => t.SubjectId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .Index(t => new { t.TutorId, t.SubjectId, t.StudyLevelId }, unique: true, name: "UQ_TutorSubject");
            
            CreateTable(
                "dbo.StudyLevels",
                c => new
                    {
                        StudyLevelId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        SortOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.StudyLevelId)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.Tutors",
                c => new
                    {
                        TutorId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Qualification = c.String(nullable: false, maxLength: 100),
                        ExperienceYears = c.Int(nullable: false),
                        HourlyRate = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Bio = c.String(maxLength: 1000),
                        VerificationStatus = c.Int(nullable: false),
                        VerifiedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.TutorId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true);
            
            CreateTable(
                "dbo.TutorVerifications",
                c => new
                    {
                        VerificationId = c.Int(nullable: false, identity: true),
                        TutorId = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        Decision = c.Int(nullable: false),
                        ReviewedByUserId = c.Int(),
                        ReviewedByStaffId = c.Int(),
                        ReviewedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        IdentityChecked = c.Boolean(nullable: false),
                        QualificationChecked = c.Boolean(nullable: false),
                        Notes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.VerificationId)
                .ForeignKey("dbo.Users", t => t.ReviewedByUserId)
                .ForeignKey("dbo.Staff", t => t.ReviewedByStaffId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .Index(t => t.TutorId)
                .Index(t => t.ReviewedByUserId)
                .Index(t => t.ReviewedByStaffId);
            
            CreateTable(
                "dbo.Staff",
                c => new
                    {
                        StaffId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        EmployeeNumber = c.String(nullable: false, maxLength: 20),
                        JobTitle = c.String(maxLength: 60),
                    })
                .PrimaryKey(t => t.StaffId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true)
                .Index(t => t.EmployeeNumber, unique: true);
            
            CreateTable(
                "dbo.Students",
                c => new
                    {
                        StudentId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        StudentNumber = c.String(nullable: false, maxLength: 20),
                        StudyLevelId = c.Int(),
                        Faculty = c.String(maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.StudentId)
                .ForeignKey("dbo.StudyLevels", t => t.StudyLevelId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId, unique: true)
                .Index(t => t.StudentNumber, unique: true)
                .Index(t => t.StudyLevelId);
            
            CreateTable(
                "dbo.BookWaitlists",
                c => new
                    {
                        WaitlistId = c.Int(nullable: false, identity: true),
                        BookId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        CampusId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        NotifiedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.WaitlistId)
                .ForeignKey("dbo.Books", t => t.BookId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => new { t.BookId, t.StudentId, t.CampusId }, unique: true, name: "UQ_BookWaitlist");
            
            CreateTable(
                "dbo.DailyMetrics",
                c => new
                    {
                        MetricDate = c.DateTime(nullable: false, storeType: "date"),
                        Revenue = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Refunds = c.Decimal(nullable: false, precision: 12, scale: 2),
                        PaymentsCount = c.Int(nullable: false),
                        BookingsCreated = c.Int(nullable: false),
                        SessionsCompleted = c.Int(nullable: false),
                        RentalsStarted = c.Int(nullable: false),
                        RentalsReturned = c.Int(nullable: false),
                        OverdueRentals = c.Int(nullable: false),
                        BooksSold = c.Int(nullable: false),
                        FinesIssued = c.Decimal(nullable: false, precision: 12, scale: 2),
                        NewStudents = c.Int(nullable: false),
                        NewTutors = c.Int(nullable: false),
                        CapturedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.MetricDate);
            
            CreateTable(
                "dbo.Fees",
                c => new
                    {
                        FeeId = c.Int(nullable: false, identity: true),
                        StudentId = c.Int(nullable: false),
                        Type = c.Int(nullable: false),
                        Description = c.String(nullable: false, maxLength: 200),
                        Amount = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Status = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        SettledAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        BookingId = c.Int(),
                        RentalId = c.Int(),
                        RentalRenewalId = c.Int(),
                        SaleId = c.Int(),
                        FineId = c.Int(),
                    })
                .PrimaryKey(t => t.FeeId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .ForeignKey("dbo.Fines", t => t.FineId)
                .ForeignKey("dbo.Rentals", t => t.RentalId)
                .ForeignKey("dbo.RentalRenewals", t => t.RentalRenewalId)
                .ForeignKey("dbo.Sales", t => t.SaleId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.StudentId)
                .Index(t => t.BookingId)
                .Index(t => t.RentalId)
                .Index(t => t.RentalRenewalId)
                .Index(t => t.SaleId)
                .Index(t => t.FineId);
            
            CreateTable(
                "dbo.PaymentAllocations",
                c => new
                    {
                        AllocationId = c.Int(nullable: false, identity: true),
                        PaymentId = c.Int(nullable: false),
                        FeeId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 12, scale: 2),
                    })
                .PrimaryKey(t => t.AllocationId)
                .ForeignKey("dbo.Fees", t => t.FeeId)
                .ForeignKey("dbo.Payments", t => t.PaymentId)
                .Index(t => t.PaymentId)
                .Index(t => t.FeeId);
            
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        PaymentId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 30),
                        StudentId = c.Int(nullable: false),
                        Type = c.Int(nullable: false),
                        Method = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 12, scale: 2),
                        CardBrand = c.String(maxLength: 20),
                        CardLast4 = c.String(maxLength: 4),
                        GatewayReference = c.String(maxLength: 50),
                        FailureReason = c.String(maxLength: 200),
                        CheckoutToken = c.Guid(),
                        RefundOfPaymentId = c.Int(),
                        Note = c.String(maxLength: 200),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        CompletedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ProcessedByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.PaymentId)
                .ForeignKey("dbo.Users", t => t.ProcessedByUserId)
                .ForeignKey("dbo.Payments", t => t.RefundOfPaymentId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.StudentId)
                .Index(t => t.RefundOfPaymentId)
                .Index(t => t.ProcessedByUserId);
            
            CreateTable(
                "dbo.Fines",
                c => new
                    {
                        FineId = c.Int(nullable: false, identity: true),
                        RentalId = c.Int(nullable: false),
                        Type = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 12, scale: 2),
                        DaysLate = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Reason = c.String(maxLength: 200),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        CreatedByUserId = c.Int(),
                        WaivedByUserId = c.Int(),
                        WaivedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        WaiverReason = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.FineId)
                .ForeignKey("dbo.Users", t => t.CreatedByUserId)
                .ForeignKey("dbo.Rentals", t => t.RentalId)
                .ForeignKey("dbo.Users", t => t.WaivedByUserId)
                .Index(t => t.RentalId)
                .Index(t => t.CreatedByUserId)
                .Index(t => t.WaivedByUserId);
            
            CreateTable(
                "dbo.Rentals",
                c => new
                    {
                        RentalId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 20),
                        StudentId = c.Int(nullable: false),
                        CopyId = c.Int(nullable: false),
                        RentedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        DueDate = c.DateTime(nullable: false, storeType: "date"),
                        ReturnedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        Status = c.Int(nullable: false),
                        Fee = c.Decimal(nullable: false, precision: 12, scale: 2),
                        RenewalCount = c.Int(nullable: false),
                        HoldExpiresAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ReturnCondition = c.Int(),
                        ProcessedByUserId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.RentalId)
                .ForeignKey("dbo.BookCopies", t => t.CopyId)
                .ForeignKey("dbo.Users", t => t.ProcessedByUserId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.StudentId)
                .Index(t => t.CopyId)
                .Index(t => t.ProcessedByUserId);
            
            CreateTable(
                "dbo.RentalRenewals",
                c => new
                    {
                        RenewalId = c.Int(nullable: false, identity: true),
                        RentalId = c.Int(nullable: false),
                        RenewedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        PreviousDueDate = c.DateTime(nullable: false, storeType: "date"),
                        NewDueDate = c.DateTime(nullable: false, storeType: "date"),
                        Fee = c.Decimal(nullable: false, precision: 12, scale: 2),
                    })
                .PrimaryKey(t => t.RenewalId)
                .ForeignKey("dbo.Rentals", t => t.RentalId)
                .Index(t => t.RentalId);
            
            CreateTable(
                "dbo.Sales",
                c => new
                    {
                        SaleId = c.Int(nullable: false, identity: true),
                        Reference = c.String(nullable: false, maxLength: 20),
                        StudentId = c.Int(nullable: false),
                        CopyId = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Status = c.Int(nullable: false),
                        HoldExpiresAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        CompletedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.SaleId)
                .ForeignKey("dbo.BookCopies", t => t.CopyId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.Reference, unique: true)
                .Index(t => t.StudentId)
                .Index(t => t.CopyId);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        NotificationId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 150),
                        Message = c.String(nullable: false, maxLength: 500),
                        Type = c.Int(nullable: false),
                        LinkUrl = c.String(maxLength: 300),
                        IsRead = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        DedupKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.NotificationId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => new { t.UserId, t.IsRead }, name: "IX_Notification_UserUnread");
            
            CreateTable(
                "dbo.PasswordResetRequests",
                c => new
                    {
                        RequestId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(),
                        Identifier = c.String(nullable: false, maxLength: 100),
                        Message = c.String(maxLength: 300),
                        Status = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        HandledByUserId = c.Int(),
                        HandledAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ResponseNote = c.String(maxLength: 200),
                        RequestIp = c.String(maxLength: 45),
                        User_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.RequestId)
                .ForeignKey("dbo.Users", t => t.HandledByUserId)
                .ForeignKey("dbo.Users", t => t.User_UserId)
                .Index(t => t.HandledByUserId)
                .Index(t => t.User_UserId);
            
            CreateTable(
                "dbo.PasswordResetTokens",
                c => new
                    {
                        TokenId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        TokenHash = c.Binary(nullable: false, maxLength: 32),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        ExpiresAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        UsedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        RequestId = c.Int(),
                    })
                .PrimaryKey(t => t.TokenId)
                .ForeignKey("dbo.PasswordResetRequests", t => t.RequestId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.TokenHash, unique: true)
                .Index(t => t.RequestId);
            
            CreateTable(
                "dbo.PostponementRequests",
                c => new
                    {
                        PostponementId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        StudentId = c.Int(nullable: false),
                        TutorId = c.Int(nullable: false),
                        OriginalDate = c.DateTime(nullable: false, storeType: "date"),
                        OriginalStart = c.Time(nullable: false, precision: 0),
                        OriginalEnd = c.Time(nullable: false, precision: 0),
                        ProposedDate = c.DateTime(nullable: false, storeType: "date"),
                        ProposedStart = c.Time(nullable: false, precision: 0),
                        ProposedEnd = c.Time(nullable: false, precision: 0),
                        ReasonCategory = c.Int(nullable: false),
                        StudentNote = c.String(maxLength: 200),
                        Status = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        TutorResponseNote = c.String(maxLength: 200),
                        RespondedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.PostponementId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .Index(t => t.BookingId)
                .Index(t => t.StudentId)
                .Index(t => t.TutorId);
            
            CreateTable(
                "dbo.ProfileUpdateRequests",
                c => new
                    {
                        RequestId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        FieldName = c.String(nullable: false, maxLength: 50),
                        CurrentValue = c.String(maxLength: 200),
                        RequestedValue = c.String(nullable: false, maxLength: 200),
                        Reason = c.String(maxLength: 300),
                        Status = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        HandledByUserId = c.Int(),
                        HandledAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ResponseNote = c.String(maxLength: 200),
                        User_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.RequestId)
                .ForeignKey("dbo.Users", t => t.HandledByUserId)
                .ForeignKey("dbo.Users", t => t.User_UserId)
                .Index(t => t.HandledByUserId)
                .Index(t => t.User_UserId);
            
            CreateTable(
                "dbo.ReportRuns",
                c => new
                    {
                        ReportRunId = c.Int(nullable: false, identity: true),
                        ReportKey = c.String(nullable: false, maxLength: 50),
                        Parameters = c.String(maxLength: 300),
                        Format = c.String(maxLength: 10),
                        RowCount = c.Int(nullable: false),
                        GeneratedByUserId = c.Int(nullable: false),
                        GeneratedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.ReportRunId)
                .ForeignKey("dbo.Users", t => t.GeneratedByUserId)
                .Index(t => t.GeneratedByUserId);
            
            CreateTable(
                "dbo.Reviews",
                c => new
                    {
                        ReviewId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        Rating = c.Byte(nullable: false),
                        Comment = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        IsHidden = c.Boolean(nullable: false),
                        HiddenReason = c.String(maxLength: 200),
                        HiddenAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        HiddenByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.ReviewId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .ForeignKey("dbo.Users", t => t.HiddenByUserId)
                .Index(t => t.BookingId, unique: true)
                .Index(t => t.HiddenByUserId);
            
            CreateTable(
                "dbo.StudentSubjects",
                c => new
                    {
                        StudentSubjectId = c.Int(nullable: false, identity: true),
                        StudentId = c.Int(nullable: false),
                        SubjectId = c.Int(nullable: false),
                        StudyLevelId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.StudentSubjectId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .ForeignKey("dbo.StudyLevels", t => t.StudyLevelId)
                .ForeignKey("dbo.Subjects", t => t.SubjectId)
                .Index(t => new { t.StudentId, t.SubjectId, t.StudyLevelId }, unique: true, name: "UQ_StudentSubject");
            
            CreateTable(
                "dbo.SupplierInvoices",
                c => new
                    {
                        InvoiceId = c.Int(nullable: false, identity: true),
                        SupplierId = c.Int(nullable: false),
                        OrderId = c.Int(nullable: false),
                        InvoiceNumber = c.String(nullable: false, maxLength: 40),
                        Amount = c.Decimal(nullable: false, precision: 12, scale: 2),
                        OriginalFileName = c.String(nullable: false, maxLength: 200),
                        StoredFileName = c.String(nullable: false, maxLength: 100),
                        UploadedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        Status = c.Int(nullable: false),
                        ReviewedByUserId = c.Int(),
                        ReviewedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        PaidAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        Note = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.InvoiceId)
                .ForeignKey("dbo.SupplierOrders", t => t.OrderId)
                .ForeignKey("dbo.Users", t => t.ReviewedByUserId)
                .ForeignKey("dbo.Suppliers", t => t.SupplierId)
                .Index(t => new { t.SupplierId, t.InvoiceNumber }, unique: true, name: "UQ_SupplierInvoice_Number")
                .Index(t => t.OrderId)
                .Index(t => t.ReviewedByUserId);
            
            CreateTable(
                "dbo.SupportMessages",
                c => new
                    {
                        MessageId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 100),
                        CampusId = c.Int(),
                        Subject = c.String(nullable: false, maxLength: 150),
                        Body = c.String(nullable: false, maxLength: 2000),
                        Status = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        HandledByUserId = c.Int(),
                        HandledAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ResponseNote = c.String(maxLength: 500),
                        User_UserId = c.Int(),
                    })
                .PrimaryKey(t => t.MessageId)
                .ForeignKey("dbo.Campuses", t => t.CampusId)
                .ForeignKey("dbo.Users", t => t.HandledByUserId)
                .ForeignKey("dbo.Users", t => t.User_UserId)
                .Index(t => t.CampusId)
                .Index(t => t.HandledByUserId)
                .Index(t => t.User_UserId);
            
            CreateTable(
                "dbo.SystemSettings",
                c => new
                    {
                        SettingKey = c.String(nullable: false, maxLength: 60),
                        Value = c.String(nullable: false, maxLength: 200),
                        Description = c.String(maxLength: 300),
                        UpdatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                        UpdatedByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.SettingKey)
                .ForeignKey("dbo.Users", t => t.UpdatedByUserId)
                .Index(t => t.UpdatedByUserId);
            
            CreateTable(
                "dbo.TimesheetEntries",
                c => new
                    {
                        EntryId = c.Int(nullable: false, identity: true),
                        TimesheetId = c.Int(nullable: false),
                        BookingId = c.Int(),
                        WorkDate = c.DateTime(nullable: false, storeType: "date"),
                        Hours = c.Decimal(nullable: false, precision: 12, scale: 2),
                        Description = c.String(nullable: false, maxLength: 200),
                    })
                .PrimaryKey(t => t.EntryId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .ForeignKey("dbo.Timesheets", t => t.TimesheetId)
                .Index(t => t.TimesheetId)
                .Index(t => t.BookingId);
            
            CreateTable(
                "dbo.Timesheets",
                c => new
                    {
                        TimesheetId = c.Int(nullable: false, identity: true),
                        TutorId = c.Int(nullable: false),
                        WeekStart = c.DateTime(nullable: false, storeType: "date"),
                        Status = c.Int(nullable: false),
                        TotalHours = c.Decimal(nullable: false, precision: 12, scale: 2),
                        SubmittedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ReviewedByUserId = c.Int(),
                        ReviewedAt = c.DateTime(precision: 0, storeType: "datetime2"),
                        ReviewNote = c.String(maxLength: 300),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.TimesheetId)
                .ForeignKey("dbo.Users", t => t.ReviewedByUserId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .Index(t => new { t.TutorId, t.WeekStart }, unique: true, name: "UQ_Timesheet_TutorWeek")
                .Index(t => t.ReviewedByUserId);
            
            CreateTable(
                "dbo.TutoringSessions",
                c => new
                    {
                        SessionId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        SessionDate = c.DateTime(nullable: false, storeType: "date"),
                        Attendance = c.Int(nullable: false),
                        HoursDelivered = c.Decimal(nullable: false, precision: 12, scale: 2),
                        TutorNotes = c.String(maxLength: 1000),
                        CreatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.SessionId)
                .ForeignKey("dbo.Bookings", t => t.BookingId)
                .Index(t => t.BookingId, unique: true);
            
            CreateTable(
                "dbo.TutorStats",
                c => new
                    {
                        TutorId = c.Int(nullable: false),
                        SessionsCompleted = c.Int(nullable: false),
                        SessionsCancelled = c.Int(nullable: false),
                        HoursDelivered = c.Decimal(nullable: false, precision: 12, scale: 2),
                        HoursPublished = c.Decimal(nullable: false, precision: 12, scale: 2),
                        UtilisationPercent = c.Decimal(nullable: false, precision: 12, scale: 2),
                        AverageRating = c.Decimal(precision: 12, scale: 2),
                        ReviewCount = c.Int(nullable: false),
                        GrossEarnings = c.Decimal(nullable: false, precision: 12, scale: 2),
                        RefreshedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.TutorId)
                .ForeignKey("dbo.Tutors", t => t.TutorId)
                .Index(t => t.TutorId);

            // Filtered unique indexes and CHECK constraints EF6 cannot express (double-booking,
            // stock and payment backstops). Hand-written; keep this line if the migration is re-scaffolded.
            IntegrityConstraints.Up(s => Sql(s));
        }

        public override void Down()
        {
            IntegrityConstraints.Down(s => Sql(s));
            DropForeignKey("dbo.TutorStats", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.TutoringSessions", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.TimesheetEntries", "TimesheetId", "dbo.Timesheets");
            DropForeignKey("dbo.Timesheets", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.Timesheets", "ReviewedByUserId", "dbo.Users");
            DropForeignKey("dbo.TimesheetEntries", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.SystemSettings", "UpdatedByUserId", "dbo.Users");
            DropForeignKey("dbo.SupportMessages", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.SupportMessages", "HandledByUserId", "dbo.Users");
            DropForeignKey("dbo.SupportMessages", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.SupplierInvoices", "SupplierId", "dbo.Suppliers");
            DropForeignKey("dbo.SupplierInvoices", "ReviewedByUserId", "dbo.Users");
            DropForeignKey("dbo.SupplierInvoices", "OrderId", "dbo.SupplierOrders");
            DropForeignKey("dbo.StudentSubjects", "SubjectId", "dbo.Subjects");
            DropForeignKey("dbo.StudentSubjects", "StudyLevelId", "dbo.StudyLevels");
            DropForeignKey("dbo.StudentSubjects", "StudentId", "dbo.Students");
            DropForeignKey("dbo.Reviews", "HiddenByUserId", "dbo.Users");
            DropForeignKey("dbo.Reviews", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.ReportRuns", "GeneratedByUserId", "dbo.Users");
            DropForeignKey("dbo.ProfileUpdateRequests", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.ProfileUpdateRequests", "HandledByUserId", "dbo.Users");
            DropForeignKey("dbo.PostponementRequests", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.PostponementRequests", "StudentId", "dbo.Students");
            DropForeignKey("dbo.PostponementRequests", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.PasswordResetTokens", "UserId", "dbo.Users");
            DropForeignKey("dbo.PasswordResetTokens", "RequestId", "dbo.PasswordResetRequests");
            DropForeignKey("dbo.PasswordResetRequests", "User_UserId", "dbo.Users");
            DropForeignKey("dbo.PasswordResetRequests", "HandledByUserId", "dbo.Users");
            DropForeignKey("dbo.Notifications", "UserId", "dbo.Users");
            DropForeignKey("dbo.Fees", "StudentId", "dbo.Students");
            DropForeignKey("dbo.Fees", "SaleId", "dbo.Sales");
            DropForeignKey("dbo.Sales", "StudentId", "dbo.Students");
            DropForeignKey("dbo.Sales", "CopyId", "dbo.BookCopies");
            DropForeignKey("dbo.Fees", "RentalRenewalId", "dbo.RentalRenewals");
            DropForeignKey("dbo.Fees", "RentalId", "dbo.Rentals");
            DropForeignKey("dbo.Fees", "FineId", "dbo.Fines");
            DropForeignKey("dbo.Fines", "WaivedByUserId", "dbo.Users");
            DropForeignKey("dbo.Fines", "RentalId", "dbo.Rentals");
            DropForeignKey("dbo.Rentals", "StudentId", "dbo.Students");
            DropForeignKey("dbo.RentalRenewals", "RentalId", "dbo.Rentals");
            DropForeignKey("dbo.Rentals", "ProcessedByUserId", "dbo.Users");
            DropForeignKey("dbo.Rentals", "CopyId", "dbo.BookCopies");
            DropForeignKey("dbo.Fines", "CreatedByUserId", "dbo.Users");
            DropForeignKey("dbo.Fees", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.PaymentAllocations", "PaymentId", "dbo.Payments");
            DropForeignKey("dbo.Payments", "StudentId", "dbo.Students");
            DropForeignKey("dbo.Payments", "RefundOfPaymentId", "dbo.Payments");
            DropForeignKey("dbo.Payments", "ProcessedByUserId", "dbo.Users");
            DropForeignKey("dbo.PaymentAllocations", "FeeId", "dbo.Fees");
            DropForeignKey("dbo.BookWaitlists", "StudentId", "dbo.Students");
            DropForeignKey("dbo.BookWaitlists", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.BookWaitlists", "BookId", "dbo.Books");
            DropForeignKey("dbo.Bookings", "TutorSubjectId", "dbo.TutorSubjects");
            DropForeignKey("dbo.Bookings", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.Bookings", "StudentId", "dbo.Students");
            DropForeignKey("dbo.Students", "UserId", "dbo.Users");
            DropForeignKey("dbo.Students", "StudyLevelId", "dbo.StudyLevels");
            DropForeignKey("dbo.Bookings", "CancelledByUserId", "dbo.Users");
            DropForeignKey("dbo.Bookings", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.Bookings", "AvailabilityId", "dbo.TutorAvailability");
            DropForeignKey("dbo.TutorAvailability", "TutorSubjectId", "dbo.TutorSubjects");
            DropForeignKey("dbo.TutorSubjects", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.TutorVerifications", "TutorId", "dbo.Tutors");
            DropForeignKey("dbo.TutorVerifications", "ReviewedByStaffId", "dbo.Staff");
            DropForeignKey("dbo.Staff", "UserId", "dbo.Users");
            DropForeignKey("dbo.TutorVerifications", "ReviewedByUserId", "dbo.Users");
            DropForeignKey("dbo.Tutors", "UserId", "dbo.Users");
            DropForeignKey("dbo.TutorSubjects", "SubjectId", "dbo.Subjects");
            DropForeignKey("dbo.TutorSubjects", "StudyLevelId", "dbo.StudyLevels");
            DropForeignKey("dbo.TutorAvailability", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.BookCopies", "DeliveryLineId", "dbo.DeliveryLines");
            DropForeignKey("dbo.DeliveryLines", "OrderLineId", "dbo.SupplierOrderLines");
            DropForeignKey("dbo.DeliveryLines", "DeliveryId", "dbo.Deliveries");
            DropForeignKey("dbo.Deliveries", "ReceivedByUserId", "dbo.Users");
            DropForeignKey("dbo.Deliveries", "OrderId", "dbo.SupplierOrders");
            DropForeignKey("dbo.SupplierOrders", "SupplierId", "dbo.Suppliers");
            DropForeignKey("dbo.SupplierOrderLines", "OrderId", "dbo.SupplierOrders");
            DropForeignKey("dbo.SupplierOrderLines", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.SupplierOrderLines", "BookId", "dbo.Books");
            DropForeignKey("dbo.SupplierOrders", "CreatedByManagerId", "dbo.Managers");
            DropForeignKey("dbo.Deliveries", "DispatchedByUserId", "dbo.Users");
            DropForeignKey("dbo.BookCopies", "CampusId", "dbo.Campuses");
            DropForeignKey("dbo.BookCopies", "BookId", "dbo.Books");
            DropForeignKey("dbo.Books", "UpdatedByManagerId", "dbo.Managers");
            DropForeignKey("dbo.Books", "SubjectId", "dbo.Subjects");
            DropForeignKey("dbo.Books", "PreferredSupplierId", "dbo.Suppliers");
            DropForeignKey("dbo.Suppliers", "UserId", "dbo.Users");
            DropForeignKey("dbo.Books", "CreatedByManagerId", "dbo.Managers");
            DropForeignKey("dbo.Managers", "UserId", "dbo.Users");
            DropForeignKey("dbo.Books", "CategoryId", "dbo.Categories");
            DropForeignKey("dbo.BackupLogs", "RequestedByUserId", "dbo.Users");
            DropForeignKey("dbo.AuditLogs", "ActorUserId", "dbo.Users");
            DropForeignKey("dbo.Admins", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.UserRoles", "AssignedByUserId", "dbo.Users");
            DropForeignKey("dbo.Users", "CreatedByUserId", "dbo.Users");
            DropForeignKey("dbo.Users", "CampusId", "dbo.Campuses");
            DropIndex("dbo.TutorStats", new[] { "TutorId" });
            DropIndex("dbo.TutoringSessions", new[] { "BookingId" });
            DropIndex("dbo.Timesheets", new[] { "ReviewedByUserId" });
            DropIndex("dbo.Timesheets", "UQ_Timesheet_TutorWeek");
            DropIndex("dbo.TimesheetEntries", new[] { "BookingId" });
            DropIndex("dbo.TimesheetEntries", new[] { "TimesheetId" });
            DropIndex("dbo.SystemSettings", new[] { "UpdatedByUserId" });
            DropIndex("dbo.SupportMessages", new[] { "User_UserId" });
            DropIndex("dbo.SupportMessages", new[] { "HandledByUserId" });
            DropIndex("dbo.SupportMessages", new[] { "CampusId" });
            DropIndex("dbo.SupplierInvoices", new[] { "ReviewedByUserId" });
            DropIndex("dbo.SupplierInvoices", new[] { "OrderId" });
            DropIndex("dbo.SupplierInvoices", "UQ_SupplierInvoice_Number");
            DropIndex("dbo.StudentSubjects", "UQ_StudentSubject");
            DropIndex("dbo.Reviews", new[] { "HiddenByUserId" });
            DropIndex("dbo.Reviews", new[] { "BookingId" });
            DropIndex("dbo.ReportRuns", new[] { "GeneratedByUserId" });
            DropIndex("dbo.ProfileUpdateRequests", new[] { "User_UserId" });
            DropIndex("dbo.ProfileUpdateRequests", new[] { "HandledByUserId" });
            DropIndex("dbo.PostponementRequests", new[] { "TutorId" });
            DropIndex("dbo.PostponementRequests", new[] { "StudentId" });
            DropIndex("dbo.PostponementRequests", new[] { "BookingId" });
            DropIndex("dbo.PasswordResetTokens", new[] { "RequestId" });
            DropIndex("dbo.PasswordResetTokens", new[] { "TokenHash" });
            DropIndex("dbo.PasswordResetTokens", new[] { "UserId" });
            DropIndex("dbo.PasswordResetRequests", new[] { "User_UserId" });
            DropIndex("dbo.PasswordResetRequests", new[] { "HandledByUserId" });
            DropIndex("dbo.Notifications", "IX_Notification_UserUnread");
            DropIndex("dbo.Sales", new[] { "CopyId" });
            DropIndex("dbo.Sales", new[] { "StudentId" });
            DropIndex("dbo.Sales", new[] { "Reference" });
            DropIndex("dbo.RentalRenewals", new[] { "RentalId" });
            DropIndex("dbo.Rentals", new[] { "ProcessedByUserId" });
            DropIndex("dbo.Rentals", new[] { "CopyId" });
            DropIndex("dbo.Rentals", new[] { "StudentId" });
            DropIndex("dbo.Rentals", new[] { "Reference" });
            DropIndex("dbo.Fines", new[] { "WaivedByUserId" });
            DropIndex("dbo.Fines", new[] { "CreatedByUserId" });
            DropIndex("dbo.Fines", new[] { "RentalId" });
            DropIndex("dbo.Payments", new[] { "ProcessedByUserId" });
            DropIndex("dbo.Payments", new[] { "RefundOfPaymentId" });
            DropIndex("dbo.Payments", new[] { "StudentId" });
            DropIndex("dbo.Payments", new[] { "Reference" });
            DropIndex("dbo.PaymentAllocations", new[] { "FeeId" });
            DropIndex("dbo.PaymentAllocations", new[] { "PaymentId" });
            DropIndex("dbo.Fees", new[] { "FineId" });
            DropIndex("dbo.Fees", new[] { "SaleId" });
            DropIndex("dbo.Fees", new[] { "RentalRenewalId" });
            DropIndex("dbo.Fees", new[] { "RentalId" });
            DropIndex("dbo.Fees", new[] { "BookingId" });
            DropIndex("dbo.Fees", new[] { "StudentId" });
            DropIndex("dbo.BookWaitlists", "UQ_BookWaitlist");
            DropIndex("dbo.Students", new[] { "StudyLevelId" });
            DropIndex("dbo.Students", new[] { "StudentNumber" });
            DropIndex("dbo.Students", new[] { "UserId" });
            DropIndex("dbo.Staff", new[] { "EmployeeNumber" });
            DropIndex("dbo.Staff", new[] { "UserId" });
            DropIndex("dbo.TutorVerifications", new[] { "ReviewedByStaffId" });
            DropIndex("dbo.TutorVerifications", new[] { "ReviewedByUserId" });
            DropIndex("dbo.TutorVerifications", new[] { "TutorId" });
            DropIndex("dbo.Tutors", new[] { "UserId" });
            DropIndex("dbo.StudyLevels", new[] { "Name" });
            DropIndex("dbo.TutorSubjects", "UQ_TutorSubject");
            DropIndex("dbo.TutorAvailability", new[] { "CampusId" });
            DropIndex("dbo.TutorAvailability", new[] { "TutorSubjectId" });
            DropIndex("dbo.Bookings", new[] { "CancelledByUserId" });
            DropIndex("dbo.Bookings", new[] { "CampusId" });
            DropIndex("dbo.Bookings", new[] { "AvailabilityId" });
            DropIndex("dbo.Bookings", new[] { "TutorSubjectId" });
            DropIndex("dbo.Bookings", new[] { "TutorId" });
            DropIndex("dbo.Bookings", new[] { "StudentId" });
            DropIndex("dbo.Bookings", new[] { "Reference" });
            DropIndex("dbo.SupplierOrderLines", new[] { "CampusId" });
            DropIndex("dbo.SupplierOrderLines", new[] { "BookId" });
            DropIndex("dbo.SupplierOrderLines", new[] { "OrderId" });
            DropIndex("dbo.SupplierOrders", new[] { "CreatedByManagerId" });
            DropIndex("dbo.SupplierOrders", new[] { "SupplierId" });
            DropIndex("dbo.SupplierOrders", new[] { "Reference" });
            DropIndex("dbo.Deliveries", new[] { "ReceivedByUserId" });
            DropIndex("dbo.Deliveries", new[] { "DispatchedByUserId" });
            DropIndex("dbo.Deliveries", new[] { "OrderId" });
            DropIndex("dbo.Deliveries", new[] { "Reference" });
            DropIndex("dbo.DeliveryLines", new[] { "OrderLineId" });
            DropIndex("dbo.DeliveryLines", new[] { "DeliveryId" });
            DropIndex("dbo.Subjects", new[] { "Name" });
            DropIndex("dbo.Suppliers", new[] { "UserId" });
            DropIndex("dbo.Managers", new[] { "EmployeeNumber" });
            DropIndex("dbo.Managers", new[] { "UserId" });
            DropIndex("dbo.Categories", new[] { "Name" });
            DropIndex("dbo.Books", new[] { "UpdatedByManagerId" });
            DropIndex("dbo.Books", new[] { "CreatedByManagerId" });
            DropIndex("dbo.Books", new[] { "PreferredSupplierId" });
            DropIndex("dbo.Books", new[] { "SubjectId" });
            DropIndex("dbo.Books", new[] { "CategoryId" });
            DropIndex("dbo.Books", new[] { "Isbn" });
            DropIndex("dbo.BookCopies", new[] { "DeliveryLineId" });
            DropIndex("dbo.BookCopies", new[] { "Barcode" });
            DropIndex("dbo.BookCopies", new[] { "CampusId" });
            DropIndex("dbo.BookCopies", new[] { "BookId" });
            DropIndex("dbo.BackupLogs", new[] { "RequestedByUserId" });
            DropIndex("dbo.AuditLogs", "IX_AuditLog_Created");
            DropIndex("dbo.AuditLogs", "IX_AuditLog_Entity");
            DropIndex("dbo.AuditLogs", "IX_AuditLog_Action");
            DropIndex("dbo.AuditLogs", new[] { "ActorUserId" });
            DropIndex("dbo.Roles", new[] { "Name" });
            DropIndex("dbo.UserRoles", new[] { "AssignedByUserId" });
            DropIndex("dbo.UserRoles", new[] { "RoleId" });
            DropIndex("dbo.UserRoles", new[] { "UserId" });
            DropIndex("dbo.Campuses", new[] { "Name" });
            DropIndex("dbo.Users", new[] { "CreatedByUserId" });
            DropIndex("dbo.Users", new[] { "CampusId" });
            DropIndex("dbo.Users", new[] { "Email" });
            DropIndex("dbo.Users", new[] { "UserName" });
            DropIndex("dbo.Admins", new[] { "UserId" });
            DropTable("dbo.TutorStats");
            DropTable("dbo.TutoringSessions");
            DropTable("dbo.Timesheets");
            DropTable("dbo.TimesheetEntries");
            DropTable("dbo.SystemSettings");
            DropTable("dbo.SupportMessages");
            DropTable("dbo.SupplierInvoices");
            DropTable("dbo.StudentSubjects");
            DropTable("dbo.Reviews");
            DropTable("dbo.ReportRuns");
            DropTable("dbo.ProfileUpdateRequests");
            DropTable("dbo.PostponementRequests");
            DropTable("dbo.PasswordResetTokens");
            DropTable("dbo.PasswordResetRequests");
            DropTable("dbo.Notifications");
            DropTable("dbo.Sales");
            DropTable("dbo.RentalRenewals");
            DropTable("dbo.Rentals");
            DropTable("dbo.Fines");
            DropTable("dbo.Payments");
            DropTable("dbo.PaymentAllocations");
            DropTable("dbo.Fees");
            DropTable("dbo.DailyMetrics");
            DropTable("dbo.BookWaitlists");
            DropTable("dbo.Students");
            DropTable("dbo.Staff");
            DropTable("dbo.TutorVerifications");
            DropTable("dbo.Tutors");
            DropTable("dbo.StudyLevels");
            DropTable("dbo.TutorSubjects");
            DropTable("dbo.TutorAvailability");
            DropTable("dbo.Bookings");
            DropTable("dbo.SupplierOrderLines");
            DropTable("dbo.SupplierOrders");
            DropTable("dbo.Deliveries");
            DropTable("dbo.DeliveryLines");
            DropTable("dbo.Subjects");
            DropTable("dbo.Suppliers");
            DropTable("dbo.Managers");
            DropTable("dbo.Categories");
            DropTable("dbo.Books");
            DropTable("dbo.BookCopies");
            DropTable("dbo.BackupLogs");
            DropTable("dbo.AuditLogs");
            DropTable("dbo.Roles");
            DropTable("dbo.UserRoles");
            DropTable("dbo.Campuses");
            DropTable("dbo.Users");
            DropTable("dbo.Admins");
        }
    }
}
