/* =============================================================================
   StudyLink - 01_CreateSchema.sql
   Books, Tutoring & Academic Resource Hub  |  Nova Tech Developers
   Target: Microsoft SQL Server 2016+ (developed on SQL Server 2022 Express)

   GENERATED from the EF6 migrations in src\StudyLink.Data\Migrations
   (ef6.exe database update --script). Do not hand-edit: change the model,
   add a migration, and regenerate. The web application applies the same
   migrations automatically on first start, so running this script is only
   needed for a DBA-controlled install.

   Run:  sqlcmd -S .\SQLEXPRESS -E -i database\01_CreateSchema.sql
         then database\02_SeedLookups.sql

   Enum columns are INT; the values are documented in
   src\StudyLink.Data\Entities\Enums.cs (e.g. Bookings.Status 1 = AwaitingPayment,
   2 = AwaitingTutor, 3 = Confirmed, 4 = Completed, 5 = Cancelled, 6 = Declined,
   7 = Expired).
   ============================================================================= */
SET NOCOUNT ON;
-- Filtered indexes require QUOTED_IDENTIFIER ON; sqlcmd defaults it OFF.
SET QUOTED_IDENTIFIER ON;
GO
IF DB_ID(N'StudyLink') IS NULL
    EXEC (N'CREATE DATABASE StudyLink;');
GO
USE StudyLink;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[Admins] (
    [AdminId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    CONSTRAINT [PK_dbo.Admins] PRIMARY KEY ([AdminId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Admins]([UserId])
CREATE TABLE [dbo].[Users] (
    [UserId] [int] NOT NULL IDENTITY,
    [UserName] [nvarchar](50) NOT NULL,
    [Email] [nvarchar](100) NOT NULL,
    [FirstName] [nvarchar](50) NOT NULL,
    [LastName] [nvarchar](50) NOT NULL,
    [Gender] [int] NOT NULL,
    [CellNumber] [nvarchar](15) NOT NULL,
    [CampusId] [int] NOT NULL,
    [PasswordHash] [varbinary](64) NOT NULL,
    [PasswordSalt] [varbinary](32) NOT NULL,
    [HashIterations] [int] NOT NULL,
    [IsActive] [bit] NOT NULL,
    [FailedLoginCount] [int] NOT NULL,
    [LockoutEnd] [datetime2](0),
    [MustChangePassword] [bit] NOT NULL,
    [LastLoginAt] [datetime2](0),
    [CreatedAt] [datetime2](0) NOT NULL,
    [UpdatedAt] [datetime2](0),
    [CreatedByUserId] [int],
    CONSTRAINT [PK_dbo.Users] PRIMARY KEY ([UserId])
)
CREATE UNIQUE INDEX [IX_UserName] ON [dbo].[Users]([UserName])
CREATE UNIQUE INDEX [IX_Email] ON [dbo].[Users]([Email])
CREATE INDEX [IX_CampusId] ON [dbo].[Users]([CampusId])
CREATE INDEX [IX_CreatedByUserId] ON [dbo].[Users]([CreatedByUserId])
CREATE TABLE [dbo].[Campuses] (
    [CampusId] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](50) NOT NULL,
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_dbo.Campuses] PRIMARY KEY ([CampusId])
)
CREATE UNIQUE INDEX [IX_Name] ON [dbo].[Campuses]([Name])
CREATE TABLE [dbo].[UserRoles] (
    [UserId] [int] NOT NULL,
    [RoleId] [int] NOT NULL,
    [AssignedAt] [datetime2](0) NOT NULL,
    [AssignedByUserId] [int],
    CONSTRAINT [PK_dbo.UserRoles] PRIMARY KEY ([UserId], [RoleId])
)
CREATE INDEX [IX_UserId] ON [dbo].[UserRoles]([UserId])
CREATE INDEX [IX_RoleId] ON [dbo].[UserRoles]([RoleId])
CREATE INDEX [IX_AssignedByUserId] ON [dbo].[UserRoles]([AssignedByUserId])
CREATE TABLE [dbo].[Roles] (
    [RoleId] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](30) NOT NULL,
    [Description] [nvarchar](200),
    CONSTRAINT [PK_dbo.Roles] PRIMARY KEY ([RoleId])
)
CREATE UNIQUE INDEX [IX_Name] ON [dbo].[Roles]([Name])
CREATE TABLE [dbo].[AuditLogs] (
    [AuditId] [bigint] NOT NULL IDENTITY,
    [ActorUserId] [int],
    [ActorRole] [nvarchar](30) NOT NULL,
    [Action] [nvarchar](50) NOT NULL,
    [EntityType] [nvarchar](40) NOT NULL,
    [EntityId] [nvarchar](40) NOT NULL,
    [PreviousState] [nvarchar](1000),
    [NewState] [nvarchar](1000),
    [Details] [nvarchar](500),
    [IpAddress] [nvarchar](45),
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.AuditLogs] PRIMARY KEY ([AuditId])
)
CREATE INDEX [IX_ActorUserId] ON [dbo].[AuditLogs]([ActorUserId])
CREATE INDEX [IX_AuditLog_Action] ON [dbo].[AuditLogs]([Action])
CREATE INDEX [IX_AuditLog_Entity] ON [dbo].[AuditLogs]([EntityType], [EntityId])
CREATE INDEX [IX_AuditLog_Created] ON [dbo].[AuditLogs]([CreatedAt])
CREATE TABLE [dbo].[BackupLogs] (
    [BackupId] [int] NOT NULL IDENTITY,
    [FilePath] [nvarchar](400) NOT NULL,
    [Status] [int] NOT NULL,
    [StartedAt] [datetime2](0) NOT NULL,
    [CompletedAt] [datetime2](0),
    [SizeBytes] [bigint],
    [RequestedByUserId] [int] NOT NULL,
    [Message] [nvarchar](500),
    CONSTRAINT [PK_dbo.BackupLogs] PRIMARY KEY ([BackupId])
)
CREATE INDEX [IX_RequestedByUserId] ON [dbo].[BackupLogs]([RequestedByUserId])
CREATE TABLE [dbo].[BookCopies] (
    [CopyId] [int] NOT NULL IDENTITY,
    [BookId] [int] NOT NULL,
    [CampusId] [int] NOT NULL,
    [Barcode] [nvarchar](30) NOT NULL,
    [Condition] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [AcquiredAt] [datetime2](0) NOT NULL,
    [DeliveryLineId] [int],
    [Notes] [nvarchar](200),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_dbo.BookCopies] PRIMARY KEY ([CopyId])
)
CREATE INDEX [IX_BookId] ON [dbo].[BookCopies]([BookId])
CREATE INDEX [IX_CampusId] ON [dbo].[BookCopies]([CampusId])
CREATE UNIQUE INDEX [IX_Barcode] ON [dbo].[BookCopies]([Barcode])
CREATE INDEX [IX_DeliveryLineId] ON [dbo].[BookCopies]([DeliveryLineId])
CREATE TABLE [dbo].[Books] (
    [BookId] [int] NOT NULL IDENTITY,
    [Isbn] [nvarchar](20) NOT NULL,
    [Title] [nvarchar](200) NOT NULL,
    [Authors] [nvarchar](200) NOT NULL,
    [Edition] [nvarchar](30),
    [Publisher] [nvarchar](100),
    [PublishedYear] [int],
    [CategoryId] [int] NOT NULL,
    [SubjectId] [int],
    [Description] [nvarchar](1000),
    [CoverImage] [varbinary](max),
    [CoverContentType] [nvarchar](50),
    [SalePrice] [decimal](12, 2) NOT NULL,
    [WeeklyRentalFee] [decimal](12, 2) NOT NULL,
    [LateFeePerDay] [decimal](12, 2) NOT NULL,
    [ReorderLevel] [int] NOT NULL,
    [IsRentable] [bit] NOT NULL,
    [IsForSale] [bit] NOT NULL,
    [IsActive] [bit] NOT NULL,
    [PreferredSupplierId] [int],
    [CreatedByManagerId] [int],
    [UpdatedByManagerId] [int],
    [CreatedAt] [datetime2](0) NOT NULL,
    [UpdatedAt] [datetime2](0),
    CONSTRAINT [PK_dbo.Books] PRIMARY KEY ([BookId])
)
CREATE UNIQUE INDEX [IX_Isbn] ON [dbo].[Books]([Isbn])
CREATE INDEX [IX_CategoryId] ON [dbo].[Books]([CategoryId])
CREATE INDEX [IX_SubjectId] ON [dbo].[Books]([SubjectId])
CREATE INDEX [IX_PreferredSupplierId] ON [dbo].[Books]([PreferredSupplierId])
CREATE INDEX [IX_CreatedByManagerId] ON [dbo].[Books]([CreatedByManagerId])
CREATE INDEX [IX_UpdatedByManagerId] ON [dbo].[Books]([UpdatedByManagerId])
CREATE TABLE [dbo].[Categories] (
    [CategoryId] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](80) NOT NULL,
    [Description] [nvarchar](300),
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_dbo.Categories] PRIMARY KEY ([CategoryId])
)
CREATE UNIQUE INDEX [IX_Name] ON [dbo].[Categories]([Name])
CREATE TABLE [dbo].[Managers] (
    [ManagerId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [EmployeeNumber] [nvarchar](20) NOT NULL,
    CONSTRAINT [PK_dbo.Managers] PRIMARY KEY ([ManagerId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Managers]([UserId])
CREATE UNIQUE INDEX [IX_EmployeeNumber] ON [dbo].[Managers]([EmployeeNumber])
CREATE TABLE [dbo].[Suppliers] (
    [SupplierId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [CompanyName] [nvarchar](120) NOT NULL,
    [ContactPerson] [nvarchar](100) NOT NULL,
    [VatNumber] [nvarchar](20),
    [Phone] [nvarchar](20) NOT NULL,
    [Address] [nvarchar](250),
    CONSTRAINT [PK_dbo.Suppliers] PRIMARY KEY ([SupplierId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Suppliers]([UserId])
CREATE TABLE [dbo].[Subjects] (
    [SubjectId] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](100) NOT NULL,
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_dbo.Subjects] PRIMARY KEY ([SubjectId])
)
CREATE UNIQUE INDEX [IX_Name] ON [dbo].[Subjects]([Name])
CREATE TABLE [dbo].[DeliveryLines] (
    [DeliveryLineId] [int] NOT NULL IDENTITY,
    [DeliveryId] [int] NOT NULL,
    [OrderLineId] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [QuantityAccepted] [int],
    CONSTRAINT [PK_dbo.DeliveryLines] PRIMARY KEY ([DeliveryLineId])
)
CREATE INDEX [IX_DeliveryId] ON [dbo].[DeliveryLines]([DeliveryId])
CREATE INDEX [IX_OrderLineId] ON [dbo].[DeliveryLines]([OrderLineId])
CREATE TABLE [dbo].[Deliveries] (
    [DeliveryId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](20) NOT NULL,
    [OrderId] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [DispatchedAt] [datetime2](0) NOT NULL,
    [DispatchedByUserId] [int] NOT NULL,
    [ReceivedAt] [datetime2](0),
    [ReceivedByUserId] [int],
    [Note] [nvarchar](300),
    CONSTRAINT [PK_dbo.Deliveries] PRIMARY KEY ([DeliveryId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[Deliveries]([Reference])
CREATE INDEX [IX_OrderId] ON [dbo].[Deliveries]([OrderId])
CREATE INDEX [IX_DispatchedByUserId] ON [dbo].[Deliveries]([DispatchedByUserId])
CREATE INDEX [IX_ReceivedByUserId] ON [dbo].[Deliveries]([ReceivedByUserId])
CREATE TABLE [dbo].[SupplierOrders] (
    [OrderId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](20) NOT NULL,
    [SupplierId] [int] NOT NULL,
    [CreatedByManagerId] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [OrderedAt] [datetime2](0) NOT NULL,
    [ExpectedDate] [date],
    [AcknowledgedAt] [datetime2](0),
    [Notes] [nvarchar](500),
    CONSTRAINT [PK_dbo.SupplierOrders] PRIMARY KEY ([OrderId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[SupplierOrders]([Reference])
CREATE INDEX [IX_SupplierId] ON [dbo].[SupplierOrders]([SupplierId])
CREATE INDEX [IX_CreatedByManagerId] ON [dbo].[SupplierOrders]([CreatedByManagerId])
CREATE TABLE [dbo].[SupplierOrderLines] (
    [LineId] [int] NOT NULL IDENTITY,
    [OrderId] [int] NOT NULL,
    [BookId] [int] NOT NULL,
    [CampusId] [int] NOT NULL,
    [QuantityOrdered] [int] NOT NULL,
    [QuantityDelivered] [int] NOT NULL,
    [UnitCost] [decimal](12, 2) NOT NULL,
    CONSTRAINT [PK_dbo.SupplierOrderLines] PRIMARY KEY ([LineId])
)
CREATE INDEX [IX_OrderId] ON [dbo].[SupplierOrderLines]([OrderId])
CREATE INDEX [IX_BookId] ON [dbo].[SupplierOrderLines]([BookId])
CREATE INDEX [IX_CampusId] ON [dbo].[SupplierOrderLines]([CampusId])
CREATE TABLE [dbo].[Bookings] (
    [BookingId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](20) NOT NULL,
    [StudentId] [int] NOT NULL,
    [TutorId] [int] NOT NULL,
    [TutorSubjectId] [int] NOT NULL,
    [AvailabilityId] [int] NOT NULL,
    [CampusId] [int] NOT NULL,
    [SessionDate] [date] NOT NULL,
    [StartTime] [time](0) NOT NULL,
    [EndTime] [time](0) NOT NULL,
    [Fee] [decimal](12, 2) NOT NULL,
    [Status] [int] NOT NULL,
    [StudentMessage] [nvarchar](500),
    [HoldExpiresAt] [datetime2](0),
    [PaidAt] [datetime2](0),
    [TutorRespondedAt] [datetime2](0),
    [DeclineReason] [nvarchar](200),
    [CancelledAt] [datetime2](0),
    [CancelledByUserId] [int],
    [CancelReasonCategory] [int],
    [CancelReason] [nvarchar](200),
    [CompletedAt] [datetime2](0),
    [CreatedAt] [datetime2](0) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_dbo.Bookings] PRIMARY KEY ([BookingId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[Bookings]([Reference])
CREATE INDEX [IX_StudentId] ON [dbo].[Bookings]([StudentId])
CREATE INDEX [IX_TutorId] ON [dbo].[Bookings]([TutorId])
CREATE INDEX [IX_TutorSubjectId] ON [dbo].[Bookings]([TutorSubjectId])
CREATE INDEX [IX_AvailabilityId] ON [dbo].[Bookings]([AvailabilityId])
CREATE INDEX [IX_CampusId] ON [dbo].[Bookings]([CampusId])
CREATE INDEX [IX_CancelledByUserId] ON [dbo].[Bookings]([CancelledByUserId])
CREATE TABLE [dbo].[TutorAvailability] (
    [AvailabilityId] [int] NOT NULL IDENTITY,
    [TutorSubjectId] [int] NOT NULL,
    [CampusId] [int] NOT NULL,
    [AvailabilityDate] [date] NOT NULL,
    [StartTime] [time](0) NOT NULL,
    [EndTime] [time](0) NOT NULL,
    [IsBooked] [bit] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_dbo.TutorAvailability] PRIMARY KEY ([AvailabilityId])
)
CREATE INDEX [IX_TutorSubjectId] ON [dbo].[TutorAvailability]([TutorSubjectId])
CREATE INDEX [IX_CampusId] ON [dbo].[TutorAvailability]([CampusId])
CREATE TABLE [dbo].[TutorSubjects] (
    [TutorSubjectId] [int] NOT NULL IDENTITY,
    [TutorId] [int] NOT NULL,
    [SubjectId] [int] NOT NULL,
    [StudyLevelId] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.TutorSubjects] PRIMARY KEY ([TutorSubjectId])
)
CREATE UNIQUE INDEX [UQ_TutorSubject] ON [dbo].[TutorSubjects]([TutorId], [SubjectId], [StudyLevelId])
CREATE TABLE [dbo].[StudyLevels] (
    [StudyLevelId] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](50) NOT NULL,
    [SortOrder] [int] NOT NULL,
    CONSTRAINT [PK_dbo.StudyLevels] PRIMARY KEY ([StudyLevelId])
)
CREATE UNIQUE INDEX [IX_Name] ON [dbo].[StudyLevels]([Name])
CREATE TABLE [dbo].[Tutors] (
    [TutorId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [Qualification] [nvarchar](100) NOT NULL,
    [ExperienceYears] [int] NOT NULL,
    [HourlyRate] [decimal](12, 2) NOT NULL,
    [Bio] [nvarchar](1000),
    [VerificationStatus] [int] NOT NULL,
    [VerifiedAt] [datetime2](0),
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.Tutors] PRIMARY KEY ([TutorId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Tutors]([UserId])
CREATE TABLE [dbo].[TutorVerifications] (
    [VerificationId] [int] NOT NULL IDENTITY,
    [TutorId] [int] NOT NULL,
    [SubmittedAt] [datetime2](0) NOT NULL,
    [Decision] [int] NOT NULL,
    [ReviewedByUserId] [int],
    [ReviewedByStaffId] [int],
    [ReviewedAt] [datetime2](0),
    [IdentityChecked] [bit] NOT NULL,
    [QualificationChecked] [bit] NOT NULL,
    [Notes] [nvarchar](500),
    CONSTRAINT [PK_dbo.TutorVerifications] PRIMARY KEY ([VerificationId])
)
CREATE INDEX [IX_TutorId] ON [dbo].[TutorVerifications]([TutorId])
CREATE INDEX [IX_ReviewedByUserId] ON [dbo].[TutorVerifications]([ReviewedByUserId])
CREATE INDEX [IX_ReviewedByStaffId] ON [dbo].[TutorVerifications]([ReviewedByStaffId])
CREATE TABLE [dbo].[Staff] (
    [StaffId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [EmployeeNumber] [nvarchar](20) NOT NULL,
    [JobTitle] [nvarchar](60),
    CONSTRAINT [PK_dbo.Staff] PRIMARY KEY ([StaffId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Staff]([UserId])
CREATE UNIQUE INDEX [IX_EmployeeNumber] ON [dbo].[Staff]([EmployeeNumber])
CREATE TABLE [dbo].[Students] (
    [StudentId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [StudentNumber] [nvarchar](20) NOT NULL,
    [StudyLevelId] [int],
    [Faculty] [nvarchar](100),
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.Students] PRIMARY KEY ([StudentId])
)
CREATE UNIQUE INDEX [IX_UserId] ON [dbo].[Students]([UserId])
CREATE UNIQUE INDEX [IX_StudentNumber] ON [dbo].[Students]([StudentNumber])
CREATE INDEX [IX_StudyLevelId] ON [dbo].[Students]([StudyLevelId])
CREATE TABLE [dbo].[BookWaitlists] (
    [WaitlistId] [int] NOT NULL IDENTITY,
    [BookId] [int] NOT NULL,
    [StudentId] [int] NOT NULL,
    [CampusId] [int] NOT NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [NotifiedAt] [datetime2](0),
    CONSTRAINT [PK_dbo.BookWaitlists] PRIMARY KEY ([WaitlistId])
)
CREATE UNIQUE INDEX [UQ_BookWaitlist] ON [dbo].[BookWaitlists]([BookId], [StudentId], [CampusId])
CREATE TABLE [dbo].[DailyMetrics] (
    [MetricDate] [date] NOT NULL,
    [Revenue] [decimal](12, 2) NOT NULL,
    [Refunds] [decimal](12, 2) NOT NULL,
    [PaymentsCount] [int] NOT NULL,
    [BookingsCreated] [int] NOT NULL,
    [SessionsCompleted] [int] NOT NULL,
    [RentalsStarted] [int] NOT NULL,
    [RentalsReturned] [int] NOT NULL,
    [OverdueRentals] [int] NOT NULL,
    [BooksSold] [int] NOT NULL,
    [FinesIssued] [decimal](12, 2) NOT NULL,
    [NewStudents] [int] NOT NULL,
    [NewTutors] [int] NOT NULL,
    [CapturedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.DailyMetrics] PRIMARY KEY ([MetricDate])
)
CREATE TABLE [dbo].[Fees] (
    [FeeId] [int] NOT NULL IDENTITY,
    [StudentId] [int] NOT NULL,
    [Type] [int] NOT NULL,
    [Description] [nvarchar](200) NOT NULL,
    [Amount] [decimal](12, 2) NOT NULL,
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [SettledAt] [datetime2](0),
    [BookingId] [int],
    [RentalId] [int],
    [RentalRenewalId] [int],
    [SaleId] [int],
    [FineId] [int],
    CONSTRAINT [PK_dbo.Fees] PRIMARY KEY ([FeeId])
)
CREATE INDEX [IX_StudentId] ON [dbo].[Fees]([StudentId])
CREATE INDEX [IX_BookingId] ON [dbo].[Fees]([BookingId])
CREATE INDEX [IX_RentalId] ON [dbo].[Fees]([RentalId])
CREATE INDEX [IX_RentalRenewalId] ON [dbo].[Fees]([RentalRenewalId])
CREATE INDEX [IX_SaleId] ON [dbo].[Fees]([SaleId])
CREATE INDEX [IX_FineId] ON [dbo].[Fees]([FineId])
CREATE TABLE [dbo].[PaymentAllocations] (
    [AllocationId] [int] NOT NULL IDENTITY,
    [PaymentId] [int] NOT NULL,
    [FeeId] [int] NOT NULL,
    [Amount] [decimal](12, 2) NOT NULL,
    CONSTRAINT [PK_dbo.PaymentAllocations] PRIMARY KEY ([AllocationId])
)
CREATE INDEX [IX_PaymentId] ON [dbo].[PaymentAllocations]([PaymentId])
CREATE INDEX [IX_FeeId] ON [dbo].[PaymentAllocations]([FeeId])
CREATE TABLE [dbo].[Payments] (
    [PaymentId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](30) NOT NULL,
    [StudentId] [int] NOT NULL,
    [Type] [int] NOT NULL,
    [Method] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [Amount] [decimal](12, 2) NOT NULL,
    [CardBrand] [nvarchar](20),
    [CardLast4] [nvarchar](4),
    [GatewayReference] [nvarchar](50),
    [FailureReason] [nvarchar](200),
    [CheckoutToken] [uniqueidentifier],
    [RefundOfPaymentId] [int],
    [Note] [nvarchar](200),
    [CreatedAt] [datetime2](0) NOT NULL,
    [CompletedAt] [datetime2](0),
    [ProcessedByUserId] [int],
    CONSTRAINT [PK_dbo.Payments] PRIMARY KEY ([PaymentId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[Payments]([Reference])
CREATE INDEX [IX_StudentId] ON [dbo].[Payments]([StudentId])
CREATE INDEX [IX_RefundOfPaymentId] ON [dbo].[Payments]([RefundOfPaymentId])
CREATE INDEX [IX_ProcessedByUserId] ON [dbo].[Payments]([ProcessedByUserId])
CREATE TABLE [dbo].[Fines] (
    [FineId] [int] NOT NULL IDENTITY,
    [RentalId] [int] NOT NULL,
    [Type] [int] NOT NULL,
    [Amount] [decimal](12, 2) NOT NULL,
    [DaysLate] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [Reason] [nvarchar](200),
    [CreatedAt] [datetime2](0) NOT NULL,
    [CreatedByUserId] [int],
    [WaivedByUserId] [int],
    [WaivedAt] [datetime2](0),
    [WaiverReason] [nvarchar](200),
    CONSTRAINT [PK_dbo.Fines] PRIMARY KEY ([FineId])
)
CREATE INDEX [IX_RentalId] ON [dbo].[Fines]([RentalId])
CREATE INDEX [IX_CreatedByUserId] ON [dbo].[Fines]([CreatedByUserId])
CREATE INDEX [IX_WaivedByUserId] ON [dbo].[Fines]([WaivedByUserId])
CREATE TABLE [dbo].[Rentals] (
    [RentalId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](20) NOT NULL,
    [StudentId] [int] NOT NULL,
    [CopyId] [int] NOT NULL,
    [RentedAt] [datetime2](0) NOT NULL,
    [DueDate] [date] NOT NULL,
    [ReturnedAt] [datetime2](0),
    [Status] [int] NOT NULL,
    [Fee] [decimal](12, 2) NOT NULL,
    [RenewalCount] [int] NOT NULL,
    [HoldExpiresAt] [datetime2](0),
    [ReturnCondition] [int],
    [ProcessedByUserId] [int],
    [CreatedAt] [datetime2](0) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_dbo.Rentals] PRIMARY KEY ([RentalId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[Rentals]([Reference])
CREATE INDEX [IX_StudentId] ON [dbo].[Rentals]([StudentId])
CREATE INDEX [IX_CopyId] ON [dbo].[Rentals]([CopyId])
CREATE INDEX [IX_ProcessedByUserId] ON [dbo].[Rentals]([ProcessedByUserId])
CREATE TABLE [dbo].[RentalRenewals] (
    [RenewalId] [int] NOT NULL IDENTITY,
    [RentalId] [int] NOT NULL,
    [RenewedAt] [datetime2](0) NOT NULL,
    [PreviousDueDate] [date] NOT NULL,
    [NewDueDate] [date] NOT NULL,
    [Fee] [decimal](12, 2) NOT NULL,
    CONSTRAINT [PK_dbo.RentalRenewals] PRIMARY KEY ([RenewalId])
)
CREATE INDEX [IX_RentalId] ON [dbo].[RentalRenewals]([RentalId])
CREATE TABLE [dbo].[Sales] (
    [SaleId] [int] NOT NULL IDENTITY,
    [Reference] [nvarchar](20) NOT NULL,
    [StudentId] [int] NOT NULL,
    [CopyId] [int] NOT NULL,
    [Price] [decimal](12, 2) NOT NULL,
    [Status] [int] NOT NULL,
    [HoldExpiresAt] [datetime2](0),
    [CreatedAt] [datetime2](0) NOT NULL,
    [CompletedAt] [datetime2](0),
    CONSTRAINT [PK_dbo.Sales] PRIMARY KEY ([SaleId])
)
CREATE UNIQUE INDEX [IX_Reference] ON [dbo].[Sales]([Reference])
CREATE INDEX [IX_StudentId] ON [dbo].[Sales]([StudentId])
CREATE INDEX [IX_CopyId] ON [dbo].[Sales]([CopyId])
CREATE TABLE [dbo].[Notifications] (
    [NotificationId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [Title] [nvarchar](150) NOT NULL,
    [Message] [nvarchar](500) NOT NULL,
    [Type] [int] NOT NULL,
    [LinkUrl] [nvarchar](300),
    [IsRead] [bit] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [DedupKey] [nvarchar](100),
    CONSTRAINT [PK_dbo.Notifications] PRIMARY KEY ([NotificationId])
)
CREATE INDEX [IX_Notification_UserUnread] ON [dbo].[Notifications]([UserId], [IsRead])
CREATE TABLE [dbo].[PasswordResetRequests] (
    [RequestId] [int] NOT NULL IDENTITY,
    [UserId] [int],
    [Identifier] [nvarchar](100) NOT NULL,
    [Message] [nvarchar](300),
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [HandledByUserId] [int],
    [HandledAt] [datetime2](0),
    [ResponseNote] [nvarchar](200),
    [RequestIp] [nvarchar](45),
    [User_UserId] [int],
    CONSTRAINT [PK_dbo.PasswordResetRequests] PRIMARY KEY ([RequestId])
)
CREATE INDEX [IX_HandledByUserId] ON [dbo].[PasswordResetRequests]([HandledByUserId])
CREATE INDEX [IX_User_UserId] ON [dbo].[PasswordResetRequests]([User_UserId])
CREATE TABLE [dbo].[PasswordResetTokens] (
    [TokenId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [TokenHash] [varbinary](32) NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [ExpiresAt] [datetime2](0) NOT NULL,
    [UsedAt] [datetime2](0),
    [RequestId] [int],
    CONSTRAINT [PK_dbo.PasswordResetTokens] PRIMARY KEY ([TokenId])
)
CREATE INDEX [IX_UserId] ON [dbo].[PasswordResetTokens]([UserId])
CREATE UNIQUE INDEX [IX_TokenHash] ON [dbo].[PasswordResetTokens]([TokenHash])
CREATE INDEX [IX_RequestId] ON [dbo].[PasswordResetTokens]([RequestId])
CREATE TABLE [dbo].[PostponementRequests] (
    [PostponementId] [int] NOT NULL IDENTITY,
    [BookingId] [int] NOT NULL,
    [StudentId] [int] NOT NULL,
    [TutorId] [int] NOT NULL,
    [OriginalDate] [date] NOT NULL,
    [OriginalStart] [time](0) NOT NULL,
    [OriginalEnd] [time](0) NOT NULL,
    [ProposedDate] [date] NOT NULL,
    [ProposedStart] [time](0) NOT NULL,
    [ProposedEnd] [time](0) NOT NULL,
    [ReasonCategory] [int] NOT NULL,
    [StudentNote] [nvarchar](200),
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [TutorResponseNote] [nvarchar](200),
    [RespondedAt] [datetime2](0),
    CONSTRAINT [PK_dbo.PostponementRequests] PRIMARY KEY ([PostponementId])
)
CREATE INDEX [IX_BookingId] ON [dbo].[PostponementRequests]([BookingId])
CREATE INDEX [IX_StudentId] ON [dbo].[PostponementRequests]([StudentId])
CREATE INDEX [IX_TutorId] ON [dbo].[PostponementRequests]([TutorId])
CREATE TABLE [dbo].[ProfileUpdateRequests] (
    [RequestId] [int] NOT NULL IDENTITY,
    [UserId] [int] NOT NULL,
    [FieldName] [nvarchar](50) NOT NULL,
    [CurrentValue] [nvarchar](200),
    [RequestedValue] [nvarchar](200) NOT NULL,
    [Reason] [nvarchar](300),
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [HandledByUserId] [int],
    [HandledAt] [datetime2](0),
    [ResponseNote] [nvarchar](200),
    [User_UserId] [int],
    CONSTRAINT [PK_dbo.ProfileUpdateRequests] PRIMARY KEY ([RequestId])
)
CREATE INDEX [IX_HandledByUserId] ON [dbo].[ProfileUpdateRequests]([HandledByUserId])
CREATE INDEX [IX_User_UserId] ON [dbo].[ProfileUpdateRequests]([User_UserId])
CREATE TABLE [dbo].[ReportRuns] (
    [ReportRunId] [int] NOT NULL IDENTITY,
    [ReportKey] [nvarchar](50) NOT NULL,
    [Parameters] [nvarchar](300),
    [Format] [nvarchar](10),
    [RowCount] [int] NOT NULL,
    [GeneratedByUserId] [int] NOT NULL,
    [GeneratedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.ReportRuns] PRIMARY KEY ([ReportRunId])
)
CREATE INDEX [IX_GeneratedByUserId] ON [dbo].[ReportRuns]([GeneratedByUserId])
CREATE TABLE [dbo].[Reviews] (
    [ReviewId] [int] NOT NULL IDENTITY,
    [BookingId] [int] NOT NULL,
    [Rating] [tinyint] NOT NULL,
    [Comment] [nvarchar](500),
    [CreatedAt] [datetime2](0) NOT NULL,
    [IsHidden] [bit] NOT NULL,
    [HiddenReason] [nvarchar](200),
    [HiddenAt] [datetime2](0),
    [HiddenByUserId] [int],
    CONSTRAINT [PK_dbo.Reviews] PRIMARY KEY ([ReviewId])
)
CREATE UNIQUE INDEX [IX_BookingId] ON [dbo].[Reviews]([BookingId])
CREATE INDEX [IX_HiddenByUserId] ON [dbo].[Reviews]([HiddenByUserId])
CREATE TABLE [dbo].[StudentSubjects] (
    [StudentSubjectId] [int] NOT NULL IDENTITY,
    [StudentId] [int] NOT NULL,
    [SubjectId] [int] NOT NULL,
    [StudyLevelId] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.StudentSubjects] PRIMARY KEY ([StudentSubjectId])
)
CREATE UNIQUE INDEX [UQ_StudentSubject] ON [dbo].[StudentSubjects]([StudentId], [SubjectId], [StudyLevelId])
CREATE TABLE [dbo].[SupplierInvoices] (
    [InvoiceId] [int] NOT NULL IDENTITY,
    [SupplierId] [int] NOT NULL,
    [OrderId] [int] NOT NULL,
    [InvoiceNumber] [nvarchar](40) NOT NULL,
    [Amount] [decimal](12, 2) NOT NULL,
    [OriginalFileName] [nvarchar](200) NOT NULL,
    [StoredFileName] [nvarchar](100) NOT NULL,
    [UploadedAt] [datetime2](0) NOT NULL,
    [Status] [int] NOT NULL,
    [ReviewedByUserId] [int],
    [ReviewedAt] [datetime2](0),
    [PaidAt] [datetime2](0),
    [Note] [nvarchar](300),
    CONSTRAINT [PK_dbo.SupplierInvoices] PRIMARY KEY ([InvoiceId])
)
CREATE UNIQUE INDEX [UQ_SupplierInvoice_Number] ON [dbo].[SupplierInvoices]([SupplierId], [InvoiceNumber])
CREATE INDEX [IX_OrderId] ON [dbo].[SupplierInvoices]([OrderId])
CREATE INDEX [IX_ReviewedByUserId] ON [dbo].[SupplierInvoices]([ReviewedByUserId])
CREATE TABLE [dbo].[SupportMessages] (
    [MessageId] [int] NOT NULL IDENTITY,
    [UserId] [int],
    [Name] [nvarchar](100) NOT NULL,
    [Email] [nvarchar](100) NOT NULL,
    [CampusId] [int],
    [Subject] [nvarchar](150) NOT NULL,
    [Body] [nvarchar](2000) NOT NULL,
    [Status] [int] NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL,
    [HandledByUserId] [int],
    [HandledAt] [datetime2](0),
    [ResponseNote] [nvarchar](500),
    [User_UserId] [int],
    CONSTRAINT [PK_dbo.SupportMessages] PRIMARY KEY ([MessageId])
)
CREATE INDEX [IX_CampusId] ON [dbo].[SupportMessages]([CampusId])
CREATE INDEX [IX_HandledByUserId] ON [dbo].[SupportMessages]([HandledByUserId])
CREATE INDEX [IX_User_UserId] ON [dbo].[SupportMessages]([User_UserId])
CREATE TABLE [dbo].[SystemSettings] (
    [SettingKey] [nvarchar](60) NOT NULL,
    [Value] [nvarchar](200) NOT NULL,
    [Description] [nvarchar](300),
    [UpdatedAt] [datetime2](0) NOT NULL,
    [UpdatedByUserId] [int],
    CONSTRAINT [PK_dbo.SystemSettings] PRIMARY KEY ([SettingKey])
)
CREATE INDEX [IX_UpdatedByUserId] ON [dbo].[SystemSettings]([UpdatedByUserId])
CREATE TABLE [dbo].[TimesheetEntries] (
    [EntryId] [int] NOT NULL IDENTITY,
    [TimesheetId] [int] NOT NULL,
    [BookingId] [int],
    [WorkDate] [date] NOT NULL,
    [Hours] [decimal](12, 2) NOT NULL,
    [Description] [nvarchar](200) NOT NULL,
    CONSTRAINT [PK_dbo.TimesheetEntries] PRIMARY KEY ([EntryId])
)
CREATE INDEX [IX_TimesheetId] ON [dbo].[TimesheetEntries]([TimesheetId])
CREATE INDEX [IX_BookingId] ON [dbo].[TimesheetEntries]([BookingId])
CREATE TABLE [dbo].[Timesheets] (
    [TimesheetId] [int] NOT NULL IDENTITY,
    [TutorId] [int] NOT NULL,
    [WeekStart] [date] NOT NULL,
    [Status] [int] NOT NULL,
    [TotalHours] [decimal](12, 2) NOT NULL,
    [SubmittedAt] [datetime2](0),
    [ReviewedByUserId] [int],
    [ReviewedAt] [datetime2](0),
    [ReviewNote] [nvarchar](300),
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.Timesheets] PRIMARY KEY ([TimesheetId])
)
CREATE UNIQUE INDEX [UQ_Timesheet_TutorWeek] ON [dbo].[Timesheets]([TutorId], [WeekStart])
CREATE INDEX [IX_ReviewedByUserId] ON [dbo].[Timesheets]([ReviewedByUserId])
CREATE TABLE [dbo].[TutoringSessions] (
    [SessionId] [int] NOT NULL IDENTITY,
    [BookingId] [int] NOT NULL,
    [SessionDate] [date] NOT NULL,
    [Attendance] [int] NOT NULL,
    [HoursDelivered] [decimal](12, 2) NOT NULL,
    [TutorNotes] [nvarchar](1000),
    [CreatedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.TutoringSessions] PRIMARY KEY ([SessionId])
)
CREATE UNIQUE INDEX [IX_BookingId] ON [dbo].[TutoringSessions]([BookingId])
CREATE TABLE [dbo].[TutorStats] (
    [TutorId] [int] NOT NULL,
    [SessionsCompleted] [int] NOT NULL,
    [SessionsCancelled] [int] NOT NULL,
    [HoursDelivered] [decimal](12, 2) NOT NULL,
    [HoursPublished] [decimal](12, 2) NOT NULL,
    [UtilisationPercent] [decimal](12, 2) NOT NULL,
    [AverageRating] [decimal](12, 2),
    [ReviewCount] [int] NOT NULL,
    [GrossEarnings] [decimal](12, 2) NOT NULL,
    [RefreshedAt] [datetime2](0) NOT NULL,
    CONSTRAINT [PK_dbo.TutorStats] PRIMARY KEY ([TutorId])
)
CREATE INDEX [IX_TutorId] ON [dbo].[TutorStats]([TutorId])
CREATE UNIQUE INDEX UX_Bookings_ActiveSlot ON dbo.Bookings (AvailabilityId) WHERE Status IN (1, 2, 3);
CREATE INDEX IX_Bookings_Tutor_Date ON dbo.Bookings (TutorId, SessionDate, Status);
CREATE INDEX IX_Bookings_Student_Date ON dbo.Bookings (StudentId, SessionDate, Status);
CREATE UNIQUE INDEX UX_Rentals_ActiveCopy ON dbo.Rentals (CopyId) WHERE Status IN (1, 2);
CREATE UNIQUE INDEX UX_Sales_ActiveCopy ON dbo.Sales (CopyId) WHERE Status IN (1, 2);
CREATE INDEX IX_BookCopies_Book_Campus_Status ON dbo.BookCopies (BookId, CampusId, Status);
CREATE INDEX IX_Rentals_Status_Due ON dbo.Rentals (Status, DueDate);
CREATE UNIQUE INDEX UX_Postponement_OnePending ON dbo.PostponementRequests (BookingId) WHERE Status = 1;
CREATE UNIQUE INDEX UX_Payments_CheckoutToken ON dbo.Payments (CheckoutToken) WHERE CheckoutToken IS NOT NULL;
CREATE UNIQUE INDEX UX_Notifications_DedupKey ON dbo.Notifications (DedupKey) WHERE DedupKey IS NOT NULL;
CREATE INDEX IX_Fees_Student_Status ON dbo.Fees (StudentId, Status);
ALTER TABLE dbo.TutorAvailability ADD CONSTRAINT CK_TutorAvailability_Times CHECK (EndTime > StartTime);
ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_Times CHECK (EndTime > StartTime);
ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_Fee CHECK (Fee >= 0);
ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_CancelConsistency CHECK ((Status = 5 AND CancelledAt IS NOT NULL AND CancelReasonCategory IS NOT NULL) OR (Status <> 5 AND CancelledAt IS NULL AND CancelledByUserId IS NULL AND CancelReasonCategory IS NULL));
ALTER TABLE dbo.PostponementRequests ADD CONSTRAINT CK_Postponement_Times CHECK (ProposedEnd > ProposedStart);
ALTER TABLE dbo.PostponementRequests ADD CONSTRAINT CK_Postponement_Responded CHECK ((Status = 1 AND RespondedAt IS NULL) OR (Status <> 1 AND RespondedAt IS NOT NULL));
ALTER TABLE dbo.Reviews ADD CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5);
ALTER TABLE dbo.Tutors ADD CONSTRAINT CK_Tutors_Values CHECK (HourlyRate >= 0 AND ExperienceYears BETWEEN 0 AND 70);
ALTER TABLE dbo.Books ADD CONSTRAINT CK_Books_Prices CHECK (SalePrice >= 0 AND WeeklyRentalFee >= 0 AND LateFeePerDay >= 0 AND ReorderLevel >= 0);
ALTER TABLE dbo.Rentals ADD CONSTRAINT CK_Rentals_Values CHECK (Fee >= 0 AND RenewalCount >= 0);
ALTER TABLE dbo.Fines ADD CONSTRAINT CK_Fines_Amount CHECK (Amount >= 0 AND DaysLate >= 0);
ALTER TABLE dbo.Fees ADD CONSTRAINT CK_Fees_Amount CHECK (Amount >= 0);
ALTER TABLE dbo.Fees ADD CONSTRAINT CK_Fees_ExactlyOneSource CHECK ((CASE WHEN BookingId IS NULL THEN 0 ELSE 1 END + CASE WHEN RentalId IS NULL THEN 0 ELSE 1 END + CASE WHEN RentalRenewalId IS NULL THEN 0 ELSE 1 END + CASE WHEN SaleId IS NULL THEN 0 ELSE 1 END + CASE WHEN FineId IS NULL THEN 0 ELSE 1 END) = 1);
ALTER TABLE dbo.Payments ADD CONSTRAINT CK_Payments_Amount CHECK (Amount > 0);
ALTER TABLE dbo.PaymentAllocations ADD CONSTRAINT CK_PaymentAllocations_Amount CHECK (Amount > 0);
ALTER TABLE dbo.SupplierOrderLines ADD CONSTRAINT CK_OrderLines_Qty CHECK (QuantityOrdered > 0 AND QuantityDelivered >= 0 AND QuantityDelivered <= QuantityOrdered AND UnitCost >= 0);
ALTER TABLE dbo.DeliveryLines ADD CONSTRAINT CK_DeliveryLines_Qty CHECK (Quantity > 0 AND (QuantityAccepted IS NULL OR QuantityAccepted BETWEEN 0 AND Quantity));
ALTER TABLE dbo.TimesheetEntries ADD CONSTRAINT CK_TimesheetEntries_Hours CHECK (Hours > 0 AND Hours <= 24);
ALTER TABLE dbo.SupplierInvoices ADD CONSTRAINT CK_SupplierInvoices_Amount CHECK (Amount >= 0);
ALTER TABLE [dbo].[Admins] ADD CONSTRAINT [FK_dbo.Admins_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_dbo.Users_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_dbo.Users_dbo.Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_dbo.UserRoles_dbo.Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_dbo.UserRoles_dbo.Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId])
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_dbo.UserRoles_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[AuditLogs] ADD CONSTRAINT [FK_dbo.AuditLogs_dbo.Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[BackupLogs] ADD CONSTRAINT [FK_dbo.BackupLogs_dbo.Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[BookCopies] ADD CONSTRAINT [FK_dbo.BookCopies_dbo.Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId])
ALTER TABLE [dbo].[BookCopies] ADD CONSTRAINT [FK_dbo.BookCopies_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[BookCopies] ADD CONSTRAINT [FK_dbo.BookCopies_dbo.DeliveryLines_DeliveryLineId] FOREIGN KEY ([DeliveryLineId]) REFERENCES [dbo].[DeliveryLines] ([DeliveryLineId])
ALTER TABLE [dbo].[Books] ADD CONSTRAINT [FK_dbo.Books_dbo.Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([CategoryId])
ALTER TABLE [dbo].[Books] ADD CONSTRAINT [FK_dbo.Books_dbo.Managers_CreatedByManagerId] FOREIGN KEY ([CreatedByManagerId]) REFERENCES [dbo].[Managers] ([ManagerId])
ALTER TABLE [dbo].[Books] ADD CONSTRAINT [FK_dbo.Books_dbo.Suppliers_PreferredSupplierId] FOREIGN KEY ([PreferredSupplierId]) REFERENCES [dbo].[Suppliers] ([SupplierId])
ALTER TABLE [dbo].[Books] ADD CONSTRAINT [FK_dbo.Books_dbo.Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([SubjectId])
ALTER TABLE [dbo].[Books] ADD CONSTRAINT [FK_dbo.Books_dbo.Managers_UpdatedByManagerId] FOREIGN KEY ([UpdatedByManagerId]) REFERENCES [dbo].[Managers] ([ManagerId])
ALTER TABLE [dbo].[Managers] ADD CONSTRAINT [FK_dbo.Managers_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Suppliers] ADD CONSTRAINT [FK_dbo.Suppliers_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[DeliveryLines] ADD CONSTRAINT [FK_dbo.DeliveryLines_dbo.Deliveries_DeliveryId] FOREIGN KEY ([DeliveryId]) REFERENCES [dbo].[Deliveries] ([DeliveryId])
ALTER TABLE [dbo].[DeliveryLines] ADD CONSTRAINT [FK_dbo.DeliveryLines_dbo.SupplierOrderLines_OrderLineId] FOREIGN KEY ([OrderLineId]) REFERENCES [dbo].[SupplierOrderLines] ([LineId])
ALTER TABLE [dbo].[Deliveries] ADD CONSTRAINT [FK_dbo.Deliveries_dbo.Users_DispatchedByUserId] FOREIGN KEY ([DispatchedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Deliveries] ADD CONSTRAINT [FK_dbo.Deliveries_dbo.SupplierOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[SupplierOrders] ([OrderId])
ALTER TABLE [dbo].[Deliveries] ADD CONSTRAINT [FK_dbo.Deliveries_dbo.Users_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[SupplierOrders] ADD CONSTRAINT [FK_dbo.SupplierOrders_dbo.Managers_CreatedByManagerId] FOREIGN KEY ([CreatedByManagerId]) REFERENCES [dbo].[Managers] ([ManagerId])
ALTER TABLE [dbo].[SupplierOrders] ADD CONSTRAINT [FK_dbo.SupplierOrders_dbo.Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([SupplierId])
ALTER TABLE [dbo].[SupplierOrderLines] ADD CONSTRAINT [FK_dbo.SupplierOrderLines_dbo.Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId])
ALTER TABLE [dbo].[SupplierOrderLines] ADD CONSTRAINT [FK_dbo.SupplierOrderLines_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[SupplierOrderLines] ADD CONSTRAINT [FK_dbo.SupplierOrderLines_dbo.SupplierOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[SupplierOrders] ([OrderId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.TutorAvailability_AvailabilityId] FOREIGN KEY ([AvailabilityId]) REFERENCES [dbo].[TutorAvailability] ([AvailabilityId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.Users_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[Bookings] ADD CONSTRAINT [FK_dbo.Bookings_dbo.TutorSubjects_TutorSubjectId] FOREIGN KEY ([TutorSubjectId]) REFERENCES [dbo].[TutorSubjects] ([TutorSubjectId])
ALTER TABLE [dbo].[TutorAvailability] ADD CONSTRAINT [FK_dbo.TutorAvailability_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[TutorAvailability] ADD CONSTRAINT [FK_dbo.TutorAvailability_dbo.TutorSubjects_TutorSubjectId] FOREIGN KEY ([TutorSubjectId]) REFERENCES [dbo].[TutorSubjects] ([TutorSubjectId])
ALTER TABLE [dbo].[TutorSubjects] ADD CONSTRAINT [FK_dbo.TutorSubjects_dbo.StudyLevels_StudyLevelId] FOREIGN KEY ([StudyLevelId]) REFERENCES [dbo].[StudyLevels] ([StudyLevelId])
ALTER TABLE [dbo].[TutorSubjects] ADD CONSTRAINT [FK_dbo.TutorSubjects_dbo.Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([SubjectId])
ALTER TABLE [dbo].[TutorSubjects] ADD CONSTRAINT [FK_dbo.TutorSubjects_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[Tutors] ADD CONSTRAINT [FK_dbo.Tutors_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[TutorVerifications] ADD CONSTRAINT [FK_dbo.TutorVerifications_dbo.Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[TutorVerifications] ADD CONSTRAINT [FK_dbo.TutorVerifications_dbo.Staff_ReviewedByStaffId] FOREIGN KEY ([ReviewedByStaffId]) REFERENCES [dbo].[Staff] ([StaffId])
ALTER TABLE [dbo].[TutorVerifications] ADD CONSTRAINT [FK_dbo.TutorVerifications_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[Staff] ADD CONSTRAINT [FK_dbo.Staff_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Students] ADD CONSTRAINT [FK_dbo.Students_dbo.StudyLevels_StudyLevelId] FOREIGN KEY ([StudyLevelId]) REFERENCES [dbo].[StudyLevels] ([StudyLevelId])
ALTER TABLE [dbo].[Students] ADD CONSTRAINT [FK_dbo.Students_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[BookWaitlists] ADD CONSTRAINT [FK_dbo.BookWaitlists_dbo.Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId])
ALTER TABLE [dbo].[BookWaitlists] ADD CONSTRAINT [FK_dbo.BookWaitlists_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[BookWaitlists] ADD CONSTRAINT [FK_dbo.BookWaitlists_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.Fines_FineId] FOREIGN KEY ([FineId]) REFERENCES [dbo].[Fines] ([FineId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.Rentals_RentalId] FOREIGN KEY ([RentalId]) REFERENCES [dbo].[Rentals] ([RentalId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.RentalRenewals_RentalRenewalId] FOREIGN KEY ([RentalRenewalId]) REFERENCES [dbo].[RentalRenewals] ([RenewalId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [dbo].[Sales] ([SaleId])
ALTER TABLE [dbo].[Fees] ADD CONSTRAINT [FK_dbo.Fees_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[PaymentAllocations] ADD CONSTRAINT [FK_dbo.PaymentAllocations_dbo.Fees_FeeId] FOREIGN KEY ([FeeId]) REFERENCES [dbo].[Fees] ([FeeId])
ALTER TABLE [dbo].[PaymentAllocations] ADD CONSTRAINT [FK_dbo.PaymentAllocations_dbo.Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[Payments] ([PaymentId])
ALTER TABLE [dbo].[Payments] ADD CONSTRAINT [FK_dbo.Payments_dbo.Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Payments] ADD CONSTRAINT [FK_dbo.Payments_dbo.Payments_RefundOfPaymentId] FOREIGN KEY ([RefundOfPaymentId]) REFERENCES [dbo].[Payments] ([PaymentId])
ALTER TABLE [dbo].[Payments] ADD CONSTRAINT [FK_dbo.Payments_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[Fines] ADD CONSTRAINT [FK_dbo.Fines_dbo.Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Fines] ADD CONSTRAINT [FK_dbo.Fines_dbo.Rentals_RentalId] FOREIGN KEY ([RentalId]) REFERENCES [dbo].[Rentals] ([RentalId])
ALTER TABLE [dbo].[Fines] ADD CONSTRAINT [FK_dbo.Fines_dbo.Users_WaivedByUserId] FOREIGN KEY ([WaivedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Rentals] ADD CONSTRAINT [FK_dbo.Rentals_dbo.BookCopies_CopyId] FOREIGN KEY ([CopyId]) REFERENCES [dbo].[BookCopies] ([CopyId])
ALTER TABLE [dbo].[Rentals] ADD CONSTRAINT [FK_dbo.Rentals_dbo.Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Rentals] ADD CONSTRAINT [FK_dbo.Rentals_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[RentalRenewals] ADD CONSTRAINT [FK_dbo.RentalRenewals_dbo.Rentals_RentalId] FOREIGN KEY ([RentalId]) REFERENCES [dbo].[Rentals] ([RentalId])
ALTER TABLE [dbo].[Sales] ADD CONSTRAINT [FK_dbo.Sales_dbo.BookCopies_CopyId] FOREIGN KEY ([CopyId]) REFERENCES [dbo].[BookCopies] ([CopyId])
ALTER TABLE [dbo].[Sales] ADD CONSTRAINT [FK_dbo.Sales_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[Notifications] ADD CONSTRAINT [FK_dbo.Notifications_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PasswordResetRequests] ADD CONSTRAINT [FK_dbo.PasswordResetRequests_dbo.Users_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PasswordResetRequests] ADD CONSTRAINT [FK_dbo.PasswordResetRequests_dbo.Users_User_UserId] FOREIGN KEY ([User_UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PasswordResetTokens] ADD CONSTRAINT [FK_dbo.PasswordResetTokens_dbo.PasswordResetRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[PasswordResetRequests] ([RequestId])
ALTER TABLE [dbo].[PasswordResetTokens] ADD CONSTRAINT [FK_dbo.PasswordResetTokens_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[ProfileUpdateRequests] ADD CONSTRAINT [FK_dbo.ProfileUpdateRequests_dbo.Users_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[ProfileUpdateRequests] ADD CONSTRAINT [FK_dbo.ProfileUpdateRequests_dbo.Users_User_UserId] FOREIGN KEY ([User_UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[ReportRuns] ADD CONSTRAINT [FK_dbo.ReportRuns_dbo.Users_GeneratedByUserId] FOREIGN KEY ([GeneratedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Reviews] ADD CONSTRAINT [FK_dbo.Reviews_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[Reviews] ADD CONSTRAINT [FK_dbo.Reviews_dbo.Users_HiddenByUserId] FOREIGN KEY ([HiddenByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[StudentSubjects] ADD CONSTRAINT [FK_dbo.StudentSubjects_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[StudentSubjects] ADD CONSTRAINT [FK_dbo.StudentSubjects_dbo.StudyLevels_StudyLevelId] FOREIGN KEY ([StudyLevelId]) REFERENCES [dbo].[StudyLevels] ([StudyLevelId])
ALTER TABLE [dbo].[StudentSubjects] ADD CONSTRAINT [FK_dbo.StudentSubjects_dbo.Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[Subjects] ([SubjectId])
ALTER TABLE [dbo].[SupplierInvoices] ADD CONSTRAINT [FK_dbo.SupplierInvoices_dbo.SupplierOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[SupplierOrders] ([OrderId])
ALTER TABLE [dbo].[SupplierInvoices] ADD CONSTRAINT [FK_dbo.SupplierInvoices_dbo.Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[SupplierInvoices] ADD CONSTRAINT [FK_dbo.SupplierInvoices_dbo.Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([SupplierId])
ALTER TABLE [dbo].[SupportMessages] ADD CONSTRAINT [FK_dbo.SupportMessages_dbo.Campuses_CampusId] FOREIGN KEY ([CampusId]) REFERENCES [dbo].[Campuses] ([CampusId])
ALTER TABLE [dbo].[SupportMessages] ADD CONSTRAINT [FK_dbo.SupportMessages_dbo.Users_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[SupportMessages] ADD CONSTRAINT [FK_dbo.SupportMessages_dbo.Users_User_UserId] FOREIGN KEY ([User_UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[SystemSettings] ADD CONSTRAINT [FK_dbo.SystemSettings_dbo.Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[TimesheetEntries] ADD CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[TimesheetEntries] ADD CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Timesheets_TimesheetId] FOREIGN KEY ([TimesheetId]) REFERENCES [dbo].[Timesheets] ([TimesheetId])
ALTER TABLE [dbo].[Timesheets] ADD CONSTRAINT [FK_dbo.Timesheets_dbo.Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[Timesheets] ADD CONSTRAINT [FK_dbo.Timesheets_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[TutoringSessions] ADD CONSTRAINT [FK_dbo.TutoringSessions_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[TutorStats] ADD CONSTRAINT [FK_dbo.TutorStats_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
CREATE TABLE [dbo].[__MigrationHistory] (
    [MigrationId] [nvarchar](150) NOT NULL,
    [ContextKey] [nvarchar](300) NOT NULL,
    [Model] [varbinary](max) NOT NULL,
    [ProductVersion] [nvarchar](32) NOT NULL,
    CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY ([MigrationId], [ContextKey])
)
INSERT [dbo].[__MigrationHistory]([MigrationId], [ContextKey], [Model], [ProductVersion])
VALUES (N'202609302059544_InitialCreate', N'StudyLink',  0x1F8B0800000000000400EDBDDB721CB7B228F83E11F30F0A3DCD9C58C794642D1F2F87744E50A468716FD1D222257BF65347A91B246B545DD5ABAA5A32F789F365F3309F34BF30405D7149DC2FD52D2B1C41AB0B8944229148241289C4FFF7FFFCBF2FFEC79FDBE2D11754377955BE7CFCF487278F1FA1725D6DF2F2EEE5E37D7BFB5F7F7EFC3FFEFBFFFEBFBD78BDD9FEF9E8F711EE4702876B96CDCBC7F76DBBFBE5E4A459DFA36DD6FCB0CDD775D554B7ED0FEB6A7B926DAA93674F9EFCE3E4E9D31384513CC6B81E3D7A71BD2FDB7C8BBA1FF8E75955AED1AEDD67C555B54145337CC725371DD647BF655BD4ECB2357AF9F8A6DD6F1EDEE6E5E71FCEB3367BFCE8B4C8334CC50D2A6E1F3FCACAB26AB316D3F8CBC706DDB47555DEDDECF087ACF8F0B04318EE362B1A34D0FECB0C6EDA8D27CF48374EE68A23AAF5BE69ABAD25C2A73F0E7C39E1AB3B71F7F1C437CCB9D798C3ED03E975C7BD978F4F37DB1CE3E55BFAE5ACA80914CFD91F3A04396A7EE82AFEED115BFCB74918B0CC90FFFEF6E86C5FB4FB1ABD2CD1BEADB3E26F8FDEEF3F15F9FADFD1C387EA332A5F96FBA2A049C444E232E603FEF4BEAE76A86E1FAED12D4DF8E5E6F1A313B6F2095F7BAACB57ECFB7759B63F3E7BFCE8374C46F6A940932C50BCB869AB1AFD8A4A54672DDABCCFDA16D578282F37A8E3A64002D72096B95ADF1E87E3B7EC4B7ED7350F607BFCE81A155D61739FEF7A31EF0764D5175FD4D5F6BA2AC6DE765F5737D5BE5EE3963E5442D187ACBE432D4BC48B9359589422D437692F41A4DE8202340E8BADFC980D6758F121FF1A5BC4FA0BABE1C78FAEB23FDFA2F2AEBD7FF9F8EF58EF5EE47FA2CDF86120E2639963A58DEBB4F51EE9658E6BF8F536CB0B45AB4F9F4469F622AF9B76910EBFCD166A180BC786CCA1A159329BC74F9698CE5051FCB6DF7EA2B00103F7F7189D38CBB6BB7D63ADE8382CEFB3A6F95AD59B3759733F627A959759FDC074E1A7E770171C5BBBC98A56D11AE94780D6489F2E5BA20688E6F6E3D36573BA6EF32F93ACBEAAB04ECF4AFBD986E738EE577597976715B6BFFCA87A5BAD3F57FBF67539490156EAE8434EE6D4FB1AADF3DE507CA2C373851793B3FBACBC43E308F9F693CCEDAE97A7AD1F696735226A5C8FC692C08FBB8D195E23F25E3D80CB94A991D1CF65D0CCE8EC85B17CB633A8CF82A141974196869292B13F0A6266109E9EB14442D2546C4B15A94E10C959444A05638C29981AA5699A4B478A9D4CB2717CEC8DB2BEE68266D9BC8CD81A66A60B5038D36C1163C14DF35B59F3440A5D2D7A52F7E0AC7A1094501A6F03A01EC4B16D1F1CA74D93DF951196A211B1E72232A3516B491A0ED0957331A8C579185B55DECBBA8ABE1E02A0ACFB23A5A9FBE3B2B0F8AC29526AFCB7F9AE4A616185E03ACBCD6668B2B5E4C7286BC9396AD675BEEB7D8BD2B69F196EF4035B4FEA7927B59E9859E924E9A7FB4D4E360B4E6ED1A1EE929E514282936774AC38C93CD9664794796CC854B5C91A03D5EBC523F18421A69772AE4472C54D22AA68FA79C4A6E7014AD6303661BEE4D5BEB9C1B2A6EAF6D3276EEA89D7BFE86BA296CE519BE545A314A210ED5CEE4E379B1A35AA969E9BB90213B944E4A62499F0F0A9C7A06F57030875F2C19488A71F6CB19769F42A5B7FDEEF1C578CA9F2824B464F83CB9A31D74C65285DE405C295EE950A298A4622FA613FCF26227E7DF7C7027B7C750C57E259B5DD15288033F126FF4FF4EAA1450D671268F6B6E85F7BD4C89D9096BDB9C21A2CBB53FB59C2DAA75407409D33CDD8150339AB1E1040D04030949F22AAAACF67D5EEC1490F0D7597F4FDE1E69D3C7F43BD542A88B0CA57ACC31C97BDCA6A22E1C9EDE0B3AAC4CB276D0A938941C6812AF157AF04A1A3723D5DFF6B9FD711B4EB392AF22FA82613447410686CCC8AD2A5E136DA82E7E2EB3063F9234D1251B5AF6B54AE1FAE3A745D5B0C093F1B9E74EA27135985F62D12A7B354EF9269052BDC4135AD7A084AD1D205A282654AAD4F9EE467601362F11C8C2B92D3E4781E464B9F9A361612A0900690D3C940792F4EAE0BD392B6F1A0ECAD2D63A34522DCA274D97C523BF162AC031FF256E98531D568B6CA7DDFDE577508556AEB0DE1563DE7F556E3FB20A2DBDCABA378822C16634B9BFF40596DB5989D6129BDAB6A03C34BB3ECEF3FFDDF68DD5A2EA5669EEB401E9BB30A6BC0CB2DB50B018284F03FE1A60C70E385B9C5B35CE3E033F42D6AD89DE1ED739DAFA786CEB12DB4CD0AC62A7A8AF97FB3CEC818DA0FE81F087D2EB0022DDBACB840F1DA798B0510E37F8FEAF3EC215A2BD7A8AA37A87E8BBEA0C24FD02F9B8E279F6685E91ACC74D95C543519487F4461A2C830B36F11362C3737FBDDAEC8AD1DFA5324CE5556E259665B7D889D72AD7E68215D0A93B457B952C36F3543B006DF54001A7A73A9B5915CED72C9399EA1D1CE9FE3C126BD75B8D820090A4E09903CC7380009E778285B0E0A53474E3200CAD12C40C0448B60B6540F8BB69CD60980A370F80ED335165A474D70F35F4E9608C9D1C703C0840A505E1BA379D2BAC4EBF575178DD89B0D41FB983D53233259A4C5CF0B465AFC18E6E02F6E84E03475ECA575A8BAA0B0521682ADACCA8C8B43BB3A265CE3D915D50342DA6B21813C04F6B16DA312E543DBE8EF821E660ABDD4EFBC9ADA0BF458774189A64D6E5B91969AEB072ED3C4B79C950F1A4DFE348ECF8B6CD6B3758BF79D8DCEF910A3F9DFB336D454D66CEAEEAB52EDDB8BE2DAD3C6AC3C73F383D8EBA5C93AE6151353206826B6D453350D56B48B66EAAA2EAA9826C79EBD5E827D82CB598591267364538D3D03B21722BAFE8292C41FB9DA8A93F2C836A24C8DEDFA2E77EF3A0FA411F16A44FFDC6703E121B09CAE497E0EE47A3F64E40EA87B9963C71972D6C12080A08B61285BFFC2C47F3DA914A884D609424DEC0CE6B582CCAC739FFD0730F37D667DCA197F4D1C6BA85CA7B79B3A79F13E8B134370462E3A86E19CE7CD2E6BD7F731027126D461020DAFD11AE1AE7A474D8E78CCAECF012142E11D44721D4CF150A9DC562CA4A8DB1800A96A63A16CF5305187F06183E372C11F3EA81715ABE542CDCD010460635722E75F5F6C7DC97112493559341C40DB5C2C27908209E2951938E5EE9AE9102CB8844D8AD976FD32D4E8DFC2E265EE87F23C47F65F0F19B1725C14BBBA1156C4D77FEEF0AE196DCEA97B4C336A5DBCECE7B2FA5AA0CD9DFF0AA88B730D1DB06F74EACB0C9CF2F8570D29F5F748C01D233C65C7EAC64B886C6DE35618FFB597E9FFBC7791727406116894C1D912AB3C4B67C70B3A4B87213403AF3C4B775BF65CDD36029205973F579F4D6A5F4D90FDD3E15C0C197D34C3521306D9A0427CD1612DDF9E55CDBCC878C7ECD95F2910350D1FA60443A895C00C16F0B681D88278ED40066340AEE34504F926C7633DD0532BDFFF58DD43E82C12B7AB08B8EAC2B7113005AE171286AA7F811D051E204CA3AF1AFDB0C70C0882C4F8484B63A67FC9720C9F174C528625579A1BD410D187F71C2E37A2FB9A3DA6109BA272131863CCA076E88E793F6F9D2F99771321DA2D6AAEB93755B1C1DBD0BC468DEF2EF27D967BEF44BBA9778D9A5D556EFCF7B578B40BBC085DA34C1DF911E61EE7598615675104C8093A2272F248F7B5FB3ECF91AAB447847873F705EA13B68ECC31471A9F91A1F221C4BA2870B43776E9D5481A748D0775C502B231D77C3918722D0005BECE4BF0C3B779E7122961AEB96DE779A9A18B8283889B8A1514CE30D6B1FEFD0AA224718211C91B8AA4A48DE5B66475AA5D49D4002192D4154809EA4B9DC8D15D8B98D04BAE47F0E56A1A55D7258C77431D26767ADAEF8B04244BA6BFE32C64DB6D929D851D6EAF14729710C6B2A739F11732EF2F1B32CB662797EBA5C8EFD682F9122C2810603196C1084A520A185CA78B2DC9B4BB1AD2A00B8135BE4764315D7F413DCFEB4B5B3D6FA76F03EBF95849131C9C030FDDD5F64027EF117340CEB4CAA7E2349D28586E0E8A20F0E403E0425E9265DB90E80BAE5C43A8E3E559B91DCDAA1CDE98164BD5F4C9CD6AF3C3516A581D4E45A7DA4B5E8A60669CF5BD088BF97AE4CF5CDC54753B9C1B99EB25BB35D079F15B7AD5735EEE8EED9EDF3FF75991DFE6EB4C9FE7274AB6A73F772410A75C23921DC9F3C5AE375839160FD7F43E2AF4E1C1ABBC5233298437158BFA3424C069452767108CED35C70EC501F96C75CBBC626363B386F221518A65D6FB82638F8EBFDD387F851774F9BD461511B4482858458349F82582C04C03E0BC9E8510903AAF1F349205D7129A0C972585AF7F6C3BA86DDEC6F0CF9C0F7519B548F36A06B0BEA1F225475F1DCFF1E6CA5823DFDE3AD6F655C7E3189FDDA375001F1B632004C2993A7A791E1843B5485750E9C6190E56E43260FBDB258C6459F762A865D6950ED8AA3F7D8DC0DB62B7754A4F76905D32EEEF15EA5344B86C93A7EA8BEE93071D65BF458694DB81EF7352E7E8E19AFFB7EA932E91EF4FA9726ECCD227A6DDE0CAC470511EC0DBDBD49D69BBB99A70D585FD4C43FCA38B93C92874F2B0E6D040F6425348E199D30413666B2C020F019C1B8BEF83D50EFA290E04F4CD8BA5C0EC1640823DAF39E21655CEFC5D4A90BFAA2107CE7F645805E78D93BEA1EB2FA87446125CB40E5D3795DA09715D2750A07998888D50F99C63852EE0AD959B53CFEDCD9051A6C014C44C211868C542040E389C90C351875CB19A3EC7F0065D60DF845F12DDC797AB8954C5F999A7B3C9F2E2E10AE14572ED94D166AEBE640ED28E803EA2CA564DD275B5D1587A4DF95B97E24FEBFA41E53EDEA90566CCBEDC34D1F0BFCF1EB658F09AB36A5FB67EFA7508BF6C06FD18E4E24C3385A7FBA1EB1F5C6886470583E0BA4658FE4B5F64EFBEA07AB347034EFF01686EAAC293A40B7299FDB269F673DF828B5DF7A06CA7F23CFB8C11755E214F3467D98E6833DF45DD585577B7A4EC5534AEB6A06AC6ADBB18AF43B554766BA8CB8D0FD4733364D1C7DDE8BF45496E1EEB2DA82DADD7535CC8C36C723CD48E6557DFA0B60D70514C7643D864B170AA84FFA2AFD675C9CB37B65E1683C71AE5B79C8AA2521D5C0F06C60CB6EA74DF6C2C8300C2A1350C657BD83FDD6E070825F8A6F2993CEAB360C2D365B6FB8B0B59C64C82F482CB91397E0329B890E5C1541F4B111193123016B324F45F4122862237320649D750334141440D850ADA4608EB8D60F7949484B2BE9025887C03E9E80A42EE433BA4E2F693FA0CD31162B3294C48177B4640B2E4D5A88908A78B514CED54B6CEC03F5F5BC7C8424B6B68C8F5268267A3E33AC3CF0FF56A644AE480C590D0095A45ECF0C580E01132C4F4F698D40B4E656A5AD8CE63E31995360F4C9C87CA636D9506263A6D97AE507B5F6D20746389FF1E6540E8FA787ADC3DD559566F5ED559B951EE1683A4D0A8376FB3A67DAE68E87980767EC5B3E26BF66022E6411E71BDC8F2026B9554F933485C5DB56F3B8D35B6F6EB3E17750FE8D97D772BD338FA38BCF87D8BB4230F967304FF5CA3A6310DFA0CB889B55AB3F59B597E753736346606A8085F317002C974B1CCC06060ECB779BDAC2B699C814402C73229751340C8FDD5342AE21E8B2B92D21564AF75E198EAF462D9ECA6178ED94D417F5454030CF4D1793B8C71379C4CA0C806C679F6D0BCA5CE488365FF261D76B4A9522DD6B116B431AFB5C3D5833F32E7F730FAAABE8B6887A58E3502FABCE4B06BABCBBE3A8350DE2DA6447470B1C521FDA20433E0189D3FC3C438BA4647B990D3324370D48C05303D53A9D79A3432C27E55EA6B2EB82ECDFADE7665325D29BE2789A56CFD9D772E56C2F61877D1F62848E6A63112C45711034B6A2F708E8B6ACC54ACC3694A80B0A1804951FB8138ABCA4DCE1CB7134E1239A44A82EE2B135919C79BDB0AF31E5CC686A5B12F9F1731EAB3B084D1652E67AEF006DF606DE737F1D0BA1F6ADB3EA095ECDAC5521997FCF6ECDD0C87F9C51EA98A8C83CA050E8240D6AF9928F6EF234A71FBCE96C8D81764F3CE1D5EBBDA4B038265CDA6311EC5C16E0243590E7E53DF111E4195E3CA5FF26ADF8432417E435F43A10A6A35B8ECB39C740B3C83250AC8691EF7A11E0ED71D71BD25EF3A0EF160B653168C23FBBED191A309B1D1795FE7EB80B34FBFBF20A3ECB8BB0868B71FDE3993BD29DB0577F186ECF451BC1E3995843CB0E843CF447387FE0E9312C4D4E96FB0B98783D1F517549934192EAA93AF9F4A8586B828AE4B75F0344E42C3702FB0F89EDAD083E7747A4324F6635D288379429C655C36D7283BD83CDCE768B3DFE189A212A5C0070AD2FBEBF4900A97D88542413F8A109E91754DF3B5AA37D7A841ED35FAD71EB95D6B87F02CBA3BEC2870DB1D4E551755959AF9D6A1BECD95F93322E5DAD4ABC7303A05F44077837360777ADE64E5C6F5CDA6A1AEBFB3993C9CD5A0249158E314D9A962F5FE1E549D4E2C9684C688DA674555A12365549040E08C123C585213B01D7E71900299911D78B118420C3D978A0ECB92499A496DA724CD63C5A3B2A709D16FB2E69E3FB2A175F733C3B3996574ADF126DF122F666F00250C9B0EE66EC8C1FED269886E1C5713B8444530506A1DC182C6516B7D1B4AA5368398901B40A1554D8B974D4482237D8C5F11CD92F74D286A9C2E9D70F5532938E3478FD35C230991B9F85D9DDF610D5B04390A1991756943466C0134DF88F7F57CC923C89952B5ABB04E0DD2F51159E8AE8F78C376DDFA0156C7F489294C7DE8BE12A51F0E6C2346BD219C6C2BE4FA60B1535A0060A581D20428C0C48555011BF4FA02D00E7495410E66447AF8374AA1568434D2522023A203A492C6DDB9CD0BF471B7C1E2E763CB0078BE3BF212EED12E72546CC83FFD6F26DA6AE52E68AFFD3D2BF6291467373A6813AAB9D0773CBEFB30BF2D1FA6AB6B11D08712D7A212525C07D4E0E1F6E0503BC22E5C066446B6FF4EFC1AEDAABABDDE3B7914A7CA8B2E54030D6E4B1555395D8C1369547D2A1A67A1799FD5F8FF985AD5DB2D61F4EF45556FB356D1CCD3208B59F535C0DD836928254AD8155DC444F214C992E8C941B0570C241D3E090000F1931094A7C2218FCEB8691B52735155430870D33363CD2373EA5D6362C8C41D4E2D1E5A07EBB6DAF63912BC837A9631D02E9B37F9663367F5708DB1E9B124BAE2DC37E66B18F6583CB36AA8DC2AFDBC803C296C09A09898625BA36DEC988AA81946A06A2C9291359587785CC7E3197A16C3F24FED783C452F6248A549039D6BFC459FA397C62833E309462B8310B217667830170FAAFE651EF1C57B0DC5EA777A00486BBA47ED6040F4082AA778FCBF86DCF1A95D2FEDB6DFED8A1CAF2BE597AABBEEE0A0DE58140BEAB7810217C546554DA6D146BE799FB26EFC910CFDD7BE49F6FC1833A38F47BC17798134AEE548EED64E543606ED478A59FDB82BAACCE898CEFF1AD3204ACEC98702BC1CEC9DCA2ECBBD7168DCB88EFE1DE90AD4690178FD61F5F36A80A4961F08405C7D4028F717788D68851F10964369A9F6783C78446544F80C2C277BFCAD257A02F45EEAABBA9DC2D7DD56FA19C3A2EF527514B82CF454D503BEDFB0CC2AF17A9BE5AABB52919A95BC2868B4914B7F3BEE55B5511D19600322920521DE17EEA7E3F79354C793544787A7CB3B8AACE6045E520401C0854184B276BB294F7CB936C0B35E198C8EE008E7BB5C0BC2B3B862B18EC800EF713F342DDA92A78E3A9173586869044B3A0C7B0ABA43526B572155573A050D1F6BB754410BC5D4983D2E16E660B70F4008AFBD07BC9E870D131678CAD2D2BDA260A9690B8388535702E7357D091B9B7B845A5C420298EDE72F8B61C109DCB5EF62264F155319C913CB7C3D596E4FC3FD51D59F83C4EABFC1121AEF85D8B4EF173A1D26B2B20F1D2AC210C2DC96805947368F684CA8A580A5F44E3F7514CF8061D49197265AF292293DB5AD2F9A9AEB8580CA28C4EDA73F10FACCDCD871D629C0DE6F628BE3EEEF03E654115757E11DFA366F033C2C72102EE11E4F1CA76EF22361A29F7249F6552F8DC86715D5AACE407EE31931EC3186CAE5EADBC74B2CBF48437189BF3EC31529280B7055A64381257778D5DD695561512CBA43ED28708A6599AB1E5938E04079107BF51477A1DC6474AE4722ACF367E78C84786D39470579CE22E2DBF59D2412ADAC8AA57E6AEA935D5A2F2BED6A76CE8186350C22AA13099CBF5A21C2E2AC5048E525CDD4D1E8B33651CDAC45BD1AF9AD2A4DE77E33A5D30CA24A9A3332D58BC2175DA259DF35D30D3B5E15E335F3B1CD710BFD44457806450C5139C50CCBEE101BE8AD6FC5C4660D7135A3AE9AE675569798B8781B063C156B62E6C4D4B00AF36C54418079C616C1FA942AB7D3A3FB2DA545B152E8A2332E9B8B22BB6BA6FED9ABD41E53287D8A574A8CAD78C002400B12CBF52B44A2C7C683F62E4577E78AC772218C10037B81B634B428D80CF4BBF69EB06800FE510D8CE5E616110BE14375933D4CB59E8BA3D38F83626C5E65EBCFFBDD680FF98F108D6FB971BADE9765A773CC86EA66BF5E23B4215AD76CB4C80BB014F48F0E8CE79E8BF1E73C837039D6FF466E6099B1FDD7AAB2E1B8F9F4A8AA5A362978D8F36C8B97A8998CBF3B0E65B81934635B6E104FBF6001276B91E95092145EF517F309D43FB8653AA03755B1311DD0B7154992018FA66EF07F5283FF5113D75FF9EEF676AAF1DF1CC4653025038A0C8B7139B139CF9B5DD6AEEF29966AE5668D722BB92101529EAA770C3DECA23DC38D028076B9A1785F646BF361385D7F2EABAF7849BB331F8AF759DDE659513C505B23B3E92C56D0CC696A23E7A1A6078F40405B8746B8A0B2FE9AE5648735BD186E38E643B561676036E8D8C0B8CDEBADF958535BFA40632D4AD3BAC84B732DDE671BDD78A97038E75B001502E05D4EB0C826B52556C059D1E5973593AB2BB4C9D7640B6D2651AFFFCCB6783FB3CBEAE1550F33B93A6D9AFCAE24F27E8EB24DD1BD5F6E2660EC4ECB45917433060FC294B43F9C4A91A05E701DC13B6E0C4D93642A087D9D706BBBB86B6B76C41F20552D468349F7EC7C70C784184908EFE2C368BC36EC767595DA2CBB40C36BF6FECC1F502DA83A87D388EEAD3B5353983C214757D0EFA1C8DB73740DCD8C2106CBFB7DBDBECF1A7375892DB5CD1E5DD01AD6685BC5D4D0ACC964D3468863EAB82CCC9819E194F1846C3939FA58EEB2DCD88C7F4FC3EA2C39C1CED208CF35BADDABB4ADB05BCE988D9DCB423B58B4A1B402856EB911C5A65D7D67A11408D365636AC3C42BD4DE13775B3036F608176464564B27860D63C2E90B06E1D1ACF5E20E2DBEF39BE8F9606BFD806B39860F4BA429C3FBE5D194DB8C3FD395D701D7C409DB82FCDEB74D9B5909B9CDCAC82D5C2E3CEF6DB9705CA7F12DAE586CFD4CEB16F3D3DCA8C59497E65609333D92B811E9176D03787C266CC736AED6EB86D6E23462BFF87CA6FF20F038971B8AC1A52CF256B3E51AEB092CD66CBCFA681AE114E6BFE9A6F49AC4DF9CA38CCCED8CA6F31F46748E09F3A78CE1B3D08941EB2A0482A3E2A9466AB9FA82F3F8A94688B9FA7FE4EDFDA6CEBE524E31833D3846708D9AC1D74BB5ADD15FE320F7219BF3B4336399E8BA7F66C62B8147CFCC78C43BDA9F693833BCF47197376DEF0B9E2E5BCC28341C12DCA7A2234B3391040C826BEB99664ACDB75844F235F36AAA3AC6EACF357F36711E8943FC5C231B7DBDF33DBAA928C7EE732357156FF63E3738F3CF0A61817FAE110B6271F222FF5C2307C35286F55A55332E0BCDE8F7DBEDCBA6D9D39574610159DE165868C5A089E79AF17E5B7DBD69ABF5E7B98266989923669E293F69C67AAC3C460B00A7F53F6903A55814C2C2F1934606C63C5AC2D4F84923054C02AEFE788CAAAC1507F1ED4CAAFACF1AC631D5058DF2B36EEB0164D7A7ABEB8ED9D9EA9C5EF859774CD6AFD4BCA8FCAC61189B3D4218E59F35D3E874B3CDCB418D53A6FF3F38E3C0702BC53CAD11622F45215CDCE85EEA44C6FA9CDECCA506BC2316C0AF26605D7CD8065BA5CF371F670405B34B378282EDEF32825CA243FFC163102E376EE28A1378B8180F93F628859B9C2E23C565CA0A13BF36215CD0BBB743C6210697E42AC45D8D972973BDD85485AF7B4FB8A81E20F08345B9600C679DDD1AFB80802D8EE5AC4A3153C47B9FFEE3C5E35C30E0BE12F7391A83B627DE5C170E49BA4F3F35B47F5033D4DD12C955319A6CA74D53ADF36E1CC6537092BD6CC82FC7B6F8BADC3CEAEF2B5130F35DA687E9EE6F9F43ED0A0F418E7730EBBC7D78F9F8BF081D80D14D0900667463B23B16A1C8C66B725187DCA3EE36E7C422CECB96017944AE8CE5E53ADF6585BC6DAECA23E8AEE79C72F2846FE1646A822F39475D0C55D9CAD9E8DBF6D404CB9A131D6F5E9C5092602220FD26E7D583464646B070623261042405C2F8E4871F828A0ADFBEC9888D6947BC658567A791B88C95E45424921AD293551F4CAA151D0E56263F04CC568678D48B0892848884D224E1B0090573ADC390A94E0AB443DEFD092D47DD1F40822084A1C5876EDB64D8087C28C1A199E9D7764A41E9E6B5764CD924AF810485C90AAB5135A105856E3BB586A199E9D7760241E99CA96A2999412011E94A6DE48342975638C486134986C8C0C3178BFD266FDF5677ABD335B9BF251D4B060C148F01C24A4258ACC9AD15B8FD54C202B2D4C846211516959A3E3D03217E3A895218BD2034244313A08D10C1D8D36A1C250D89E449C965233B66AEB6AC7055D5E7D519DED8DD55B542A8682850983080951C31084167CD4052241982DA37739AF4757CE50762A87FFB0904E62A2BB33BBCE62A8D1B1A081297A1DC4662189469150ED474223D0331F2E0CD9C5EB84777D138D66A55C04187D3313C62407424D218CEF4515262329C4395204A47C2692B17A0929E0412363D44A6D4420C142451F3E368E652C5224DAB88C0B61369229099C7A18AFAD46B35DA4CE3AD541902783065246206E447269481D59194169331A59F8CF5554852769B1022545E56D2C6C7E09452203CABEC2955FCFBCB8C2C0D8FD34515258E003301923CFD6E2F3F1C3BBD5B4F252BD323414606120F1D4C7A04C48B1948324A121B48324E1BAD755CDD45258C2425EC526A2B256B8292491401B095AA1929204D9098069423A16D938123F001844760A65FDB29054513AAC3C105179685C27624ED9B79620284EE48D8EADB7E02C119AF93ACE62B290AAF34080D09D10868234430F2B45B33250D89B6684A269BD040D75B74DBC6DC99327725A9ABA93C00C35BF4F66E004953E6465460D7809A9E74A694D9501CA5D3A9EBD2DBBC446AE30A06D70AE1DB2E9D96A320CECDA4B5BBD4442432C0D40C3F784B4C245F6392C92A2410B185AC351D21C9CC361DEB8FC07E13BBD0AF81E642D0ABF6F8C2D6B7A3F094828B77349163C83119E8AE42708163B8EF4946728B4EEB8887C1235A702E1EF928A6DB82EE7835D3FD0949B92B552B33162CF83E74518D0553914A51C19C3D7CFD34D13D264E307166CCB0C14588429D3C525341446A4F86C861130AE65A8B7A31C65E74EBF5240DBAA167A05572656B59C10D00D22513DCC0E205D261E4A61AEA04123290E1FE74A416B0D9DA361280D9928C2562730BBA95106A2686B00914998C32810F296902E38D17473925290F8A18D9900902081DFCD08841AE5064907C853DBC969263A34702489A92EF616849206B7D1A823E355B5EE0E1D2B9C26415208913606D444FDA506A57988E9064AE301DEB8FC015D63F9B3B8611752941D017C4BFDDCD0981082F153649149656CE8026A095742638A2A8C96931F20B4CB542489B9CF52168492E709A984108389EA859C7124611B2A5020A55BCF6A6229560A983E26710A910594BCF12B1F062C3897C132203FD1A4E25167482E4D5989454E1E55255928A0EFBE69CA518C91A4BEE0033A126A5B46946C1CC2536D65A36B0A7CD6E6FFB94639ABB3B1C2078F433C3581DFCF0A8135FE291349F48A0648C3D6A25D675CA4993753593AAB3BE45D090978A731AD5C65066664AE30A11151C3338765A4E41DA22B2DA1B56E6F2D25B19F1E5B26F079046D0128C26840C192623DD55082E7A0CD73DC948BD8F3490300634DA1E7239910229482A4D20878F449018271E2309E6DE5076031DDDF9CAF25C2271091C175AA28C4520A01F433B3061884A74F69497772B467064520201CB4E9EBAC71CEC0E9E04D432B1534979C09327193D26834BD70B70F024E37A185212CA99C15DB8192CB06C2D78114E6CDE64E482DD831359EADB7C5291195EF250A5661361830BCF8C39B9D74C414422DF8682C166A234555BD849D6A5EE37398A14416147590765E72413105B9D3D86132B3925662E8A40278F724E872023A15469DCAE335060495AC4D52A369DCCCD2A32F2E05DACA3FE1C4759B7DC4C7322E822366295E81B40FC022F601C01A6F31B5709B47E717CF52620A1E8A8BD500C5460B159C4ED04369ECAE30472F3F09D4D0CD92619B378E01882B3AC1349454A62D7918AE147E5301A9FF8D5A740622065C23502D94A188B3C7D3A24B07D93810C94120964AE5FFBA905C8C01DC4C14613A205BD43121A92BA88246C3E123FD144BD897DCD034713A945CD6D1915696D6E19AF8FC1F07E9F3D9037964F8BA21A0EB12F90FC5E0E080D89960068235F702B80801152E3089792049371C5E09E92A5E4B51709E9C46A85A95AA3A651FAB5015885483908128339B95F5B414422679282C1261450D516752F8DDDC028F7E5E69D3C608F070C2C4D135A40942468C34B134F84D140F675034913CF5D130AC63A4A4A128A92CE92E2E0020BD262A693848074569384AFC769308D12606ECE4CDD8F6E388D2D99ABAA680614474A728525E7BF372909C48ED87EA3935236FC140C245A80B5AC12261A9BC4EF04784CC3AD7440FBA67E1F5CC5536600567A379E424ABA5C82DA47D1593050562C33777018931BD970FB89EC6B989D465EA5837814FD1AA3C98A5597474336C0140C242F7DB18DC4D00825EA054AEC11485E80D68DC60BC37B0A0BC048BF96D30988C9665E040D2A2E8BEEE4E53424523472E61ED33EBEEF05FE8BBEE2CE0CA2A01E7716582E51039CBD60710D00A2058B6C50C9828930DB57931A41A40B66B52F11E994946E4BCF8205554E8BEDE7E1F6D36DE7619E1EC36EBE33DD342A888209662A2FA46780B693A917808D47A0553AAAFFC834295D19A8604232215C663BC5379F7237C5F3D2A4EDB1CEA2C60D711C5CA8521B8E00813C341792DC8590C085F5CD5C58A629BCF04F4FC8F3CEAFD944D2A05B5D907271B19708DBA525AC542CB5B8085C3C86B565227ADCB7E8C77534CF43CACA88532A32926D550CC9E168311C4552259804712C361724252129AEE86405527BF12608F0720E2EB4BA9933214BEFBF13DA4EE4BD131878F0BEBB8E62DDA698060A261A8B6D87A1D6D36D86215E1EC5569844979171562D187DE782AC3D1D2A483600590BBBD2D02D1B8D0C860FB0BAD0BCF36B36953468B40605134A2696521940E3E93406C0C7635018BF55ED9CFA4A792D588084C48506B2911B1179DA5BC2D2F613F94BA4CCF56B3F492855D37CADEACD356A507B8DFEB5474DBB7A93951B751E0C753538A84AAC611757A56C71819062137A12C99FD970981033553A3CA954AA37698D64B268A3F644317465CE87EA3322F936FB3E1811CC54D1B2A78376660EDB161801A91F8A489316A4CDCCD3D055093967C131F1A625F54CED7B613E4F67F82462B88061A2A16289E54164FAE11B2955D3EEAA1291E8DF718EE802711575406113C1ADA44DD19A79E06E28A1D3136332E461A2780DC6C19B988544507B73455E2791082E77BB454F4CBABDBDC1381CC35E1FEA863A578FB44622F15B249F8F961093B10E91DB47CB7D4F4252885C5DDDE605FAB8DB642DB2F00E28AB81C207D4B0923E758BE9BD0346F4A432FF8C86E388BC03507FD4BB0E598D64B298CC3B708D7655DD5EEFCBD5AFA844B5E61612080D07E40E807631B910F6B43B30250D8926A092CB263450D516BE36401E67D1EEBD5830589C08849D2C3138936FAAE0F6D3EDA3609E1EC3D669A0FC4DBEC1ED2875110317546C26A40B5C58020948A679409E1AADF5439D43C8A34D3F17AB8CA101C11559901D1ECA91B4913EC2464947BADDB59AE7C7B0B1067A6096B3DDEC01E9A0A2B6E023D25A6A4CC73A6432F7A37F489AEF8726992E0C1E57EC967A505A4D87D1F806C9A7ABE6B9371D29846CBFC38383D7F1F24B95AFD1EA5DBD51BD2000418322C6025AC918D80628623D604F72244153116332BE5D055F295371DD93880544CCE0796A7995C8C2B6E4C3D47A5A126D0DF4DC3721E4501EA5E63A33FE3696BDF17764C99B9A5168BA444A8E27C56C1D1B7084153D9EF9FEA42412B9AA6EAF50D364774897611C849609DB0C682B6B6213C679C6C3AA38292126631B24D9B892E3BE54A4972FFDA99BAC4254295BF08C4D4749C27554C576235FDB419CAB713D51BF3226C24695B364E767370F4D8BB637A86D899BBD3FC453CE3A181E64060D6AC50B491BE9A79C9A9054334ECD72232AC64A8B4EB80FF91635F708B5AFCBB67ED01EABC1E0E01BDD0CA4D503DD701B4B241F55936232CA614EDED46CF7A623A59C99ECCA2160A58C3989D7A27B7015158974988ACB47B4EF9EBBA18E44E4E022C8D322B18692E64D06304484A184AB9ECDA75FF8E6A1375C96E68EC75CFC28F602622513D738AB9F408BD1208F95C2AE8002F703D09242E888C8E365F8066F29C83D5DADB905C38322C7825AC99CA495E4014E1A4212DA5B6ABE1F85C1D5BDA0D966DA659185938A1601313287E4A853AF8C70F3C9564698B187B632627D8A998EEBB4B806AA99A884BCFC7CFE8914A13FF1B0ADF74D5B6DB3B2ACDA0ECB2FD8EC3B2B6A329ACDCBC76DBD1773C610D478933C203DDD6CF3B279FCA8FF4E4941570008125B9D589950EDDE50D754EE3DA908AA3FFAA20D9A27232B2381946991C81018553EDD6FF2F66D7507B27028D3227995AD3FEF77122C53A11E4D9FD82B077B3367FD32C022436030A42DBAC36A5A32A85DA19E84ABACCCEE60C11A8AB428C6732008C77CA2A645D2C567C0388610170D8A7354E45F504DE62DC813BADC1419CCDD1193316BBAC000157F86D80A1B74B26E0A40466288D76A992476068F0645A7B24FBF6479917DCA0B5C04E002604CB02A24837D545CC7BC29D20CE41A15BB674295941CB3EABF63D11AD3D54851D14006DDCB6E6FC18EE1EF5768FBC944BEFA302B197F50A9E732FD12AC4CA0E6D776759310CBCAC3156AEB7C0D4EC3B9588BEA028193A5CBCEA5A92ABC9C0521025E2C3343AB40A6EF9444055C98CCFA3E5B27B81AA33E43AE118221DDA71CCF943655277C196C1BF499F03495E9F44F101236AD967664C48C28F0304199536C90779920B4A887CC1B3AC4E26557103374C958871AB84F07E206EFED69C568B89A058BD074E94D8B86B863611CFD951533E5A73244B8785EC3D57A88B151ADD5535C9301CAF9485686913EEED521A40FF2407CEC89A96E6DA3DD54B0EDC4FB064D312A71992DBAB34743BAE4D21E2423E304EF05E59649E733E0D050FB50767335C62F3DA220A8FD1518DEC4F81B28A8C98D31113DED1F853D358C6074565008E63D24D7A513B64FC6FD9D5EE89376197EC30F205A78C5CFA1E3C2BB7D3A1C8E1D276DAE70697E57AA7ACF83A9C9E7A0657C1836E01A5EF0C85230A477012858D10118D0DDFDF1EF7EF707E83888C2A7E3BDFF46D171310A08A698890072EF3813E71361C43B6F97B4D754A99CD61908EAEFE8675374964210B3A783536A75BAEE3686406F590805C10C20D8EBD93BA6EA388B2762E7275FDA98534FA6EB6040791740788823B4AB4FC112185F4CCEE02DF06A76D0011C61001494D37020077AFFA2AAF30C0A70D19FBD8CDE3D1F3C8AD2C9CF94CB89A6C1A06ECF3E4D45CF1924D1477BB42D269FAA6CD47940DDD071F03E52C0A3029822E3AC035FA61B0732616001E4C4337050FF29FFB482072C9AD802F1BE26075635DACC0E72894488909A71142A78C884880B608C94BFAECC9936D8129680377445E2F99BB9F6DDE7EFDD329D1EDD0461FA3C85C3EA348400A8E9030FEFC10E01555C0D311EA3ADFAE330981F33809AF8094ED6FFF1344FC384190FD079908F3E3D97BB04781003AAE5AE01DBFE27F1118C876CABF3BCD965EDFA5E6640C280F26E80F0105BE83340055B607C115710E684CFC8B6D0D4D0AF8A7045D56A3B1E791A2CB912E469EC8FE99854AA66249086FD9A2A6899351CE89A326C461C5113892DCA559214D6A6437225E5CDAB245A4B6C7638DA37E117901442D327363144486EB1E9208C27B8B73A5319C41248530DA332889DD5562ACB785A65A4F2C441182C5852E9B15DF9D209CBD4E4355AA3E13D6A152F2830837ECCD021B842618B680DD0614DAB393249CE131650DF0F065EC19707BD9A815102CC91B3DA9743545894864533A4618766E5198E49334EDDEC0211FB6C3ED87839C516840134D83E301D0CB01D61F0C9850966BD03878440368551248595F74B5605629536A6CE0C735C9B880ED3A373FBC9D8A54B0328F6499106906796CC716384149A837458616056C9FD60209C697FE47E31570E25F193F5F70864EE62AA5443B5D4513C0676EABA1DDD452C447FD2B786657D97C16B3A23A926E50F17B9AAE3950C7D4CF7C81CF12A3F5CE06114E63E070AEE1EFAE05BD5A681C7B290F80C94DAC9505FC96DA4BBBA91A5A96F0354CEC0B804E1E810EB6DC247E0029AA667EC55B4903C636FA269759FEF0AA6E6120B65B8D6A87963BF7625E2086375B1F7164C8C46A6868D81C7AED3E10D489697122E063E2C2357515992E1CD8900A7DE4BF0E0B2AD491F29ABD99808C864BC515E8871E78FFAE48882D077437D6E64CC8D245B8DB9B1728D8A421A73048099F460820EC08D1957545B68C805AFDC7501502A4B8607862DA2F12A91D228125045DF618D6DCA2D43AA5C4FBADC22B4E87F8A9093EEFEFE78894B3E23C0F71D4009E69F76709D09FCF30D064CF46081D4226101F4644BED10ABCEC7B73E9896349135029C610734913676DC486C4C8C97119551262C90BA230CAC8C25D415490D5F587491834EA6C6D4E6030F66D803B521E1C0926426C5D4A246830A70861DD1E85207D6A4D1AAC2ADD75577A156640E0C28EF0B080FB107BA9CABE0118C1760527F5F38148356EFEB6A8D9A46668F4260DA4ED0D00AD6183184C115D112199BBB46B7FB72F30EF44409307AEA47506F364C88001EC81079B041A14E78103DED0A2562C583A554C774EBDD447D8CC036537DEA60043532E28E2B3744594D29394436D1C572E22928881590E693D697D82390C5E7D2DD2E6C4B754D94835010CD0082FDD61ECC733822AAC93E73C2AA4F2224F69A2E96934B4141FD9DF23F287A4CA3900C357C50EFDE67CD4A094069C9D7AC93168C48B44A32893356633A0E193338385D2758703943E6C41E5ABE7028A15BB3308FDDC544B1727210DA4155AC9B16A29166D5EC54905C20E8628D1E930FBFA1224C31D85D43784F238DF3630134148F701E9D9E50449CFD647DBD90C45C4D65EAE5F942125965B4B65F48C2A8401639F64F21C2C844829152804D7A99447CA776A62448AA0E8F4026848F2AD7AFFB23162917648B808BE33D2B90D4A2990B15DEF21106F4B767BA040773F5D8964CD792628162CA35042B1627C32E275A98887B268353784C656A91ECA97594E7AE32D4458849AEFD930F295DAC21543EA0661D4D329C74BE34E951990824275D808518C065715370424417D56925E679A35E0303FD15CA1A2AC782AA22ECB90093D5299D17CA36523352265D7260CBAEC9A42D10E7924A5F970A7035A5D6D3708D8536EC175349CBB73183A129D758ECA0B7CC6054C2B0D148F22850AB2E1A499D1BF7A24B9C982252E57554812BFA25AF05B20DCC6EA9E29B027F5CAF25D4B2CAD1AF00B7EB9EEA002004FB121D0C002D4BA336E4C0765D93467304615CFC280F28F3AAC64451D75074505911E4219C3E56C544751B31751FD4B274A190025B764DBA5884E15CEC0563CAD2BBFA1595A8961FD7C0802ADF2A000F3B6AE724C24A5F2D842F2A67C81D05D5EAC941A868A70161260C2990951C60B0C45D0987C6DEE41BBC38C8248205D1523E427A326042133FF896BE4728F308C1900ACF0E584111886A7431428235EE7A0F34AA8D54B6BA212AAD13815D89A3980D6E8A4A20CD7B258FEDF4E155921BA35C7A757D2A161650D11B085E955664CE016F905884C59A3205CBD8B2FABAA902DABC73EAABA65E7C4B74CD946BD524718D006BDE2993E4354EDC4A95C0867B4E5E9D474A045477488097B189793D41C3251169FCDC51549BCAED9B14D6B84BCA2D9B07AB926CD3B8365539647930E38EA8F2C93AF226F6468C79AD63CED709B24602AAE80B5C036411F7AC888A4312AC31EFC783CFC98317716148796FD42FD533AE1FE1A914D5BD5B186DDCDD1BF866BD8A4986592754AFDB430C32644DA2659F7F6F5DC910EDCD76C9A3EC3E6C4870A15DF676B87E0649DE195709BBF8D278A059243E2E6EC476D70C00C0BBD7B2ABFFBA27B2D95E691EC9E6258079054977D75FF12C7624A5C3BDDB2CCD1D2179DA59EC02F0B833CF92E115276DCA08F12D679FE9F5E2A4AF3D3D3E3C95BD38B959DFA36D367C78718241D668D7EEB3E2AADAA0A2190BAEB2DD8E307EAE397C7974B3CBD698EEB3FF7AF3F8D19FDBA26C5E3EBE6FDBDD2F27274D87BAF9619BAFEBAAA96EDB1FD6D5F624DB5427CF9E3CF9C7C9D3A727DB1EC7C99AB1BA5F70D44E2DE16E63DB862BC54D634A2FF2BA69CFB336FB943598EF679BAD00063CB5CC327062F1D820F58C0CB42D20D0E425AE119CFC9B7E011437F50321697876994330B3F002F78A1CE2741D44D4404BEAE19A37EBACC8EAF1416B9A50F29AF55955ECB725F7919739391EB280F168C66F229617275C3778469D089CE2E496E7BBD1A8F4ABACC7A04026BAC198C0D5C2B0528585FC4BC4D37F35C7F47A9BE5058B66F8648EA39B6A2239D467735C6F3308D5FCD51CD3AFA8EC5C6E349EF19B3996335414BFEDFB276C694CF4770B6CDD8E9D1FFFF9AB39A63136E34DD6DCB3D8D8127B8C3759D1C218FB12738C8482CB969C75F52B3E8D932F33C77AD99CAEDBFC0B2721F3570BB9C5728E366FABBBBC3CABF625D76BB1D4428AABF5E76A8F8D3D6EA4E9EFE6D8AEF64D7B769F9577681C09162B546E37E3BA5E9EB6E2A49B0A2C64BCBFA5C663A33E5BE8B97E4FCEE3A23E5BD3F5EA0152C142E1C12C6B9393CF636583FD98066B9BAC627CFD26AE01B6FADF5E4F2C68B81053DFDB7821481C0D18B86A5C2386B4C96319BF9963391DDE0AE57504FDDD1E1BAC25C4D28311226F0172141E3BC10933E4FEBAE11C35EB3ADF11DB8345C4141CCCE0CE0F6CFAEC3907242EDB4E6955E93C223584E9337EB4988FE4AD50702AD20596F83A9915B1F59FAD7009F2337EB3D8804D23C8EDC2A8EFB6D87866CD5F2DB60335FA9257FB86789E38D2B8228B898BBE02E8E6AF3613B8C5E679C34FDEE1A38591B03BDD6C6AD47098A8CF710DDF85140AF53EAD874699B038A814455D197FFB2ABC6CCF5F6D1C1605DE2AB5DCC679FE6A8E8908EE9E139EF19B15961A901CEAB3851456DB5D812039A40B2C68CBFF13BD7A6811DFC9F9B385C537BF720CE974A0D86293DC9F73733BE3F1E3E1CCBCF9B6AFCFCC1BDE1F719978D2AA7289DA092BCAF8CD7C7C48BBC2DC1DBE2DE1407B95D5EB6A837865327CB4996F25B66A044380FA9C5A9B9CAEFFB5CF6B6047447DB75969E7176C78BEF365169640256894E193CDFEF1EBEFA86E04D6D3DF0F6ADE7B4F79C7E96EB1C40699A497CD276E48FA2FE6183EE42D6FA20F9F2CA6C1BEBDEF4E45D92DC8F0D1C2A486A6F76BFBC9FD7EFFA9C89B7BFE2081FA6C8F6BF31F2893E01B8A6C546B8BEEAA5A54F5D4770B55D68752F3C8A8CFF177EB72958D95D6E556B016E8EF96D8BAF3E3B215B77162A9050F336C90D6F99A43497D36C7F507429F8B873EB34A972895C62814DA9C1EB40857798FEAF3EC813F3F608A6CCCC4AA7B55AEBF89C15A8874898D46EA3AF789572AF4771B6C1755DD671C61914D9F9738CDC2BBF25B54E3F57D8AB2E6E61E08E0707E32BC842C3D42A1CAAD4F7AA4D8A1F2E339935AECECA853DD9E9B8D7101703A3F92554DB30A2DE7293EFA53A9E9BD730FC9913CF26E2038D29AD2ED3FAC36DC745190E3ADD7DB5D513D2004458FF0650733EAF3AD1B8F61975D313218777955B9A109AF766E8B5C98A127AEB6AC7C10B50F5360E56368B3758B2DA906F033D045E6387FCF5A4832A9CF16A6C77D55F26704FD278BED1AE4875778E1179B20C31551AFF9015E84359A1E929A29B661FE8BE9D12C7EECB3CD1E43AD7A9FDA60BCD5D5D3BAECC69A328C76D8A677C379744C8139BE7FEEB38E6D2CB2F9AB3DA6D335897B471B18E35C7A6842EB69ED8FC3E921ADF6921A46A6AEC9FE1695BCBF84FA6C299FA06C5AFAC182B8F4CFF36697B5EB7B71DBC996B86084CFE2A0729B91582392AD9AA796FE6E8F4D766AC897DA1D4388A7100734A7B9740B018C7230BF8485652EA91F7B1E859CDB61B70C71FD6261F447C770713A529F2DF6B57FEEB0E98836E742200F5B627360F9B9ACBE166873071D5AB265718E180F61767BDB9A0236DF596E677542C69CBD1D174A5F1C5AB8C168370E530E362AA7427BBC83C524C34C155B3843CABC3DAB1ADED13D7D3D9879345D02F63C5F076E3B1B1EB1833555B2892B40E2397C5E685DEC136509CBE2FCD9E2049F5C49E6314D1F2DF148BC187C99C58A43BD7C2E441673654B688BE1E2BBB8C4320596018624AF001060D87FB60931DE8898A68F164198FC29B4E5C973A8D0CB4EB8C1E841BECC1CEB9BAAD8606B28AF51C31B345C91859335CB05EB68FC66399FAE51B3ABCA8D686D89A536FEA1758197FB6B94094E6AAEC866460D4FD50B27B77481033EC92D40B1D81677DFC7E9DC1340CF43B8B520C7BC64B070C873F6A30AEFEB260EBD72F898225A64064689018EB48B639C853CDC924BF74C5C77C5D2E35E7C2F1B626DF2BB85F9EBF7293F17ABA67C88E3401A91EB4CB73E188C331D4359FD218F2DE7ECC7D0C6662EF9262F7AD1A99F7D5C4BD264D7263E2545E5748346FEFA9D5EDF54753BA47166089A3F1FCCB00F09C17C9592AB36B2544301144698B89D7FEEB3627ADB4D70A4D14576EEF23A27AE181290CFED5785429B2DE6BE2E1EAE054B85FE6EE12ECD2BCE19453E584413E16E8CDC8136E650B92D7651E3D2DFBF49F5DDCD049A77FE739AC6E63ABFD5384C64849FAB7CD92226C7366FA15BB87481953B24172DD9F9AB8D8B774C2E0B1F7FF3A52E98F19CBCBD95A39E8AED718B2100F3778BFD0A71C461193CBB476B71DBC2173A6A7C10390CF16D1D4392D1F5330F31822BD4858DBAD8878ADA8A4DB528B00E627A08B1DE32ACFF567D022E42CE5F0F48808627793CB718487C78C8707F01D64C71CE15467E86A621F1E18A96DCF75E64EB7DC187584E1FBF49038CF8C3FEC8F074CB1B3FF1A611399E31CBABCB183DD6E00580FE9E3AA422E4E40BE7FD0D770D33A43FB47F6A5C44467F3F98A9729EE5C5C3156AEB7CED17803CE371894156D596F1B98717BDFEF4772BCB17957B212063F86815DAB12F370D8F67F8687370FC4058D500396EB9223B5D40226E06B1060359E642EBC087663A7F04C31FE8621B7E92FBEDCD905589672B5B668DF51AB5FBBA94A09D0B2D02E1BEA07AB34703062E1E8E2BB31BB7E6A62A80111B3E5B98032460F1B269F67CAF99020B6D47F2C08D6F3B32EA8E2EB0C2373E8CC06183DF4B50E13ACB767808A13884F9FBC168E20BE417484A0271EC352F584B2A3B4888151D3E2D14AE2624EEB04DD611FAA2F8E95654D7E3B7D44152216D1AF2DC1610D0437DB65E8502055212652ABAC0C6AFB698F05FF45586902AB4E05C2626CF1DBFD9696D61EA49C3B417D260835D725A145500BFB780CD41BB19E090CEE4A98E104DC394585B74428297F9B35518A6B726B6D355CBCA54084972971F73A90939C6071B24EEBDEAE21DDA7DC591327E4BBD4A8659B1CFB27AF3AACEF89743A8CF76B8C82B1ECF455CC367735CBFE2A5FE6BF6201125B1D4C6AD9817D88686E25AB9228B9E93839A6ADF7EA83E233E56962DB2DD8FBFBB95CC4CA0D8EE94483C245ACA460B1D168C7FAEF1CE5D768409141FCCAA71E17B2BEFC2ED221E5C2D8C5117DF04F657EC6194E979F6D0BC15DC7AF3D7D44B04A4E31C945BC8A9EEF92A92E2E8417A639F2FB3C5CA777CFE6A89A986C6832D391845343AFB3C54518FC2E5911749C5147BE9C3B45CC3E444271C8122644AFB68A33D128F2FA68F3614F59E6A91A6F97B6AA5E97B517070B600C71F6C894DEC65F8EB7D3D8725D9E485C2B4B6578A85E8A8EE7E305EBC003A79C0E4AC9AA5F535730250D1F67EC9A09E533864B0B48F181C1F290235A3506875AA04A2A4BF87D76D4B0576659EAFCA75E9B0EDA51AAE16D731FE6D9B1A401E79EB1CF26196F3188BE721383F169AA27D2C508053121A91C394555757F8BDA47702F8B2D4419940ECAD34F056EE9FB67B952A9E43850CF4C79A7B4F61FA68132987B7C6C2BDE1FEDB3213F61C6DF6BB7F475C5CEAFCF560A6EAF83AF7356A503B3C76E6791C2522743A9B3241235F37BB0AA2FD377D4E3D71FBFB20B7391F4A4D7F4F3F850F2F14E34D566EA4B95084426BBCC2EA3E7FB6B1CA48329A068907246C890DC65E3077B0BCEE0E556174E755E1D4457FFCE5A92C2448A4AB180117EEEA8D1F93AFEFA4E13759730FD0D37F5E665A4A8C6327C3187345206AFC663F656C54FC5253A66A5AAC16106931C8122BE27399342658A45B37AAB2E0BEE3CA96899D3BC4C481EFEAFC2E2FB342F4D4B025F618BB207118E550648FF3351FEFC11458797B77550325E7654BEC3102BDE68AEC710ABD660A6C8F55E1A46BEEE9D6C69B7782E5C1141CB30148A5FB832C2CA0D8D67003720C6AD20B2EB56ED4D56D5EA0FE15B7200B0780D065E53042733C7BB38B1C151BF24F3E5865FA6C3115F6758D79F67B56F0B7ADD8126B43076D009C7C59EAA88FC3531FDFF6FE71B1A3C55D55B7D77BBFADDE84C5E948515A57CEE5A18AA869A8029B5123D5049F1EF5D92658BEC6FF6F117F0F8CFE6EA1BFAA7A9B7182397EB33AEC86C211A6AF1661B9A844B53C820B2876C0CDCF44A6E080660EC92CE3396D080AA7390357940B38818713EE2CB78FBBCE5A5C87A369F8667580D65D75E00FCFFA8FCB2C5597CD9B7CB3E143C1E7AF168B5357035AD2D9125B8CC26A377DB5C524598FB9B28399B6C36E2AC8C38A0C2AF75432F6CF2C32F5247E10E7ECA5A15C2BDFB3AA8612D9F1A1A8F24B95AFC3BCDA33E0F278B3478A41AA14FB0AFC00529F6D642BE49B5AA15E011ABA02E555E28A52DF4C18FD7B1778732F6E87C5529B9989C56303E3E5CB2C76FFBBA2CA006F0EFD3DFD6D8AD88911C3242F0CF372C751BC5588B767C3D1B8B7529C5139EA441502CD613F2F4BD4E7D40E337112DB4EDDD7DB2CE7E27E864F168B73B8A78D06DB0A3241ECF63D9B077ECBB3B1F4EF7F77A8FD051C6A370F4D8BB624BF48EEF9441C83C94529A9EB4BE5B4AF2078C3E8EFE6E30778B6AD1DDAA173DDF4A71B806DB1B19F434325780E09850723A5E40197E61EA11697FB3EE1CDE0727A3C488340BAD210709EE7D3479BC8DBA17D210E802E58C647F647557F164FD7E7AF3681F07BDE273D7C8A3F139796F230F2ED23DA16516B81A5315484CB1F087D06A232A8CFA96DA10F559B158058D3DFAD2CC5A019F98F65BBD8D782ECABF9FB37E956EBE600FE3966F9F47EEB62C6E5FAD2850A83DC5EEB2A089EB0F9F342517A811F953DC553B0DC64C28D39FABBE56A2879C59A2FB3D4B5C0130CF4F76F772E110D1EE0693A8CC5F95D3AB06EEC95316616E1A9F2F856AC04F75CBCF41CE86ABEDF7F2A726CAC4058A9328B7D569BE34ADDE5BCF7A85E0B07BE50B9856EC1BDCCEE107420CD15D9AEAB601A08AAC0223CA2AE9AE67556979D5F810D90608B6C68BCAD894D09981254417A2D73DA34D53AEF9FEF32B9D6B62246D463BB9B6B431D97DB6958A76D04A7BBA485D587ACBE03F70E565EDC95CC4C24AC9C88F1A1F306CF4DF0403050A8A792D029D7C8A3CBE6B77D51BC7C7C9B150D32E6C38B1350642CA40A087CD54A95BC8E4B5C2D345AB2160E4DAAA474FEB5A58A3D17D2C913086D77D4048C0D80F5C0A407A2D04E6EECCFD116969B7101C42DB5595EA29A079956D8E1CBF4BB193F10C1C09DBBAA36A868E67A37EB7BB4CD3ABE34BB6C4DF60B18E222AF9B9608D9A7AC413DC8E347B8FB5FF20DAAC7A382410AFF559C157967408D00575999DFE209D8DD557CF9F8D993A7CF1E3F3A2DF2AC21166871FBF8D19FDBA26C7E59EF9BB6DA666559B55DD75F3EBE6FDBDD2F27274DD762F3C3365F6353A5BA6D7F5857DB936C539D605C3F9E3C7D7A8236DB13BEFA80D608CB937F8C589A66C36426A23620A3B5B7D9E6DC06FBC5BF23410A46E9C0B6105D111A6EBEF60BDEBE1C2B122A5E3ECE0973BB893D85B6BECFF036B62EE7D7051F3F2292977D2AD0247D27CA364651A49AE019FACB65B9417FBE7CFC3FB15C7F2C73AC547F79F4A1DEA347FF4BD31A6DCF2959DBE92C07CECAE6918EB140A763F095FC6B6CA4FC92D5EBFBACB663EF55F6E75B54DEB5F72F1FFFFD89350DC3017A20029E3EB1A7A05320301BFCBA463243C7C08B256033AEB68364586238C31BEA315E4B41DBD3BFDB639E02198C262BA9F4CBA3CBFF6B35D6FBDBA32E38ED97474FB433576C7CDC01F477CE7B0270BF3EE565462E2C523DFBE9B933F29BAC68D5C87F7C668D9C507CD99279DDB96B3D86767E69ADC7F129B7C7419285A3CDDBEA2E2F87BDBC3B3D6FAB2E4778770FB5C7420CE936DFA26764151E5EF17DF99899066D2D1EA3F388AFB0509DDD67E51D1A47C6A7CB64AE761D3E6D03D349F9348DF19A69EFF9503F06C1F3F18DE54C66ABCB26B44889F14ADCEB0A97B5581666A55F8D41BD167A3D5E7A2DB6D71D56D6D37545527EC9BBF3BFC299572028693F902566300FD4E26F341E23C1760DF7B5BC1AC67BBCFCAE8CA1B446CCAEDA85AF1F41BD7462EA2089AEE2058CF2412B961FED479D89A151189CCF9E68562BE3413CDD6F72B2A03BED85495DA7BDF05871549F775146132BE9AA769C3E7355F39923A1A057E78AD1741014B2FE403262D2B561C4573D8E003BD349A6FCE8E9F14CEC7ECA51F6DC95B279F483D0F5CC9FAE31193439AB568B06DEA75BDBA5DDE3A531109FA3166F781A25DEBFDBA3BDDC9D6E36356AD4889FFF3DE486C266FC073C64D82D9674630DFC2A5B7FDEEF1C55705FD94507CF3523AEA7E4DE17C672AF1E59075FD41829E8BEDF1E1E5E0E6FB931499B836E386FF2FF44AF1EBA082276E1B44333A59571B52D05045E36F494D8D447AB98CFB6AAFA4C72993BED8AC11CE8067BE2A15EC48946BA653F907D2DAFD15BD48FF90A8B4AB559D462A75E48F1D2459EDAEC74FDAF7D5EC7506743001839A376D863B3B57D4CE92174D26753046D29E7A75E7ACC75F5F5CBF84532498982DF77317C810C003C0B9DD6FE61CE5BAFFCA2AE08AD8E2E9B4F569B13E5AC7C662FB443D67C7361319B64FBF6BEAAAD84D06C9BC26A10833DA2896C8F31969AA32BFB4933056FFE07CA64A76B46F6F99021D365F1186B7A2D1F541A12BBF6A78A3E0ACDD4D7E3B2533BABB012BBDC5266D57402F77F6CB33FFF4F277C245005EB0378A32F75211819B619DE26F46FC30C8B175EB0B659C12C5D24E08444FEE062FB73437245A978E85F88EA1E3C8AD40E79E413E37F8FEAF3EC215A2BD7A82262D7A5BCF13B03ED38F269D6952E478297CD455577EF277921F13F8EC5ECBD4535B682E8143076331B40E133C7A733BEABACC493D1E79470C2E043CF741BD9991E114300FE1CEA99AFC549EB90ECD9E9AC755E06ED4F5BC125F4A08F457E8E772CF2A383F331DE51EE30435C64829A9EB62201CFEC638A34040CE4EDAEA81E10928562C5DA58180FF5B85AB88CB53C5F997EB0250BDD718F36D95F67E503AC7468C3D86163D8055BAF5B6C9F357AB3DB1AFBEF596B102DF8CC7EC7755F95BAEDACFD6ED6E0D0E599CE96B7982140824BD30922C91669323FA01DDE412F8F2E72177111A31D782E83C7BB0F6D4750E17E0C3D8C6353EE5E4E4F7F4457D5CDCF4A55F522E19FFB6C609EFBAE72C471BA5EA35DE727357411590BA58F40FA08636441A41E005ECE853AE53E7510445FBF9CF771C879DEECB2767D1FE54064C2ED7A7E2A62F08B7E446B84C532FC79F388D8FD9C98ADEF7BFCE3B7F7B336E63B625D548C246DB05EBF4073EE9B542EEE0E3ABD5FCE6C1F90D0319748E7758DC75078AFFFDC611B7A7CC76DC66D1F0CF9B9ACBE166873174157E98F87C3C590300AC2D5307635881318C20B2EFD7FCD3896D1621EA67008E39BCADBE38E0C2BFDF6AC6AE6B9EA77906515112124AC350F8A00F39499C5450C55BFF9F5777ECCC372F91D2B7AC9FB9460CBAEF1A19A7FD3CE07FF6C6DBFAB435FB21CC3E70513A16E7C3F81A97DBC9A8F4904089B16E6D1BC243BEA88A5EDFEED67F7949BC01863461E848888EEE67690785C11FB9BAAD8500F8A0735FEC6A72D8222A5DEA2DD443058CFD1BAC0A6DCF87857D8B8C6291760F84BCF2366E76BCF3C02AFF8810E19FFE0B24744DA8C2EF89044BB16102D88E220A263BB69482F774E3715B9C5D6D634542CD6A1EDC383315016B50B6886FF458C83CB866C40E60D9B4B04DC5F4015781C24F353CB560B28A666142D6039F73EFE9399FEF345DABFF9C764B829241945CF4250C43CFA1880A81F03101568FE997B22272638055628DECD3488AD900DC041875738DC6FBFA9EAB63F0B32DE5CD9A934675DE6ACC4BEA978B17FEEB322BFCDD799C9550AA7D38F3A270E3B72DDC56B834D529F170FD7B43913DA0BF02AAF825F26C10BFFC45E7F2F438FED70B741765397E68DCB34A6EBBBCC66BEFE819924819CB6CCF33B81A33986BA3E222D3EE4631B27C1D6F7F188CCB8F054BDBDF521664010829AE0937D94E2B37BB4F6DC3331CB47007C494FA2C9105DA12EE4D8C9001C44C4DEF61365EBB88D88A5AE188894FC5BF5497F7DF9A77012D439FD5DB70FC321A2CBDE413C7F3C6E091AFAB4BC0039EF8C87F3DDB1AE8FE2BFC8D6FB620E230E75F53CB58D473C727F647836E6FC5B1D665364ACEB3247E8BA11278943C4CDC77FAE68C6847534394526C8280AE16872F283CB080AE1640A71553A9AA3185B3F21B677E65701B2BC78B8426D9DAF9D2E5A7635A177080D6E5A52753D4E27B08D8CCA7D3C9700EEECBEDC34D1F0BFCF1EC8F35F8D7706F421FEAA1964D32B94417C6FCF677B47B2463443C2B80098AE51BBAF4B3F54EFBEA07AB34703465FB6373755E145CE455EA2E6B269F673AF820B5A97D7B25B1DBCFA8BD1743E092F2467D90E8F61424B84840E39E8375CCDC5F618AA45343B968E407C9893D8385D30724BD46C76F6BDA535E901068A45331E6E50DBC6085A82237B0D03CBBB8A7E4E30A2A35D3C717D3DFFB6F15FF4D59D84A9BA0F25244190C36CCF5439FA8D76A24EF7692F2CF3151AEBF1C15A3A2D8ACAFDD460AEED14CCC4D48EA8E387BE3A64611A2B7AE9787111331978E4FD264448F56D2B572ED2440D93AD28C1237CF857261C12BD1EB9C18237ABF795DF9ECA3F496D5CC3E62CAB37AFEA6C7E402B50F61582973C7BF55C89F7B92DDA5FF1E4F89A3D2884DB2BA722799B0CEF502205339363B26ADFBF063A22DF77F32DEF66F92D4986646DAB1047C5BB5BE765434010F5C2B90BDB6219CEF182CBF1CF356A1AF7C36D014104A3EAC2F10EEE85E31D5CC0A60CBFE6C5D93424596C222BFAF3ECA1794BB95D9759CD2269D6682A62A1D70AC1832D8FA4226C6D7F3A826BCC0E6D1D423ACC5F62EB26BDD35B6C939AB1D581A0823A7CCBFFF82E4B03AF8798CCD7AE96675EA1324EC8DD1E053834EB8F50C2BFB1E3BD2EC4BCE43B78E2BC8FDA625EC6ED8746F72ECAA1DB9E0957E683B85BC5787A1D5792D1C7ECB09400EEE96FCAA2EEBA184378C6A7044328D5DFD0D71068C22940F328C6CCED29DAF150C23A7E513CCCF86EFA7C43A64FDCC748FC6D8C98EBF7C13BA98CB5421F8BE67ECC46D777D1127CFD4309759E660A4D60F70EFBC7120FFE3C759EBAF88EB4B1EB4F1DEE603A24A549E2E97A9B979F3FD685922EA75729AEF13830019E0186F3D972379AA1089ACD7E8767905F88B8C5F968D37CADEACD356A503B3C60EA66E67655DDCCDCA96A5A4DE072B5AA3FC0311F9A60B3D861B61C7044D39BACDC78244BE2AAFB6C5F075411B6FB24435683629C978D3366A73E79B57E199C7073E5362254D528D1419496EA0F571D745457D12937C058F1E02C950069D2BBCEBDC99AE931F2E94944F73012872085589AC6653B603A6031B406B4169ABF3D1E67F6554D8B751922C1031E26028DC629A88AAB1F7132C60B8C3D06F7C282190DDED5F91DD63D85B7B36D44D4DD4E193105C84636E27D3D875305C04A7E568D3A95BC15A2D0DD1EF186EDB651824AAB4BC6118CAD03B6A2A954ACB10CCD30695ECD979ABABACD0BD43F53FA97DD8E1A5E2D43C586FCD32240D24C5CF7758D7BF57B56EC230854C75CB4B1C61E2AF6E8FB3EFAFB3EFAE0F7BBD76857D5EDF5DE699B3B5576D37E54E5A8C778A41D9D9FD3417BBDCF6AFC7F4C9D3AD790831AB8A8EA6DD62AB13EB517EFEAAB77D4CA342CAE6A4040E065C34FD8925D02EED369B9CD1552D36DA28C350F681FEA9D43E31A232CEF6613BF7C705AEEAA6D77E9CAE2546AD113D8CBE64DBED9CC37365C3287F418E2043FF7B8832FAD3D5A67DB81A91D61111CF6743E2F2433183C1272A5496EED9CE387A5F230125C4B685A3AC5B584ACA34C723D3EF9587EA948588EC30419AABACC0CAA6ACC29E1F620261968963BAB3EFF5CD0B9B1E07B8043AFEC93EA99F2079CA7F4E1E2C1E5EF183DB3177981B46E1117E74227DB1B23F42E41001F774595D9BADA92DDE93ABC4CBEC7F15A56E2F799F19E7A0C2671580E86AA2ECB0155F5908378A2CCDCD7DB2C5787BAB9608DF5CE9091C773B0B943872BBEAA366A87CFB3274E8AF9BB7FF6F0FDB30E1BFE83F5CFDE3C342DDA92B45C8ECFF10E553BFFA7F59E94AAABE0F74FF6D21AE750C634279C834BB63F278C306F07C4AEF396AB1E4102C9236BCD3D422D2EA99DDEFEEB2ABA2CF553C5880BFDD43D873094B9AAF793EBCB25C7FBA3AA3F7B4783908775E225B80D97EBD15EEA9DC23F6999B20E019508E421BC26435E6E1BC9EBA3B0FE40E87350570741C8C414F5F2E849591867A0AFF1F701135FC49D2A6E0FF5D8BD6BF30DEFCC7BC4FE5BE9E57DB6DD1CC08BC39088DBCD74ECAA3A9D66CC55BFA543CBA15BDECBE529EE79B9C9A8BBC5AEAFD935E7A820D96E2266FDEE2449FFA692CBC3768B4C0AA2C7177FF031D5BB6D21D3F04FC8C607E48F4078BB66DEEF3F1539360DE235F3B1CD710BDD48BE47F51A453C7D38C50CCBEE101B3F61D38AF93AE81F2F54574DF33AAB4BF2AA44348EE0295A13CB2F9A1E396D9A6A9D77A34BFB8B7AFF27A74A5E979B47D7156960281E28BA41C5ED0FE3A7AB7DD1E6BB225FE3C670E7850ECE4848430C8AFE038BE0BF080886DC156D4E5215954D5B6778F844159797EB7C97152CC11C18A80B6797F1098FF56442CB979CA31D5EF5305574DF7C5B9B90724A59C7811727D4A89A0CF6987E4F3ADE4643F5E4871F74C34DEAA61A72E321186D7ECFE1EEFA6634E47CB2C4C5469E10BFC245F95D9964F8C9BF043CFDC76F4204BAAE98B437F3FC306440249C1A3961D4A0113BB4A1371E0A029872E8E5EDA51C707166D84EF7431BF0839DEB8BCEF0D3CD362F238F76D7068361F872F4E3DCF7E3F00779BFC9DBB7D5DDEA748DB7D55157F1B12976B8A78FC73FE263578C5671C2EE4547FE55B6FEBCDF91A19FEE04FA9A71AAD19F9A63B0505F8F7EFCE7BE18ADE533D3971583AAFABC9A6E80CB37EDEC15F161DB3E7EB41003DC1C2B01DD87481B77A85792F118817D858074C7BFBD04037F9595D9DD10A3136FDA0FAD3038A66F473FE5C79E1CFC32DFCFF2D1790092ED36689A353FE574B7198C0136D964E738BF981C8C970E22CFFAB11906C9FCF1E8E7FDD495E398F8EF6B82B0461B986ED7813BA0B96F3520F49DA614B35F60FFB2D200DFE7A46580893D1F4460F876C01200F44A2A0092FBA871C65FD95CAA519F2262BF2FFE89177F9EF38BCA01C941BF1249B71D35DDB093660424FDC728C36F3C180430C0C0775DF16B2FE580A738925F62D8531FCD5B0DFDC2C7F34310D1C3EA3C6F7659BBBE8FEBDF1B9B6390CC1F8FDEE49FBA62D21ECDF245CDFFD1E2EC62E5A238004CB6815DEBE0966228F9464C02B65747E918E8487F9B9728B29120B427178FBEF4E8CD06B14F076F3F884291C290380CD1486D5AB889C7C23686282080EAF35F100E5F5AEC35FF94BB26B9CC285A4E6E918475481EAE2DB2847FD25E2617F74F4E7B96E46A6489EDCB324AC36A23B3ACAE98C4E11AAD51FFBA7AD408A5EF7B58264065E4F9A23BD891E6CEBA803BE03A8226A22058146C411491B073350CC08144C3D88A50B79B5A30241650383BF15065C5CD042480A90566A2707977B89C76DF513538114BED23779ED90184C4CA5FAE6F37818C74B78B4FBF6479917DCA0B3C0E495C1E42AB0C3EA0F49B707988FD3A0297477F57BE3FD15FCD8990E5EBCE0CC22C38D4674B3981E233D882382B8DA4AF9261627344FB4A89557487A6E5E442122EDEE660652379F08DBD542C1C84D3111C39ECB26B431CFB6F60E7DAF7C3AFB15483FC3BAAE7D7CEE7F44E513D1742C3A214B0A5DF8644307D32736AF0D9BA967182B7D9EDED152209DF634762CF2D71F607F5FDE88581EECD512B89AE230A5BD27A308F426FD80E5F07BFB0FE50D0B0880C012BA4BD6570F8A2626E0848F29225101245CBA9F71E69A462A15DC702B260B3E3380031601C59F20EF88EE7E1BBB6DC062FE056D5DACDA5272091C73C2FEF5672EAC38DB4CE6F4EA5F59BDCE6DDB77802633D68748500AE73E16D03E76613CA4AAABB0669E561899B06A6C3BFB0477C1EF821FF69645F47FAC14F9A45C46AE0278E2FECCEE8DE748C721CA21186A1690159F7ED9B3A07197B15A2D1843211DBBD957AF8D3BAB5CC877CF12C0364090009761BAEC35A016C271F2A43DC2E375D07942D261CFEE8DE8685F601C97C0C3683BEAC7B8119F2E44E8585E460210782B5541C82DBE08F2C6F8BBC6913241B189B1210CD05477F7B90E98E5F9BA90520954360293158C23560250A07E01F988421A185B8943C2C632B5A49C4D206E3FBEC618B319D1645351C5F5E2079243629A3C7B0FB6D2E0B42630C32A0348A54081D940C0D86F39406B1475EEDA6138715A66A8D9A26BA0771680F92836FC06B30F6C4A4398AE38B3A104609C028F7E5E69D3C04C662E8CC8440785626AE20580D4E0F1B461C8C9F98198740D97A4291486431A4D709E9ED041BE13B3C1301A4DE6D040FDF5C584C4F589A0D4B6B096CB6AC40FF889BB748B3661858A2017D0BA63E1FEABD578FF137355095ADA518F12E0F519267E72EF8AB9F17F12EF2A6B4102F8CD30B1DC47373D7184D56ACC48BA4AE376B5563DE37C6BE59367C8A36D1CD534A6240CFB11F3AE3D75ABA414FB5254C3DEC29A7BBC5901FCA7EB02719FF455FB110401D70193BBD000C2D02B8A692384F0E9A0F510F1A4424C63EF9369B4E1D24DA0CA6D606E9B7821602B7F44EB033F852A88094E65EEA096F6CF22D3CCFBBC1FE234B90ECEB2F6FDC8F5C5E74A127FB779162DB613A986DBB31F32FFC93EB189F282D994F878C6F28DD7D30A39C5C791B8EF4D2BA7B1A6BD0B0F437B60F4C002CCD67029B5410946DA60801CF0A94C86B439A628DFE2CDA6BE1A93D365D570EDE5FD30D76A2ED59CAD14EBF35331EEFC5376658DF8BC4DA8ED3C1287573BE63C0448A5CDE54AAF14D33A3138E72F2F96C3CD60B4FE7DFAA764E2712F7DA16DD1483882D38FABD38D31DBF3693845C34CDD7AADE5CA306B5C3FBF4AB3759B9897FA7176A9A8BBB80008E5E40C06E99B43D0DCBE1498CBFEAF82E2C618585B4BD3A1C41F9507D4624E919D081D0A36C23491D59F21686E248D15E8E233BC08614ACBE9FDE8D2F235471CD96431197C5948EB96C2CAB6DAAA6DD5525229189E3AA14304850292262D3AC8C40E5DF480421D437EFD61712975451E707232E0B44A33B8ACBD2FB64485CA2673E381C41499B11C1554896CD8E80E9B9CD0BF471B7C95A9478270D34CD4A0A0870FC760AD4AD23DA494312137F27FD5D58EC8465F99DF435DA55757BBD2F57BFA212D5216E41A8232187E6B853FAE9EBD1CBC2DC17930629A62F2C06241179AA5D4DDF1A2702FDA76F64EF3274E718B62BC3D0BFC937B89DE8D722D20E7CDA896F3CE423AF0F21CB26FDF6588A580BA655085BFC67C712C75FB0DD3A862D29201A299FA53B0419593025EB913D4EC70B4B9AE7E9169791E44FD4B948C7C289FBC6D7842FCB2F55BE1E9E55564806FD7EBDF439652B29610900914E65F11F54361AB40ED25756B8BE79B6BB80A8247AE86E7901499A00DA412C0EE5893B4E3EC6DF5A6D020EE971EA10439D3F90945E54D44D271292AA6EAF50D364772858A650031D32B72AC8075DF44D240CE53AE5DB6E7AB9487372B2B458A45E582C45E230CE4938C9887E42F25D28B4AD2E7B1E72F3D0B4687B83DA96A4A3EF4F74A26B0ABA4D5626D892E31709A63F468D8E23B0EC1BBBF91635F708B5AFCBB67E4898218E6D988DF9E08ABE919313AE5BDE0DA7948E64CFB38F0DC202F10D288AB92F47B4479DE520FE3BB98B4840E21772AD6460E1E771D9054242BAFB001EF21A61394E2374FA7542D374AA5794F15275836D62722D3251BC05D7ACA87AE8B26FC58EE0FA75148644F7DA529B257B681D37053CA0D57D3530488E610D993A74686B08565B98B3B84E8B6BA07ACAB9B1411779DDB4E7599B7DCA1A311902A985375003FCE9669BE339DB7FA60672F87EB3BE47DBECE5E3CDA70A8F73F6A918AB348068B0987BB35440DC7F86F092123DDAD1C32A201E0B20D47D1932239A0CBC84F0BE48463C29D53721412F476D86F674BFC9DBB7D51D34965311389C43A9BE8957D9FAF37E07B74195418D4CC506AD4CE976C446A622B08DBE34376056FFAA1B885F8EDB44365B7457D510E973112C9F5DA909E9575999DD81336B2A815A180AF5F8E7132EA181B9086A612C35696208AC005A184AE006BA423DFE7354E45F50FDF0B6CBCC2734C216432DD110E6CD299A52366332EA5C008374688672D5F87420962D4A3809C0685B36E3E964A38273B42B914D535CA8C7DF2DD4A75FB2BCC83EE5052E025A0260A036013093C6E593802D9636693C1DE8183A7108A94270ECA67243A6CA3AA4E88921E6DFF1549973B1C0ADB030D2166930130666B7B75768FB099E7A7429CC420C60344AA80495E258221B1F5C6836A3E68706C1693517CBE6D60861A014F17C78B8426D9DAF21BD489782AA7106D0B7D5256712DAE8BE42B871811E27F0AE8ED0020003B5278019B72E6F53D99201C7607D7E2155E117465A7B4C932A1AB7430168DE7665A6C8A74C9C9236A67279530388C1BCCF404BBDFF0C4EC6CCC45267734A09E8D962A8191AC24496A0B42480604160B094899096640CC928D4440C405A123A380302A01BC922051014488208684003787D5124020403A900204DA6D2745D0E984653193C8586629356FA8B3940137D018C9F94991935888AD196AD9AEA1D0503636E134FD17A528B788250D9C3039059BB746407D82C0D206B75863168940D1D10DB64CBC1266910032B8F3B6C104D3C0E00B4EF681893AD157564226F50D396A1054B7BC961FB9586905AAF339061BBBD4356B2D1E8CAE4BB0C5C2CB642B92459DFD91832F98882A0FC67604425E35666DD7B18F9FC49F0A9F267E2549DFE0347F5094BB67197A6B7DDA4BD825F7FB32751A841DA88D531D2CA0A97E677A5AA773C58E82ED2FED0A956FF315C377B7C8A0E760072420522210297E95ACF6445D7A0B08B831EB5EE8041DA2FAA3464A7988390AECAF0C5BF3B83837D75BAEEFC2040975888A0DDE28E05FA9E4D1FBD3B3739F6C7FC893255020386ECAA703CD155A3BEFA77B6AA3EAF664F3ED0490640B5CAB10705C33A377E5475923AB9E8FBD77DF0EEDA703E209D754C79C851E3CE33BA4AD3B74023362ECF135ED9C8F18041C84E327EE39E423A802C40C811E44F8CBA5AF3C73063F8BE26E7E435DACC882583284206223DC938765D9836AD922E4AAEF04297A4997E0DDF96EAD6148AAD9B8702E0F1CCC3F1887AD5E3833B3803781228D4A00FCEA75AFDC7705D93EFF17890D0FBBCD85D1CCF6B57E779B3CB5ABC29965833306048ADCA9F2A77B5E68FC1168CEE50D66889D4D4083F47C1436F46450F256199414EA8A5D3570219721E4B4FDEC5AEF7A511BA2F9FE252D8D073FD10D830C8970917C0FC0CFEC27C482C59A96C2F09640CDB2B9D5E98D4BC54103888B8E31F7B4D983A738DD66878F256D5670AEC98963E3ABC6B35E3957794050C4438585398D46C41D8AE538A42D3F719329A725A841D93BDCAE25518B60C60E00E24357585B035C59A2F850DBDE64BC3F23A04FA483B47360079EA645CD0A5B463A7841079D7CF05EAB38E19D0469E2D08CC02B9EB01840BEF8248DA6DA9BB8C2A0DB9B431219273AF4275878E6BA4EF12CB3A28830FDE65287673EE3E5BEA6FB4CE11997277280F13D4232A068C0E139FFA1E75C4879853BB61EF2B05EDD6C29220F676986E268C012F9AD9CFE54360C1A8B5D5BD67A122743C9556674C15B60113E386A911B84B0B9B3BC30589158B16B67705B8681D110C5F2AE26CB27BBB6FC118A0F6615310315CD829BA57AE5151480FE301B0900B60DC4E0E619B1A731D808A65A9737737A6AADDB760DD959B32547958332666B746199C30CAC5549273DD8DD40566A374D96501C2AEB809BBA639AF16E062AEA9F1BB3D5E82521EF1B240A18F79F98B5C53CDB9206C57D5CB250F1663CD4CDE658D5E12E0E228A714DD162EA8ADBABB7362BF61407907A88B791DDDDD6F4587A597EEBADA4069A8AEAFDED7D51A358DCC5E82C042AEB4DC7D3FBABB013B798D6EF7E5E61DE817106082100BD51342DAC3775431757990F0B336C958525370C26D325F47E068A39B70EE626D32E5BF023A4F170731176C559B4B97BA780AD57D130E22A412BAE0CFAA2EC21CB2F5B76E57FDB995D823BA38D071185393BD94DCD51B3F85EA9A66F500A0428E5BFC0E0E37A657235A591739B8002403B5B81BE054E5A924D4B82AD6110E22FC321273543B25221F4CBA38F418C652321DCDD8269606ADB000C7A037C9027621894798CA3CC94AB1A621A5ACA178A216B93B93BA51F56A040AAED5127495A4B3902EDB73618C459B4EBDD1ABCC2CC8D5C28E6A855A67CAC32BF558DD22F2D6A38265B12BF3242BD1DC520C0E5D1C7E6CE27489CED522F5E08B4021D72728DF4C57932D08B03515F3C2504FE580FB53658DB01E1679169C61870A01C4618A4C0EE4C0DF1C2BBAA43DAB09B986172C74E4AEC93132698B447C43710C4619490C051A4D5E52B140CCAEA4F2F5A8C0C3FB7E1499A47A8E40E55158A272742AC023383D0F8625D2B3563970D873D78558012402D32CBEEA1A41358822ED59CF1310200E53A4AA540AFC2DB062CADBB6FA15EF276BB98319060CEBAFE4B2CF0D9BDDE96B80CE929056D582C141845F23D8E477430FFB4FA1BAF726DF60BD2C1B451624ECF8C5EB1AFF16BC62A70E4346D8B3839906E9EAE1A257814E6903DC12DE48599215F2102309A4A21B8E375392769F7BAE5A7B13990554753EDC3D642E0F2683652A0BCE0AF5BD16D377E03D95E0B22C30B988AD7EE81CEC0CD88B036383F83CB784079A77BC996EB8672810D3B24E7DA78B42775D69D99BBD561D60022CD479558A2BE55BCCC7D465C933C250B74D5E1CF6ED3A940CB8EF395BE27F81077E2917BAB863F0A66E105B1ECE4BDCBB03B8A270DDD75DDFD43E19EB39E2425A64B6BF41BB2ABF96A67A10D5DB5393B08BAFB9573DF5E22C7D14D3BD03CB09B5E4D54AD96D3CDD03976166359C917B961DBA2CD035CCF96546E91D4CC9E38DFEC2CE27029FEBF55F8DBBF8E2A447303D3D3895BD38E9F3880F1FF04F8C1DAF8757D506154DF7F5C5C9F51ED7DEA2FED739EAF24A8F285E609C25DE40E5D4538713CC65795B8D4F2E72148D2063F198D414B5195E0DB3D3BACD6FB3753B44E07592F17B56EC31C8EBED27B4B92CDFEDDBDDBEC55D46DB4F0523F2E4E54655FB2F4E049A5FBCDB915F4D882E603273B2A0BF2B5FEDF36233D17D91150D6767C850902721075F1E19CB96F8F4EE1E264CBF557C58900CD1C0BEE925CB0F68BB2B30B2E65D79937D412EB4E165E82DBACBD60FF8FB97BCDB77CA90E8078265FB8BF33CBBABB36D33E098EBE39F588637DB3FFFFBFF0F3D085F0E5F680500 , N'6.5.1')


GO
PRINT 'StudyLink schema created.';
