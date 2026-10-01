/* =============================================================================
   StudyLink - 01_CreateSchema.sql
   Books, Tutoring & Academic Resource Hub  |  Nova Tech Developers
   Target: Microsoft SQL Server 2016+ (developed on SQL Server 2022 Express)

   GENERATED from the EF6 migrations in Data\Migrations
   (ef6.exe database update --script). Do not hand-edit: change the model,
   add a migration, and regenerate. The web application applies the same
   migrations automatically on first start, so running this script is only
   needed for a DBA-controlled install.

   Run:  sqlcmd -S .\SQLEXPRESS -E -i database\01_CreateSchema.sql
         then database\02_SeedLookups.sql

   Enum columns are INT; the values are documented in
   Data\Entities\Enums.cs (e.g. Bookings.Status 1 = AwaitingPayment,
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
-- SQL Express creates databases with AUTO_CLOSE ON; reopening the database on each new connection
-- is slow enough on a busy machine to time logins out.
ALTER DATABASE StudyLink SET AUTO_CLOSE OFF;
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
    CONSTRAINT [PK_dbo.PasswordResetRequests] PRIMARY KEY ([RequestId])
)
CREATE INDEX [IX_UserId] ON [dbo].[PasswordResetRequests]([UserId])
CREATE INDEX [IX_HandledByUserId] ON [dbo].[PasswordResetRequests]([HandledByUserId])
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
    CONSTRAINT [PK_dbo.ProfileUpdateRequests] PRIMARY KEY ([RequestId])
)
CREATE INDEX [IX_UserId] ON [dbo].[ProfileUpdateRequests]([UserId])
CREATE INDEX [IX_HandledByUserId] ON [dbo].[ProfileUpdateRequests]([HandledByUserId])
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
    CONSTRAINT [PK_dbo.SupportMessages] PRIMARY KEY ([MessageId])
)
CREATE INDEX [IX_UserId] ON [dbo].[SupportMessages]([UserId])
CREATE INDEX [IX_CampusId] ON [dbo].[SupportMessages]([CampusId])
CREATE INDEX [IX_HandledByUserId] ON [dbo].[SupportMessages]([HandledByUserId])
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
ALTER TABLE [dbo].[PasswordResetRequests] ADD CONSTRAINT [FK_dbo.PasswordResetRequests_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PasswordResetTokens] ADD CONSTRAINT [FK_dbo.PasswordResetTokens_dbo.PasswordResetRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[PasswordResetRequests] ([RequestId])
ALTER TABLE [dbo].[PasswordResetTokens] ADD CONSTRAINT [FK_dbo.PasswordResetTokens_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings] ([BookingId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[Students] ([StudentId])
ALTER TABLE [dbo].[PostponementRequests] ADD CONSTRAINT [FK_dbo.PostponementRequests_dbo.Tutors_TutorId] FOREIGN KEY ([TutorId]) REFERENCES [dbo].[Tutors] ([TutorId])
ALTER TABLE [dbo].[ProfileUpdateRequests] ADD CONSTRAINT [FK_dbo.ProfileUpdateRequests_dbo.Users_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [dbo].[Users] ([UserId])
ALTER TABLE [dbo].[ProfileUpdateRequests] ADD CONSTRAINT [FK_dbo.ProfileUpdateRequests_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
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
ALTER TABLE [dbo].[SupportMessages] ADD CONSTRAINT [FK_dbo.SupportMessages_dbo.Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
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
VALUES (N'202609302150412_InitialCreate', N'StudyLink',  0x1F8B0800000000000400EDBDDB721CB7B228F83E11F30F0A3DCD9C58C7B4642D1F2F877C4E50A468716FD1D222657BF613A3D4059235AAAEEA55552D99FBC4F9B279984F9A5F18A0AEB824EE97EA961D8EA0D5854422914824128944E2FFFB7FFEDF97FFE38F6DF9E4336ADAA2AE7E7AFAEC9B6F9F3E41D5A6CE8BEAFEA7A7FBEEEEBFFEF0F47FFCF7FFFD7F7BF93ADFFEF1E4B709EE3B02876B56ED4F4F1FBA6EF7E3C949BB7940DBACFD665B6C9ABAADEFBA6F36F5F624CBEB93E7DF7EFB8F9367CF4E1046F114E37AF2E4E5F5BEEA8A2DEA7FE09F6775B541BB6E9F9557758ECA76FC8E4B6E7AAC4F7EC9B6A8DD651BF4D3D39B6E9F3FBE2DAA4FDF9C675DF6F4C9695964988A1B54DE3D7D925555DD651DA6F1C75F5B74D33575757FB3C31FB2F2C3E30E61B8BBAC6CD148FB8F0BB86937BE7D4EBA71B2549C506DF66D576F2D113EFB6EE4CB095FDD89BB4F67BE61CEBDC61CEE1E49AF7BEEFDF4F434DF16182FDFD28F676543A078CE7ED3232850FB4D5FF16F4FD8E2BFCDC2806586FCF7B72767FBB2DB37E8A70AEDBB262BFFF6E4FDFE63596CFE1D3D7EA83FA1EAA76A5F96348998485CC67CC09FDE37F50E35DDE335BAA309BFCC9F3E39612B9FF0B5E7BA7CC5A17F9755F7DDF3A74F7EC164641F4B34CB02C58B9BAE6ED0CFA8424DD6A1FC7DD675A8C1437999A39E9B02095C8358E61A7D7B1C8E5FB2CFC57DDF3C80EDE9936B54F685ED43B11BC47C1890DBA1F8A2A9B7D77539F5B6FF7A7B53EF9B0D6EE9432D147DC89A7BD4B144BC3C59844529424393F61244EAAD2840D3B0D8CA8FD97086151FF2AFA945ACBFB01A7EFAE42AFBE32DAAEEBB879F9EFE1DEBDD8BE20F944F1F46227EAD0AACB4719DAED923BDCC710DBFDE6645A968F5D9B7519ABD289AB65BA5C36FB3951AC6C291933934364B66F3F4C912D3192ACB5FF6DB8F143660E0FE1EA31367D976B76FAD151D87E57DD6B65FEA267F93B50F13A6574595358F4C17BE7F0177C1B1B59BACEC14AD917E04688DF4E9B2236A80686E3F3E5DB6A79BAEF83CCBEAAB1AEBF4ACB29F6D788EE37ED5F745755663FBCB8FAAB7F5E653BDEF5E57B31460A58E3E14644EBD6FD0A6180CC56F7578AEF06272F69055F7681A21DF7E92B9DDF7F2B4F323EDAC41448DEBD15812F8EB2E37C36B44DEAB47709932353286B90C9A19BDBD30952F7606F5593034E832C8D2505232F54741CC02C2D3339548489A8B6DA922D50922398B48A9608C310573A3344D4BE944B19349368D8FBD5136D45CD12C5B96115BC3CC74010A679AAD622CB8697E2B6B9E48A1AB454FEA1E9C550F82124AE36D00D48338B5ED83E3B46D8BFB2AC2523421F65C4416346A2D49C301BA722906B5380F63ABCA075957D137400094F57FA434F57F5C16169F35454A8DFF36DF5529ACAC105C67B9D90C4DB6967C17652D3947EDA62976836F51DAF673C38D7E60EB493DEFA4D613332B9D24FD749F1764B3E0E4161DEBAEE9192524387946A78AB3CC936D764499C7864CDD98AC3150BD413C124F18627A29E74A2457DC2CA28AA65F446C7A19A0640D6313E67351EFDB1B2C6BAA6E3FFBD64D3DF1FA177D49D4D239EAB2A26C954214A29DCBDD699E37A855B5F4C2CC1598C825223725C984874F3D467D7B3B8250271F4C8978FAC1167B9946AFB2CDA7FDCE71C5982BAFB8640C34B8AC194BCD5486D24551225CE941A990A26824A21FF6CB6C22E237747F2AB0C7D7C470259ED5DB5D890238136F8AFF44AF1E3BD4722681666F8BFEB547ADDC0969D99B2BACC1B27BB59F25AC7D4A7500D439F38CBD652017D50302081A0886F2534475FDE9ACDE3D3AE9A1B1EE9ABE3FDCBC93E76FAC974A051156F98A7598E3B2575943243CB91D7C565778F9A44D613231C8385025FEEA95207454AEA79B7FED8B2682763D4765F119356482880E028D8D5953BA34DC465BF05C7C19672C7FA44922AAF64D83AACDE3558FAE6F8B21E107C3934EFD6422ABD0BE43E27496EA5D32AD60853BAAA6DB018252B47481A8609952EB9327F919D88C583C07E38AE434399E87D1D2A7A68D850428A401E4743250DE8B93EBC2B4A66D3C2A7B6BCBD8689108B7285DB61FD54EBC18EBC087A2537A614C359AAD72DF770F75134295DA7A43B855CF79BDD5F83E88E8B60FEA289E208BC5D452FE1F286BAC16B3332CA5F7756360786996FDFDC7FF1B6D3ACBA5D4CC731DC8637356630D78B9A57621409010FE27DC94016EBC307778966B1C7C86BE450DBB33BC7D6E8ACDDCD039B685B659C95845CF30FF6F36191943FB01FD1DA14F2556A055979517285E3B6FB10062FCEF51739E3D466BE51AD54D8E9AB7E8332AFD04FDB2ED79F2715198AEC14C97ED45DD9081F44714268A0C33FB0E61C332BFD9EF766561EDD09F2371AEB20ACF32DBEA63EC946BF5430BE95298A483CA951A7EB70B046BF0CD05A0A1B7945A1BC9F5AE909CE3191AEDFC391E6CD25B878B8D92A0E09400C9738C0390708E87B2E5A03075E42403A01CCD02044CB408664BF5B868CB699D01380AC7EF305D53A175D40437FFE56489901C7D3C004CA800E5B5315A26AD4BBCDE5077D588BDC510B48FD933352293455AFCB062A4C577610EFEE24608CE53C75E5AC7AA2B0A2B6521D8CAAACCB838B4AB63C2359E5D593F22A4BD1612C843601FDB3629513EB48DFE2EE861A6D04BFD2EABA9BD404F75579468DAE4B61569A9B97EE0324D7CCB59F5A8D1E4CFE2F8BCC8663DDB7478DFD9EA9C0F319AFF2DEB424D65CDA6EEA1AED4BEBD28AE3D6DCCCA73373F88BD5E9AAD635E3131058266624B3D55D36845BB68A6BEEAAA8A6976ECD9EB25D827B89E551869324736D5D833207B21A2EBAF2849FC91ABAD38298F6C23CAD4D4AEEF72F7AEF7401A11AF46F4CF7D36121E02CBE986E4E740AEF74326EE80BA9739765C20171D0C0208BA1886B2F52FCCFCD7934A814A689D21D4C42E605E2BC8C23AF7D97F0033DF67D6A79CF1D7C4B186AA4D7ABBA99717EFB338310467E2A26318CE79D1EEB26EF310231067461D26D0F01A6D10EEAA77D4E484C7ECFA1C102214DE4124D7C1140F95CAED968514751B0320556D2C94AD1E26EA103E6C705C2EF8C307F5A262B55CA8B93982006CEC4BE4FC1B8AAD2F39CE22A9268B8603685B8AE504523041BC3223A7DC5D333D821597B05931DBAE5F861AFD6B58BCCCFD509EE7C8FEEB2123568E8B625F37C28AF8FA8F1DDE35A3FC9CBAC7B4A0D6C5CB7EAAEA2F25CAEFFD57405D9C6BE8807DA3535F66E094C7BF6A48A9BF4702EE18E1293B56375E42646B1BB7C2F8AFBD4CFF97BD8B94A30B8840A30CCE9658E5593A3B5ED0593A0CA11978E559BADBB2E7EAB61190ACB8FCB9FA6C52FB6A82EC9F0EE762C8E4A319979A30C84615E28B0E6BF9EEAC6E9745C63B66CFFE4A81A869F830251842AD0416B080B70DC416C46B07321803721D2F22C837391EEB819E5AF9FEC7EA1E426F91B85D45C05557BE8D802970BD903056FD13EC28F000611A7DD5E8873D66401024C6475A1A33FD735660F8A2649232ACB9D2DCA096883EBCE770B9113DD41C3085D8145579608C3183DAA13BE6C3BC75BE64DE4F8468B7A8B9E6DED4658EB7A145835ADF5DE4FBACF0DE89F653EF1AB5BBBACAFDF7B578B44BBC085DA34C1DF911E61EE7598615675906C8093A2172F2480FB5873E2F91AAB447847873F7251A12B64ECC31471A9F91A1F221C4BA2870B43776E9D5481A748D07F596056463AEF97230E45A000A7C9D97E0876FF32E2552C25C73DB2EF352431705071137172B285C60AC63FD87154449E20C2392371649499BCA6DC9EA55BB92A8114224A92F901234943A91A3BB1631A3975C8FE0CBD534AAAE4B18EF867A4CECF4B4DF170948D64C7FC759C8B6DB243B0B3BDC5E29E42E218C654F73E24F64DE5FB664962D4E2ED74B917F590BE64BB0A04080C558062328492960709D2EB624D3EE6A48832E04D6F81E91C574FD15F53CAF2F6DF5BC9DBE0DACE763254D70700E3CF657DB039DBC47CC01B9D02A9F8AF374A260B9392882C0930F800B7949966D43A22FB8720DA18E9767E57634AB7278635A2C55D32737ABCD0F47A9617538159D6BAF7929829971D6F7222CE6EB913F73715337DD786E64AE97ECD640E7C56FED55CF79B93BB67B7EFFDC676571576C327D9E9F28D99EFED891409C6A83487624CF17BBDE60E5583E5ED3FBA8D08707AF8A5ACDA410DE542CEAF39000A715BD9C4130B6D71C7B1407E4B3D52DF38A8D8DCD1ACA8744299659EF0B8E033AFE76E3F2155ED0E5F71A5544D022A160150D26E1970802330D80F37A164240EABC7ED048565C4B68325C9614BEFEB1EDA0B64517C33F733ED665D422CDAB05C0FA86CAE7027D713CC75B2A638D7C77E758DB571D4F637CF68036017C6C8C81100867EAE8E565600CD5225D41A51B17385891CB80ED6F97309265DD8BB19659577A60ABFE0C35026F8BDDD6293DD94176C9B8BF57684811E1B24D9EABAFBA4F1E7594FD1619526E07BECF499DA3876BFEDFEA8FBA44BEDFA7CAB9B1489F9876832B13C34579006F6F537FA6EDE66AC25557F6338DF18F2E4E26A3D0C9C39A4323D92B4D2185674E134C986DB0083C06706EACBE0F563BE8E73810D0372F9602B35B0009F6BCE6845B5439CB772941FEAA861C38FF9E61155CB44EFA86AEBFA2D2994870D13A74DD546A27C4759D4081E661223642E5738E15BA80B7566E4E3DB737432699025310338560A0150B1138E070460E471D72C56AFA1CC31B74817D337E49741F5FAE265215E7679ECE262BCAC72B8417C98D53469BA5FA9A39487B0286882A5B3549D7D54663E935E52F7D8A3FADEB0755FB78A7169831FB2A6FA3E17F9F3D6EB1E0B567F5BEEAFCF4EB187ED98EFA31C8C599760E4FF743373CB8D08E8F0A06C1758DB0FC57BEC8DE7D464DBE47234EFF01686FEAD293A40B7299FDB26DF74BDF828B5DFFA06CAFF23CFB8C11F55E214F3467D98E6833DF45DD5855F7B7A4EC5534AEB6A26AC6ADBB18AF63B554766BA8CB8D8FD4733364D1C7DD18BE45496E1EEB2DA82DADD7535CC8C36C723CD48E6557DFA0AE0B70514C7643D864B170AA84FFA22FD675C9CB37B65E1683C71AE5B79CCAB2561D5C8F06C60276DBEBBEC558060184436B18CAF6B07FBEDD0E104AF0CDE50B79D467C184A7CB6CF71717B28C9904E905972373FA06527021CB83A93E9622222625602A664918BE82448C456E648C92AEA1668682881A0B15B44D10D61BC1FE2929096543214B10F906D2D11784DC87F648C5ED27F519A623C4665398902EF68C8064CDAB5133114E17A398DAA96C9D917FBEB68E918596D6D090EB4D04CF46C775869F1FEAD5C894C8118B21A133B48AD8F18B01C1136488E9ED31A9579CCAD4B4B09DC7C6332A6D1E98380F95C7DA2A8D4C74DA2E5DA1EEA1CE21745389FF1E6544E8FA787ADC3DD559D6E4AF9AACCA95BBC52029349AFC6DD6762F140DBD08D0CECF78567CC91E4DC43CC823AE17595162AD922A7F0689ABABF75DAFB1A6D67EDE17A2EE013DBBEFEE641A471F8717BF6F9176E4C1728EE09F1BD4B6A6419F0137B1566BB67E33CBAFEEC686C6C20015E1B70C9C40325D2C33301818FB6DDE20EB4A1A172091C0A94C4ADD0C10727F358F8AB8C7E28AA47405D96B5D38A63ABD5837BBE985637653D01F15D500037D74DE0E63DC0D271328B281719E3DB66FA933D260D9BF49871D6DAA548B75AC056DCA6BED70F5E0F7CCF93D8CA1AAEF22DA6369628D803E2F39ECDAEAB3AF2E2094778B29111D5C6C7148BF28C10C384697CF30318EAED1492EE4B42C101C3553014CCF5CEAB5264D8CB05F95869A2BAE4B8BBEB75D994C578ABF92C452B6FECE3B172B617B8CBB687B142473D31409E2AB888125751038C74535662AD6F1342540D850C0A4A8C3409CD5555E30C7ED8493440EA992A0FBCA4456C6F1E6B6C2BC0797B171691CCA97458CFA2C2C617499CB992BBCC13758DBF94D3CB4EE87DAB68F6825BB76B154C625BF3D7B3FC3617EB147AA22E3A07281832090F56B268AFDFB8452DCBEB32532F605D9BC7387D7AEF6D288605DB3698A4771B09BC0509683DFD4F7844750E5B8F2E7A2DEB7A14C905FD09750A8825A0D2EFB2C27DD02CF608902729AC743A887C375475C6FCDBB8E633C98ED9405E3C8FEDAE8C8D184D8E8BC6F8A4DC0D9A7DF5F905176DC5D04B4DB0FEF9CC9DE94ED83BB784376FE285E8F9C4B421E580CA167A2B9437F87490962EA0C37D8DCC3C1E8FA2BAA4C9A0C17D5C9D74FA542435C14D7A53A781627A161B817587C4F6DE8C1733ABD2112FB6B532A8379429C655CB6D7283BD83CDCE728DFEFF044518952E00305E9FD757A48854BEC42A1A01F4508CFC8BAB6FD5237F9356A51778DFEB5476ED7DA213CABEE0E7B0ADC768773D55555A566BEF5A8EF0A65FE8C48B936F5EA318C4E013DD0FDE01CD89D9E375995BBBED934D6F577369387B35A9424126B9A223B55ACDEDF83AAD399C592D01851FBDC5255E8481915241038A3040F96D4046C875F1CA4406664075E2CC61043CFA5A2C7B266926652DB2949F354F1A8EC6942F49BAC7DE08F6C68DDFDDCF06C661D5D6BBCC9B7C48BD91B4009C3A683B91B72B4BF741AA21FC7DB195CA2221828B58E6041E3A8B5A10DA5525B404CC80DA0D0EAB6C3CB2622C1913EC6AF8866CDFB2614354E974EB8FAA9149CF1A3C769AE9184C85CFCAE29EEB1862D831C854CC8FAB42113B6009A6FC2FB7AB9E411E44CA9DED558A706E9FA842C74D727BC61BB6EFD00AB63FAC414A63E745F89D20F07B611A3DE104EB615727DB0D8292D00B0D24069021460E2C2AA800D7A7D016807BACA200733223DFC1BA5502B421A69299011D1015249E3EEDC1525FA759763F1F3B165003C7F39F212EED12E0A54E6E49FFE37136DB5721FB4D7FD9695FB148AB31F1D94876A2EF41D8FBF7C985F970FD3D5B508E843896B510929AE036AF0707B70A81D61172E033223DB7F277E8D7675D35DEF9D3C8A73E55517AA9106B7A58AAA9C2EC68934AA3E158DB3D0BCCF1AFC7F4CADEAED9630FAF7A26EB659A768E65990C5ACFE12E0EEC13C941225EC8A2E6222798A6449F4E428D8B70C241D3E090000F1931094A7C2218FCEB8691B52735555430870D33353CD2373EA5D6362C8C41D4F2D1E3B07EBB6DE0E3912BC837AD631D02EDB37459E2F593D5C636C062C89AE380F8DF91A860316CFAC1A2AB7CA302F204F0A5B022826A6D8D6689B3AA6226A8111A89A8A6464CDE5211ED7F178869EC5B0FE533B1E4FD18B185269D240E71A7FD2E7E8A531CACC7882D1CA2084EC85191ECCC583AA7F99477CF15E43B1FA9D1E00D29AEE493B18103D81CA299EFEAF21777A6AD74BBBED77BBB2C0EB4AF5B9EEAF3B38A83716C58AFA6DA4C045B151559369B4896FDEA7ACB93F92B1FFDA37C95E1C6366F4E988F7A22891C6B51CC9DDDA8B4A6ED07EA498D55F77659D191DD3F95F631A45C939F950809783BD53D96585370E8D1BD7D1BF235D817A2D00AF3FAC7EBE1D21A9E5070210571F10CAFD055E235AE10784E5505AAA3D1E0F9E501911BE00CBC99E7E6B899E01BD97FABAE9E6F075B7957EC1B0EABB543D052E0B3D55F580EF37ACB34ABCDE6685EAAE54A466252F0A1A6DE4D2DF8E7B55E7AA23036C4044B220C4FBC2C374FCEB24D5F124D5D1E1E9F28E22AB398197144100706110A1ACDD6ECA135FAE0DF0AC5706A32338C2F92ED782F02CAE58AC2332C07BDC8F6D87B6E4A9A35EE41C165A1AC19A0EC38182FE90D4DA5548D5954E41C3C7DA2D55D04A3135668F8B8539D81D0210C26BEF11AFE761C38C059EB2B474DF52B0D4B48541C4A92B81F39ABE848DED03421D2E2101CCF6F397C5B0E204EEDB773193E78AA98CE49965BE9E2CB7A7E17EAF9B4F4162F5DF60098DF7426CDAF70B9D0E1359D9870E156108616E4BC0AC239B273426D452C0527AE79F3A8A17C030EAC84B13AD79C9949EDAD6174DCDF542406514E2F6D3EF087D626EEC38EB1460EF37B3C571F7F70173AA8CABABF00E7D5B74011E16390897F080278E5337F99130D14F8524FBAA9746E4B38A6A556720BFF18218F61843E572F5EDE325965FA4A1B8C45F9FE18A149405B82AD3A3C0923BBEEAEEB4AAB02856DDA1F61438C5B22C558F2C1C70A43C88BD7A8ABB50E5199DEB9108EBF2D93923215E5BCE51499EB388F8767D2F89442BAB62A99F99FA64D7D6CB4ABB9A9D73A0610D8388EA4402E7AF5688B0382B1452794D337532FAAC4D54336B51AF467EA92BD3B9DFCEE93483A892F68C4CF5B2F4459768D6F7CDF4C38E57C578CDFCDA15B88561A2223C832286A89C628665F7880DF4D6B76262B386B89AD1D46DFB3A6B2A4C5CBC0D039E8A0D3173626A58857936A920C03C638B607D4A95DBE9D1FD96D2A25829F4D11997ED4599DDB773FFEC55EA8029943EC52B25C6563E6201A00589E5FA1522D163D3417B9FA2BB77C563B910468881BD405B1A5A146C06FA5DF7405834027FA706C67273878885F0A1BEC91EE75A2FC4D119C6413136AFB2CDA7FD6EB287FC4788C6B7DE385DEFABAAD739664375B3DF6C10CA89D6351B2DF2022C05FD9D03E3B9E762FC39CF205C8FF5BF901B58666CFFB9AE6D386E3E3DEABA914D0A1EF63CDBE2256A21E3EF8E43196E062DD8D61BC4D3CF58C0C95A643A94248557F3D97C020D0F6E990EE84D5DE6A603FAB6264932E0D1D40DFEF76AF0DF1BE2FAABDEDDDDCD35FE9B83B88CA66440916131AE2736E745BBCBBACD03C552ADDC6C506125372440CA53F54EA1877DB467B85100D0AE3714EFCB6C633E0CA79B4F55FD052F69F7E643F13E6BBA222BCB476A6B64369DC50A9A394D6DE43CD4F4E8110868EBD0085754D65FB282ECB0E617C30DC77CAC36EE0CCC061D1B187745B3351F6B6A4B1F68AC4569DA944565AEC5876CA3B9970A8773BE05502100DEF5048B6C523B62059C957D7E5933B9BA4279B1215B6833897AFD47B6C5FB995DD68CAF7A98C9D569DB16F71591F77394E565FF7EB99980B13B2D1745D2CF183C0873D2FE702A45827AC57504EFB831344D92A9200C75C2ADEDE2AEADDD117F8054B5180D26DDB3F3D11D13622421BCAB0FA3F1DAB0DB35756AB3EC028DAFD9FB337F44B5A2EA1C4F23FAB7EE4C4D61F2841C5D41BF87226FCFD135343386182CEFF7CDE6216BCDD525B6D4F23DBAA035ACD1B68AA9A15993C9A68D10C7D47159983133C229E319D97A72F46BB5CB0A6333FE3D0DABB3E4043B4B233CD7E86EAFD2B6C26E396336762E0BED68D186D20A14BAF546149B76CDBD8552204C978DA90D13AF50F740DC6DC1D838205C919159239D18368C09A72F188447B3D68B3BB4F8CE6FA2E783ADF523AEF5183E2E91A60C1F9647536E33FE4C575E075C13676C2BF27BDFB55D6625E4362B23B770B9F07CB0E5C2719DC6B7BA62B1F5336D3ACC4F73A316535E995B25CCF448E246A45FB40DE0F199B11DDBB85AAF1B5A8BD388FDE2F399FE83C0E35C6F284697B2C85BCD966BAA27B058B3F11AA269845398FFA69BD21B127F738E3232B7339ACE7F18D13925CC9F33862F422706ADAB10088E8A671AA9E5EA0BCEE3671A21E6EAFF5E740F79937DA19C62067B708CE01AB5A3AF976A5BA3BFA6411E4236976967C632D175FFDC8C57028F9E9BF18877B43FD770667CE9E3BE68BBC1173C5FB65850683824B84F4547966622091804D7D673CD945A6EB188E46BE6D55C758AD55F6AFE60E23C1287F8854636867AE77B7453538EDD1746AE2ADEEC7D6170E69F95C202FF422316C4E2E445FE85460EC6A50CEBB5BA615C169AD11FB6DB976DBBA72BE9C202B2A22BB1D08A41132F34E3FDB6FE72D3D59B4F4B05CD303347CC3C53BED78CF554798A16004EEBBFD7064AB1288485E37B8D0C4C79B484A9F1BD460A98045CC3F11855592B0EE2DB9954F51F348C63AA0B1AE507DDD603C8AE4F57D71DB3B3D539BDF083EE986C58A97951F941C330367B8430CA3F68A6D169BE2DAA518D53A6FF3F38E3C0702BC53CAD11622F45215CDDE85EEB44C6FA9CDECCA506BC2316C0AF26605D7DD8465B65C8371F670405B34B378282EDEF32825CA243FFC16310AE376EE28A1378B8180F93F628859B9C2E23C565CA0A13BF36235CD1BBB743C6210697E42AC47D83972973BDD8D6A5AF7B4FB8A81E20F08345B9620C6793DD19FB80802D8EE5AC4A3153C47B9FFEE3C5E35C31E0BE16F7391A837620DE5C178E49BA4F3FB6B47F5033D4FD12C955319A6CA76D5B6F8A7E1CA6537092BD6CCC2FC7B6F8BACA9F0CF7952898E52ED3E37CF777C8A1768587A0C03B984DD13DFEF4F4BF081D80D1CD0900167453B23B16A1C8C66B725187DCA3EE37E7C4222EAA8E017942AE8C15D5A6D865A5BC6DAECA13E8AEE79272F2846FE1646E822F39477D0C55D5C9D9E8DBF6DC04CB9A131D6F5E9E5092602220C326E7D5A3464626B070623263042405C2F8ED37DF041515BE7D93119BD28E78CB0ACF4E2371992AC9A9482435A427B74330A95674385899FC10305B19E251AF22481222124A9384C326142CB50E43A67A29D00E79FF27B41CF57F0009821086161FBA6D936123F0A1048766A65FDB2905A59FD7DA316593BC061214262BAC46D5841614BAEDD41A8666A65FDB0904A577A6AAA564018144A42FB5910F0A5D5AE1101B4E241922030F5F2CF679D1BDADEF6F4F37E4FE96742C1930503C46082B0961B126B756E0F653090BC852231B855458556A86F40C84F8F9244A61F482D0900CCD80364204634FAB71943424922725978DEC98A5DABAC255D79F6ECFF0C6EEBE6E1442454381C28401ACE48841083A6B469222C910D4BE99D364A8E32B3F1043FDDB4F2030575995DDE3355769DCD04090B88CE53612C3A04CAB70A0A613E9198891076FE60CC23DB98BA6B156AB020E3A9C8EE11103A22391C670A68F921293E11CAB04513A124E5BB90095F42490B0F92132A51662A020895A1E4733972A16695A4504B69D481381CC3C0E5534A45E6B503E8FB7526508E0C194918819901F9950065647525A4CC6947E32D6572149D96D428850795D499B1E83534A81F0ACB2A754F1EF2F33B2343E4E1755943802CC0448F2F4BBBDFC70ECF46E3D95ACCC8F041919483C7430E91110AF6620C928496C20C9386DB4D671755795309294B04FA9AD94AC194A265104C056AA16A4803441621A508E84B64D068EC007101E81997E6DA714144DA80E07175C58560ADB91B46FE6890910BA2361AB6FFB090467BA4E72BB5C495178A54168488826401B218291A7DD9A296948B4455332D98406BADEAADB36E6CE94B92B495D4DE50118DFA2B77703489A3237A202BB06D4F4A433A5CC86E2289D4E7D97DE1615521B5730B85608DFF6E9B41C05716926ADDDA526229101A666F8C15B6222F91A934C56218188AD64ADE9084966B6E9587F04F69BD885610D34178241B5C717B6A11D85A7145CBCA3891C438EC940F715820B1CC37D4F32925B745A473C0C1ED18273F1C84731DD5674C7AB99EE4F48CA5DA95A99B160C1F7A1AB6A2C988A548A0AE6ECE1EBA799EE297182893363810D2E4214EAE4919A0A22527B32440E9B50B0D45AD58B31F5A25FAF6769D00D3D03AD922B5BCB0A6E00902E99E006162F900E2337D558279090810CF7A723B5802DD6B691002C96642C115B5AD0AD84503331844DA0C86494097C484913186FBC38CA29497950C4C8864C1040E8E087460C72852283E42BECE1B5941C1B3D1240D2947C0F434B02591BD2100CA9D98A120F97CE1526AB00499C006B237AD28652BBC27484247385E9587F04AEB0E1D9DC298CA84F09823E23FEED6E4E084478A9B049A2B0B472063401ADA40BC111454D4E8B915F60AE1542DAE4AC0F414B7281D3C40C42C0F144CD3A96308A90AD1550A8E2B53715A9044B1D14BF804885C85A7AD68885171B4EE49B1019E8D7702AB1A01324DF4E4949155E2E5525A9E8B06FCE598A91ACB1E40E30136A524A9B6614CC5C6253AD75037BBAECEE6E4839A6B9BBC30182473F0B8CD5C10F8F3AF1251E49F389044AC6D8A356627DA79C34595F33A93A1B5A040D79A938A7516D0C6566A634AE1051C1318363A7E514A4AD22AB8361652E2F8395115F2E87760069042DC16842C8906132D27D85E0A2C770DD938CD4FB4803096340A3ED21D713299082A4D20472F848048971E2319260EE0D6537D0D19DAF2CCF251297C071A125CA580402FA31B4031386A844674F45757FCB088E4C4A2060D9C953FF9883DDC193805A26762A290F78F224A3C76470E97A010E9E645C0F434A423933B80BB7800596AD152FC289CD9B8C5CB07B70224B7D9B4F2A32E34B1EAAD46C226C70E1593027F79A298848E4DB5030D84C94E66A2B3BC9FAD4FD2647912228EC28EBA1EC9C640262ABB3C7706225A7C4CC4511E8E451CEE9106424942A8DDB75010A2C49ABB85AC5A693B95945461EBC8B75D29FD328EB969B794E045DC426AC127D03885FE0058C23C0747EE32A81D62F8EAFDE0424141DB5178A810A2C36ABB89DC0C653799C406E1EBEB38921DB2463160F1C4370D67522A94849EC3A5231FCA81C46D313BFFA14480CA44CB826205B096391A74F8704B66F329081522281CCF56B3FB50019B88338D86842B4A27748424352179184CD47E2279AA937B1AF79E06822B5AAB92DA322ADCD2DE3F53118DEEFB347F2C6F26959D6E321F60592DFCB01A121D112006DE40B6E051030426A1CE152926032AE18DC53B294BCF622219D58DD62AA36A86D957E6D005621520E82C4604EEED7561091C899A460B0090554B555DD4B533730CA7D95BF9307ECF18081A569460B8892046D7869E289301AC8A16E2069E2B96B42C1544749494251D259521C5C60415ACD74921090CE6A92F0F5380DA64902CCCD99B9FBD10DA7A925735515CD80E24849AEB0E4FCF7262581D811DB6F7252CA869F8281440BB09655C2446393F89D008F69B8950E68DFD4EF83AB78CA0CC04AEFC65348499F4B50FB283A0B06CA8A65E60E0E6372231B6E3F917D0DB3D3C8AB74108FA25F63345979DBE7D1900D300503C9CB506C23313442897A81127B049217A075A3F1C2F09EC20230D2AFE5740262B2991741838ACBAA3B79390D89148D9CB9C7B48F1F7A81FFA22FB833A328A8C79D05964BD408672F585C038068C1221B54B26022CCF6D5A44610E98259ED4B443A25A5DBD2B3604195D36AFB79B8FD74DB7998A7C7B09BEF4D378D0AA2608299CA2BE919A0ED64EA0560E31168959EEADF334D4A57062A9890CC08D7D94EF1CDA7DC4DF1BC34697BAAB3AA71431C0717AAD4861340200FCD852477212470617D331796690A2FFCD313F2BCF36B369134E85617A45C5CEC25C27669092B156B2D2E02178F616D99899EF62DFA719DCCF390B232E1948A8C645B154372385A0C4791540926411C8BCD054949488A2B3A5989D45EBC1902BC9C830BAD6EE6CCC8D2FBEF84B61379EF04061EBCEFAEA758B729A6818289C66ADB61A8F5749B61889747B11526D165649C550BC6D0B9206B4F8F0A920D40D6C2AE3474CB462383E103AC2E34EFFC9A4D250D1AAD41C1849289B55406D0783A8D01F0F11814C62F75B7A4BE525E0B16202171A1816CE446449EF696B0B4FD44FE122973FDDA4F124AD5B65FEA26BF462DEAAED1BFF6A8ED6EDF6455AECE83A1AE0607558935ECE2AA942DAE10526C424F22F9331B0E1362E64A8727954AF526AD914C166DD45E64315C41036AF97F5C9AF043FD099134A6836818C90153452B753DB4B3CCB16D8181A57A098F2484206D660E9CBE4A484104C7C49B967584D15CFD2DF049C470057B4F43C51A3A4F64FAE16BBCBAED7675854850F5344774F1CD8A3AA0B089E056D2A668CD3C1E3A94D0E9893119F230C1D106E3E04DCC4A22A8BD1024AF934804D7BB34A427269DCBC4601C8EC1850275439D02495A2391F8AD9226494B88C958874899A4E5BE27212944AEA9EF8A12FDBACBB30E59385D94D540E1036A58499FBAC5F4BB5D237A52997F46C371444E17A83FEA5D87AC4632595C63EFA1A3634DF13BAAFDC735DAD54D77BDAF6E7F46156A3417E6406838767C04B40B1F87B0A7952C250D89A44AC965131AA86A2B0B17794748BB9F65C16071221076B2C4E04CBE5185DB4FB73785797A0CDBD191F237458EDB51EA22062EA8D8CC4857B85B0712904CF3803C35B29FC63A8790F29D7ED95819EE05822B12763BBCE92469237D3098928E741E0B35CF8FC15901F4C0EC7901B3B7CE838ADA8AEF9D6BA9311DEB90EF0E1CFD9BE77C3F34799F61F0B862B7D6DBE76A3A8CC63748EA6735CFBDE9482164FB1D1E1CBC8E579FEB62836EDF35B9EAB10B081A143116D04AC6C03640111B00079223099A8A1893F1ED2BF84A998AEB9E44AC2062062FA9CBAB4416B635DF50D7D392686BA0E7BE092187F27E3AD799E9B7B1EC4DBF234BDEDC8C42D32552723C2966EBD88823ACE8F1CCF7272591C8D54D7785DA36BB47BA64F820B44CD816405B59139B304E891F56C549093119DB2079F1951CF7A522BD7CE94F326515A24AD98AE7963A4A12AEA32AB61BF9DA0EE2AC92EB89FA413C1136AA9CAD1308AE20621DE93AAAA3C89BC7B643DB1BD475E4C4623853552A30181E942B1AD44AAC246DA4172D3521A9C44BCD72232AA64AABCADA87628BDA0784BAD755D73C6A4F28617048D258481B5193B4B146CA61352926A31CE61053CD766F3A52CA998983030256CA989378ADEACE5051914887A9B87C442E8CA51BEA40590E2E823CAD120A2B69DE64004304C04AB8EAD97CFA856F197AC36569E978CCC58F622F205632718DB3FA09B4180DF25429EC0A28703F002D29848E883C5E866FF0B684DCCED79A5B303C28722CA895CC495A491E2BA62124A1BDA5E6FB51185CFDBBB95DA65D165938A96811102373488E3AF5CA08379F6C6584197B682B23D6A798E9B84E876BA08609F028AA4FE71F4911FA030FDB66DF76F536ABAABAEBB1FC88CDBEB3B221A3D9FEF4B46BF662A628821A6F9247A4A7F9B6A8DAA74F86EF9414F4058020B1D5899509D51E0C754DE5C1298DA0FA935BDFA07932B23212489916890C8151E5D37D5E746FEB7B9085639916C9AB6CF369BF9360990BF56886747E05D89B25D79F011619028321EDD03D56D39241ED0BF5245C6555760F0BD658A445311DA9413896C3492D923ED405C631460B69509CA3B2F88C1A326F419ED0E5A6C860EE4E988C59D3C758A8F83386A9D8A093755300321243BC56CB24B1377834287A957DFA392BCAEC6351E222001700638255211974B99E7973D01EC8352A0CD2842A293966D57FC3A23525A992A2A2810CBA97DDDD811DC3DFAFD0F6A3897C0D116B32FEA04ACF65FAFD6799402D6F6CEB26219695C72BD435C5069C864BB116D50502274B9F934F5355782F0F4204BC53688656814CDF29890AB83099F5438E5E703546435E6C230463925F399E3959B24EF832D83618F25F6A2AD349DF20246C323DEDC888097BE0618212FBD820EF139568518F89617488C5BBD82066E80EBC0E3570BD11C40D5E2BD58AD178CB0D16A1F9FEA0160D71C7C23886DB3F66CA4F658870A1D186ABF518AEA45AABE710310394CBB1AE0C237D72AE43481FE481F8D81353DDDA46BBA960DB89F70D9A6254E2325B74178F8674C9A53D4846C609DE0BCA2D93DE67C0A1A1F6A1ECE66A0A057B424150FB2B30528CF1375050B31B63267ADE3F0A7B6A18C1E4ACA0102C7B48AE4B276C9F8CFB3BBFCB29ED32FC722740B4F076A743C785D73A75381C3B4EDABCC5A5C57DA5EA3D0FA6269F8396F161DC806B78C1234BC190C105A060450F604077FFC7BFFBFD1FA0E3200A9F8E0FFE1B45C7C5802A98622698CABDE34CC8548411EFBD5DD25E53A5725A1720A8BF939F4DD1590A41CC9E8E4EA9DBD34DBF31047ACB4228086600C15E2FDE3155C7593C113B3FFBD2A6948F325D0703CABB00C2431CA15D7D0A96C0F86272066F816F17071DC011064041390D077260F02FAA3ACFA00017FDC5CBE8DDF3D1A3289DFC4CB99C681A0CEAF6E2D354F49C41127DB427DB62F6A9CA469D07D40D1D07EF23053C2A802932CE3AF065BEBC21130616404E3C0307F59FF24F2B78C0A2892D10EF1B7260D5A07C71904B244284D48CA350C14326445C0063A4FC7565CEBCC196B004BCEC2C12CF5F72B6EF3E7F8599E9F4E42608D3E7391C56A72104404D1F78780F7608A8E26A88E918ED76380E83F9B100A8899FE164FD9F4EF3344C58F0009D07F9E8D373B94B800731A05AEE1AB0ED7F121FC174C8767B5EB4BBACDB3CC80C481850DE0D101E620B7D06A8600B8C2FE20AC29CF019D9169A1AFA5511AEA85A6DA7234F832557823C8DFD311F934AD58C04D2B05F73052DB3C6035D53862D88236A22B145B94A92C2DA7448AEA4BC7995446B89CD8E47FB26FC02F26B68FAC4E6D808C92D36B386F104F756672A83580269AA615406B1B3DA4A6519CFAB8C549E380883054B2A3DB62B5F3A61999BBC461B34BE42AFE2050566D08F053A0457286C11AD013AACE976894C92F38405D4F7838157F0E551AF6660940073E4ACF6E5101516A561D10269D8A145798663D2825337BB40C43E9B0F365E4EB10561000DB60F4C07036C47187C72618259EFC02121904D61144961E5FD92558158A58DA933C31CD726A2C3F4E834893276E9322A8A7D526454E4992573DC182185E6201D56189855723F180867DA1FB95FCC954349FC64C33D0299BB982AD5502D75144F819DBA6E4777110BD19FF4AD6159DF65F09ACE48AA49F9C345AEEA7825431FD33DB244BCCA0F17781885B9CF8182BB8721F856B569E0B1AC243E23A576323454721BE9BE6E64691ADA009533302E41383AC67A9BF011B880A6E9197B152D24CFD89B685ADDE7BB82A9B9C44219AE356ADED8AF5D8938C2585DECBD0513A391A96163E0B1EB74780392E5A5848B810FCBC855549664787322C0A9F7123CB86C6B3244CA6A36260232196F941762DCF9A33E39A220F4DD509F1B197323C9566369ACDAA0B294C61C0160263D98A1037063C115D5161AD3EA2B775D0094CA92E181618B68BA4AA4348A0454D17758539B72CB902AD7932EB7082DFA9F22E4A4BFBF3F5DE292CF08F0A90C5082F957325C6702FF128601133D5820B54858003DD9523BC4AAF3F1AD0FA6254D648D0067D8014DA48D1D37121B13D365446594090BA4EE08032B6309754552C317165DE4A093B931B5F9C08319F6406D4838B024994931B7A8D1A0029C614734BAD4813569B4AA70EBF5B6BF502B32070694F7058487D8035DCE55F008C60B3069B82F1C8A41B7EF9B7A83DA56668F4260DA4ED0D00AD6183184C115D112999ABB4677FB2A7F077AA204183DF513A8371B6644000F64883CD8A050273C889E768512B1E2C15AAA63BEF56EA23E26609BA93E7730821A9970C7951BA2ACE6941C229BE86239F11414C40A48F349EB4BEC11C8E273E96E1FB6A5BA26CA4128886600C17E6B0FE6391C11D5E49039E176482224F69A2E96934B4141FD9DF33F287A4CA3900C357C50EFDE67CD4A094069C9D7AC93168C48B44A3289336EA7741C32667070BA4EB0E072862C893DB47CE15042B766611EBB8B8962E5E420B483AA58372D4423CDAAD9AB20B940D0C51A3D261F7E43459862B0FB86F09E461AE7C70268289EE03C3A3DA38838FBC9FA7A2189B99ACBD4CBF38524B2CA686DBF908451812C72EC9F428491890423A5009BF43289F8CEEDCC4990541D9E804C089F54AE5FF7272C522EC8160117C77B5622A945B3142ABCE5130CE86FCF74090E96EAB12D99BE25C502C5946B08562C4E865D4EB43011F74C06A7F098CBD4223950EB28CF7D65A88B10935CFB271F52BA5843A87C40CD3A9A6438E97C69D2A33211484EBA000B3180CBE2A6E084882EAAD34ACCF3463DAC06FA2B9435548E055545D8730126AB533A2F946DA466A44CBAE4C0965D93495B20CE2595BE3E15E0ED9C5A4FC33516DAB05F4C252DDFA60C86A65C63B183DE32835109C34623C9A340ADBA6824756EDC8B2E71628A4895D75105AEE897BC16C83630BBA58A6F0AFC71BD9650CB2A47BF02DCAE7BAA038010EC4B743000B42C8DDA9003DB754D1ACD118471F1A33CA0CCAB1A13455D43D1416545908770FA581513D56DC4D47D50CBD285420A6CD935E962118673B1178C394BEFEDCFA8428DFCB8060654F9560178D851BB241156FA6A217C513943EE28A8564F0E42453B0D0833614C81ACE4008325EE4A3836F6A6C8F1E220930816444BF904E9C980194DFCE05BFA1EA1CC2304432A3C3B60054520AAD1C50809D6B8EB3DD0A83652D9EA86A8B44E0476258E6236B8292A8134EF953CB6D38757496E8C72E9D5F5A9585840456F2078555A912507BC416211166BCA142C53CBEAEBA60A68F3CEA9AF9A7AF12DD13553AE5593C43502AC79A74C92D738712B55021BEE497A751E291150DD21015EC626E6F5040D9744A4F17347516D2AB76F5258E32E29B76C1EAC4AB24DE3DA54E590E5C18C3BA2CA27EBC89BD81B31E6B58E255F27C81A09A8A22F700D9045DCB3222A0E49B0C6BC1F0F3E270F5EC48521E5BD51BF54CFB87E84A75254F76E61B471776FE09BF52A2619669D50BD6E0F31C8903589967DFEBD752543B437DB258FB2FBB021C18576D9DBE1FA192479675C25ECE24BE3816691F8B8B811DB5D330000EF5ECBAEFEEB9EC8667BA579249B9700E61524DD5D7FC5B3D891940EF76EB3347784E46967B10BC0E3CE3C4BC6579CB42923C4B79C7DA6D7CB93A1F6FCF8F05CF6F2E466F380B6D9F8E1E50906D9A05DB7CFCAAB3A47653B155C65BB1D61FC5273FCF2E466976D30DD67FFF5E6E9933FB665D5FEF4F4A1EB763F9E9CB43DEAF69B6DB169EAB6BEEBBED9D4DB932CAF4F9E7FFBED3F4E9E3D3BD90E384E368CD5FD92A3766E09771BDB365C296E1A537A51346D779E75D9C7ACC57C3FCBB70218F0D432CBC099C55383D43332D0B680409397B82670F26FFA0550DCD43784A4F1D9650EC1C2C20BDC2B7288D3771051032DA9876BDE6CB2326BA607AD6942C96BD66775B9DF56DC475EE6E478C802C6A399BE89585E9E70DDE0197522708A935B9EEF46A332ACB21E830299E8066302570BC34A1516F22F11CFF0D51CD3EB6D56942C9AF193398E7EAA89E4509FCD71BDCD2054CB57734C3FA3AA77B9D178A66FE658CE5059FEB21F9EB0A531D1DF2DB0F53B767EFC97AFE698A6D88C3759FBC062634BEC31DE646507631C4ACC31120A2E3B72D635ACF8344EBECC1CEB657BBAE98ACF9C842C5F2DE416CB39CADFD6F7457556EF2BAED762A98514D79B4FF51E1B7BDC48D3DFCDB15DEDDBEEEC21ABEED134122C56A8DC6EC6F5BD3CEDC449371758C8F8704B8DC7467DB6D073C39E9CC7457DB6A6EBD523A48285C28359D666279FC7CA06FB310DD63659C5F8FA4D5C036CF5BFBD9E58D17021A6BEB7F14290381A3070D5B8460C6993C7327D33C7723ABE15CAEB08FABB3D36584B88A5072344DE02E4283C76821366C8FD75C3396A374DB123B6078B88293898C15D1ED8F4D9738E485CB69DD2AAD279446A08D367FA68311FC95BA1E054A40B2CF1F5322B621B3E5BE112E467FA66B1019B4790DB8551DF6DB1F1CC5ABE5A6C071AF4B9A8F72DF13C71A471451613177D01D02D5F6D267087CDF3969FBCE3470B2361779AE70D6A394CD4E7B886EF4A0A857A9FD643A3CC581C548AA2AE8CBF43155EB697AF360E8B126F953A6EE3BC7C35C7440477CF09CFF4CD0A4B03480EF5D9420AEBEDAE44901CD20516B415FF895E3D7688EFE4F2D9C2E25B5E3986743A506CB1491ECEB9B99DF1F4F17066DE72DBD767E68DEF8FB84C3C6955B944ED841565FA663E3EA45D61EE8EDFD670A0BDCA9A4D9D235E998C1F6DE65B85AD1AC110A03EA7D626A79B7FED8B06D81151DF6D56DAE5051B9EEF7C998525500B1A65FC64B37FFCF21B6A5A81F5F4F7839AF7DE53DE71BA5B2CB14126E965FB911B92E18B39860F45C79BE8E3278B69B0EF1EFA5351760B327EB430A9A1E9FDDA7E72BFDF7F2C8BF6813F48A03EDBE3CAFF0365127C63918D6AEDD07DDD88AA9EFA6EA1CA86506A1E19F539FE6E5DAEB2B1D2BADC0AD602FDDD125B7F7E5C75E2364E2CB5E061860DD2A6D87028A9CFE6B87E47E853F9386456E913A5D21885429BD3830EE12AEF51739E3DF2E7074C918D9958F7AFCA0D3731580B912EB1D1487DE73EF24A85FE6E83EDA26E868C232CB2F9F31AA75978577E871ABCBECF51D6DCDC03011CCE4FC69790A5472854B9F5498F143B547E3C6752AB9D1DF5AADB73B3312D004EE747B2AA6956A1F53CC5477F2A35BF77EE21399247DE0D04475A53BAFD87D5869B2E0A72BCF57ABB2BEB4784A0E811BEEC60467DB975E331ECB22B4606E32EAF2A3734E1D5CE6D910B33F4C4D596558FA2F6610AAC7C0C5DB6E9B025D5027E06BAC81CE76F59074926F5D9C2F478A82BFE8C60F864B15D83FCF00A2FFC6A1364BC22EA353FC08BB046D3435233C536CC7F313D9AC58F7DB6D963A855EF531B8CB7BA7A5A97DD545386D10EDBFC6E388F8E2930C7F7CF7DD6B38D45B67CB5C774BA2171EF2887312EA58726B49ED6FE349C1ED26A2FA96164EA9AEC6F51C5FB4BA8CF96F209CAA6A51F2C884BFFBC687759B77910B79D6C890B46F82C0E2AB719890D22D9AA796AE9EFF6D864A7867CA9DD3184780A7140739A4BB710C02807F34B5858E692FAB1E751C8B91D76CB10D72F16467FF40C17A723F5D9625FFBC70E9B8E283F170279D8129B03CB4F55FDA544F93D7468C996C539623C84D9ED6D6B0AD87C67B99DD5091973F6765C287D7168E10693DD384E39D8A89C0BEDF18E16930C33556CE10CA98AEEAC6E7947F7FCF560E6D17C09D8F37C1DB8ED6C78C40ED654C926AE0089E7F879A575714894252C8BCB678B137C722599C7347FB4C423F162F065162B0EF5F2B91059CC95ADA12DC68BEFE212CB1458061892BC024080E1F0D926C4381731CD1F2D8230F95368CB93E750A197BD7083D1837C9939D6377599636BA86850CB1B345C918593352B04EB68FA66399FAE51BBABAB5CB4B6C4521BFFD0A6C4CBFD35CA042735576433A3C6A7EA85935BBAC0019FE416A0586C8B7BE8E37CEE09A0E721DC5A90635E335838E439FB5185F7F513875E397C4C112D3203A3C40047DAC531CE421E6EC9A57B26AEBB62E9712FBE972DB136F9DDC2F2F5AF29BF14ABA67C88E3401A91EB4CB73E188C331D4359FD218F2D97ECC7D0C66629F92A2F7AD1A99F7D5C4BD264D7263E2545E5748346FEFA9D5EDFD44D37A67166085A3E1FCCB08F09C17C9592AB36B2544301144698B89D7FEEB3727EDB4D70A4D14576EEF2A620AE181290CFED5785429B2DE6BE291FAF054B85FE6EE12E2D6ACE19453E584413E16E4CDC8136E650B92D7651E3D2DFBF4AF5DDCF049A77FE739AC6E63ABFD5384C64849FAB7CD92A26C7B6E8A05BB87481953BA4102DD9E5AB8D8B774A2E0B1F7FF3A52E98F19CBCBB93A39E8BED718B2100CB778BFD0A71C461193C7B401B71DBC2173A6A7C10390CF1751D4392D1F5330F31822BD4878DBAD8878ADA8A4DB528B00E627A08B1DE32ACFF567F042E422E5F0F4880C627793CB718487C78C8707F01D64C71CE15467EC6A621F1E18AD6DCF75E649B7DC98758CE1FBF4A038CF8C37ECFF0742B5A3FF1A611399E31CBABCB183DD5E00580FE9E3AA422E4E40BE7FD0D770D33A43F74786A5C44467F3F98A9729E15E5E315EA9A62E31780BCE071894156D596F1798017BDFEF4772BCB17557B212063FC6815DAB1AFF296C7337EB439387E24AC6A811CB75C919D2E201137A35883812C4BA175E0433B9F3F82E10F74B10D3FC9FDF676CCAAC4B3952DB3C67A8DBA7D5349D02E851681709F5193EFD188818B87E3CAECC6ADBDA94B60C4C6CF16E6000958BC6CDB3DDF6BA6C042DB913C70D3DB8E8CBAA30BACF04D0F2370D8E0F71254B8CEB21D1E42280E61F97E309AF802F9059292401C7BCD0BD692CA0E126245C74F2B85AB09893B6C937584BE287EBA15D5F5F42D759054489B863CB70504F4509FAD57A140819444998A2EB0E9AB2D26FC177D9121A40A2D389789C973A76F765A5B987AD230ED9534D868979C96651DC0EF2D6073D06E0638A43379AE2344D33025D6169D90E065F96C1586E9AD89ED74D5BA32154292DCE5C75C6A428EF1C106897BAFBA7887F65073A44CDF52AF926156ECB3ACC95F3519FF7208F5D90E1779C5E385886BFC6C8EEB67BCD47FC91E25A22496DAB8158B12DBD0505C2B5764D173725053EFBB0FF527C4C7CAB245B6FBF1777792990914DB9D128987446BD968A1C382F1CF0DDEB9CB8E3081E28359352E7C6FE55DB85DC483AB8531EAE29BC0FE8A3D8C323DCF1EDBB7825B6FF99A7A8980749C83720B39D53D5F45521C3D486FECF365B658F98E2F5F2D3135D078B02507A38826679F872A1A50B83CF222A998622F7D98966B989CE8842350844C651F6DB447E2F1C5FCD186A2C1532DD2B47C4FAD347D2F0A8ECE16E0F8832DB189BD0C7FBD6FE0B0249BBC5098D6F64AB1101DD5DD0FC68B1740278F989C55B3B4BE664E002ADADE2F19D4730A870C56F61183D32345A066140AAD4E954094F4F7F0BA6DADC0AECCF355B93E1DB6BD54C3D5E23AC6BF6E5303C8236F9D433ECC721E63F13C04E7C74A537488050A704A42237298B2EAEA0ABF97F44E005F963A281388BD9506DECAFDD376AF52C573A89081FEB5E1DE53983FDA44CAE1ADB1706F78F8B6CE843D47F97EF7EF888B4B5DBE1ECC549D5EE7BE462DEAC6C7CE3C8FA344844E67532668E4EB665F41B4FFE6CFA927EE701FE4AEE043A9E9EFE9A7F0E18562BCC9AA5C9A0B4528B4C62BACEECB671BAB8C24A369917840C296D8601C047307CBEBEE5015467F5E154E5D0CC75F9ECA428244BA8A1170E1AEDEF431F9FA4E1A7E93B50F003DC3E775A6A5C43876328C315704A2A66FF653C646C5AF3565EAB6C36A019116832CB1223E9749638245BA75A32A0BEE3BAE6C9DD8B9434C1CF8AE29EE8B2A2B454F0D5B628FB10F1287518E45F6385FF3F11E4C8195B77757B750725EB6C41E23D06BAEC81EA7D06BA6C0F658154EBAE69E6E6DBA7927581E4CC1311B8054BA3FC8C2028A6D0D3720C7A026BDE05AEB4653DF15251A5E710BB27000085D560E2334C7B337BB285099937FF2C12AF3678BA9B06F1ACCB3DFB292BF6DC596581B3A280770F265A9A33E0E4F7D7CDDFBC7D58E167775D35DEFFDB67A3316A72345695D3997C72AA2A6A10A6C468D54137C7AD4679B60F906FFBF43FC3D30FABB85FEAA9B6DC609E6F4CDEAB01B0A4798BF5A84E5A20A35F2082EA0D801373F139982039A3924B38CE7B421289CE60C5C512EE0041E4EB8B3DE3EEE3AEB701D8EA6F19BD5015A7FD5813F3C1B3EAEB3545DB66F8A3CE743C197AF168B535F035AD2D9125B8CC26A377FB5C524598FB9B28399B6E36E2AC8C38A0C2AF75432F6CF2C32F5247E10E7ECA5A15C2B7F65550D25B2D34351D5E7BAD88479B567C4E5F1668F148354290E15F801A43EDBC856C837B542BD02347605CAABC415A5BE9930F9F72EF0E65EDC0E8BA53633138B470EE3E5CB2C76FFBBB2CE006F0EFD3DFD6D8AD88911C3242F0CF372C751BC5588B767E3D1B8B7525C5039EA441502CD613F2F4BD4E7D40E337112DB4EDDD7DBACE0E27EC64F168B73B8A78D46DB0A3241ECF63DF923BFE5C92DFDFB7F39D4FE040EB59BC7B6435B925FA4F07C228EC1E4A294D4F5A5723A5410BC61F477F3F1033CDBD60EEDD0B96E86D30DC0B6C8EDE7D058099E4342E1C1482979C0A57D40A8C3E5BE4F7833B89C1E0FD22090AE34049CE7F9FCD126F2766C5F8803A00BD6F191FD5E379FC4D3F5E5AB4D20FC9EF7498F9FE2CFC4B5A53C8C7CFB88B645D45A60690C15E1F23B429F80A80CEA736A5BE843DD652520D6F4772B4B316846FE63D92E0EB520FB6AF9FE55BAD5FA39807F4E593EBDDFBA5870B9BE74A1C220B7D7FA0A82276CF9BC52945EE047654FF114ACF24CB831477FB75C0D25AF58F36596BA16788281FEFEF5CE25A2C1033C4D87B138BF4B07D68DBD32C6CC223C579EDE8A95E05E8AD79E037DCDF7FB8F65818D15082B5566B1CFEA0A5CA9BF9CF71E351BE1C0172AB7D02DB897D93D820EA4B922DB75154C0341155884473475DBBECE9AAAF72BB001126C910D8D770DB1290153822A48AC65A6C2B3BAEAB2A2420D0F32B73E7E997FB7D307A21CF0A05DD5392ADBA5DECDE6016DB3BEEFED2EDB105D8A212E8AA6ED88FEF998B5680079FA0433EA7391A36672A38C2E997F956765D10BD704709555C51D6A87BB293F3D7DFEEDB3E74F9F9C9645D692D959DE3D7DF2C7B6ACDA1F37FBB6ABB7595561E39448E94F4F1FBA6EF7E3C949DBB7D87EB32D367818EBBBEE9B4DBD3DC9F2FA04E3FAEEE4D9B313946F4FF8EA235A232CDFFE63C2D2B63993B58152CED34CC8B705677CBCFC77F4C88FEF2447584EE88AA2DE7C79C2D77EC9CFBDA922A1E2A7A705616EAFDCE7B09FF7195EE29B6A7979E9E9935FF665997D2C31FC5D56B6C2CACFB731D9D854133C437FBCAC72F4C74F4FFFE793CBF6D7AAF8D71EFDF8E443B3474FFE97A6355AD695AC2544B87016DE20E8190B743A065F87F381A191EA73D66C1EB2C68EBD57D91F6F5175DF3DFCF4F4EFDF5AD3301E2E0422E0D9B7F614F40A0466835FD748D6CC1878B104F40F8852926189E10C1B1BD359B682B6677FB7C73C1FF2184D5652E9C72797FFD7ED54EF6F4FFA83FB1F9F7CAB9DB962E3D36DC3E13EDE4000EED7C7A2CAC8650EAA67DFBF70467E93959D1AF977CFAD91138A2F3B32AFFBADACC7D02EAFD00C383E16F63848225594BFADEF8B6AB473DCE9795BF7F953FB3B3A0316E254EF8A2D7A4E56E1F185C39F9E32D3A06BC423061EF11516AAB387ACBA47D3C8F87499CCD5BEC3A75D603AA9FD9E315E33EDBD1C78C42078716D59CE64B6BA6C428B9418AFC483AE70598B6547D0FAD518D46BA1D7E3B5D7627BDD61653D5DD7241D8ABC3BFF2B9C79058292F603596206F3402DFE46E331116CD7F050CBABE1D3B62DEEAB184A6BC2ECAA5DF8FA11D44B2FA60E92E82A5EC0281FB462F9CE7ED499F34585C1F9FC5BCD6A653C88A7FBBC200BBAD35E98D475DA0B4F1527F5791F6534B192AE1BC7E9B354359F39120A0675AE184D074121EB0F2423265D1B47FC76C01160673ACB941F3D039E99DDCF38CA5EB852B68C7E10BA9EFBD33525CA247E7CB568E07DBAB55DDA3FEC1603F139EAF086A755E2FDBB3DDACBDD699E37A855237EF1F7901B0A9BF11FF19061B758D28D35F0AB6CF369BF7354C14365171DBCD48CB89E9298788CE5413DB20EBEA8298AC27DBF3D3E4A19DE7263125A06DD70DE14FF895E3DF6A7ABECC2698766BE72EF6A5B0A08BC6CE839E99B8F56319F6D75FD89E47975DA1583F9610DF6C463BD88136D7A31DA6E20875A5EA3B7AA1FF31516953A5FD562A7B2C77BE9224F6D76BAF9D7BE6862A8B3F1709C842F38ECB1D9DA3EA6F41856E2B32982B6944B1AFC0173537FF93C7D914C52A2E0F77D7C43200300CF42A7B51F7C25DE60E5177545687574D97EB4DA9C2867E5737BA11D330A9B0B8BD924DB770FFD0BC781F1BE663588C11ED144B6A7F813CDD195FDA499035BFE0365B2D33523FB7CCC1EE6B2784C35BD960FEA8AB65DFB73451F8566EAEB71D9A99DD558895D6E29B36A3E81FB3FB6D91FFFA7133E12A882F501BCD197BA108C0CDB0C6F1386BCF9E3E28517AC6D56324B17093821D13DB8D8FEDC90846F978FC3EB19FD631091DA210FA061FCEF51739E3D466BE51AD544ECFA74007E67A03D473E2EBAD2E548F0B2BDA89BFE6D092F24FEC7B198BD77A8C156107D3DDE6E6603287CE6F87CC67795557832FA9C12CE187CE8996F6A39D3236208C09F433DF3B538691D13613A9DB52ECBA0FD692BB8841EF4B1C80FF18E45BE73703EC63BCA1D67888B4C50D3D35624E0997D4C91868081BCDD95F52342B250AC581B0BE3A19E560B97B196E772D10FB664A13BEED126FBEBAC7A84950E6D183B6C0CFB60EB4D87EDB3566F765B63FF2DEB0CA2059FDBEFB81EEA4AB79DB5DFCD1A1CBA3CD7D9F216330448FE653A412499B44CE607B4C33BE8E5D145EE222E62B403CF65F078F7A1ED082ADC8FA187716ACADDCBE9E98FE8ABBAF959A9AA5E24FC739F8DCC73DF554E384E371BB4EBFDA4862E226BA1F411481F618C2C88D4E388EBB950E7BC700E82E8EB97F33E0E392FDA5DD66D1EA21C88CCB85DCF4F450C7ED18F6883A6A7DA839E374F88DDCF89D9FABEC73F7E7B3F6B63BE27D645C548522AEAF50B34E7BE4AE5E2EEA0D3FBE5CCF601091D7389745EDF780C85F7FA8F1DB6A1A7376E16DCF6C1909FAAFA4B89F2FB08BA4A7F3C1C2E86845110AE86B1AB419CC0105E71E9FF73C6B14C16F338854318DF544E03776458E9776775BBCC55BF832CAB880821999F79500498C3C52C2E62ACFAD5AFBF4BA273CBE577AAE825EF73F211BBC6C76AFE4D3B1FFCB3B5FDAE0E7DCE0A0C5F944C84BAF1FD04A6F6F16A3E2649126C5A9847F392CC711396AEFFB79FDD53E58131C68C3C081111DDCFED20F1B822F6377599538FAD0635FEA6B4DF419152EFF4E5110CD673B429B129373D6C1236AE71CE9314FED2F384D9F9DA338FC02B7EA047C63F46E91191B6A00B3E24D1AE05440BA23888E8D87E1AD2CB9DD34D456EB1B5350D158B7568FBF0600C9455ED029AE17F12E3E0B2251B9065C3E61201F72750051E07C9FCD4B2D5028AA919450B58CEBD5FFFC94CFFE522EDDFFC6332DC14928CA2E72128621EC40A40D47701880A34FFCC3D9133139C022B146F8A19C456C806E0A0C32B1CEEB7DFD44D379C05196FAEEC549AB32E7356625F55BCD83FF75959DC159BCCE42A85D3E9475310871DB9EEE2B5C1266961CBC76BDA9C09ED057855D4C12F93E0857F66AFBF9761C076B8DB20BBA94BF3C6651AD3F55D66335FFFC04C92404E5BE66982C0D11C635D1F91161F39B08D9360EBFB7844165C78AADEDDF910332208414DF0C93E49F1D903DA78EE9998E52300BEA427D16488AE501F72EC64008E22626FFB89B275DC46C45A570C444AFEADFEA8BFBEFC7D3809EA9DFEAEDB0704BD966CB67710CF1F8F5B82C63EAD2F40CE3BE3F17C77AAEBA3F82FB2CDBE5CC288435D3D4F6DE3118FDCEF199E8D45EB3445A6BA2E7384AE1B71923844DCFCFACF5B9A31611D4D4E9109328A42389A9CFCE03282423899425C958EE628C6D64F88ED9DF95580AC281FAF50D7141BA78B967D4DE88D26839B96545D8FD3096C23A36A1FCF25803BBBAFF2361AFEF7D923791AA5F5CE803EC65FB5A36C7A8532886F11F96CEF48D688764C181700D335EAF64DE587EADD67D4E47B3462F4657B7B53975EE45C14156A2FDB76BFF42AB8A0F5792DFBD5C1ABBF184DEF93F0427296EDF01826B44448E890837EC3D55C6C8FB15A44B363ED08C4C725898DD30523B744CD6667DF5B5A931E60A05834E381BCBD1D2368098EEC350C2CEF2BFA39C1888E76F1C40DF5FCDBC67FD1177712E6EA3E949004410EB33D53E5E837DA893ADDA7BDB0CC5768ACC7476BE9B42C6BF75383A5B6533013533BA28E1FFBEA908569AAE8A5E3C545CC64E091F79B1021D5B7AD5CB94813354CB6A2048FF0E15F997048F47AE4060BDEAC3ED47E7B2AFF24B5710D9BB3ACC95F35D9F28056A0EC2B042F79F6EA8512EF0B5BB43FE3C9F1257B5408B7574E45F23619DEA1440A6626C764F57E780D7442BEEFE75BD1CFF23B920CC9DA56218E8A7777CECB868020EA857317B6C5329CE30597E39F1BD4B6EE87DB02820846D585E31DDC0BC73BB8804D197ECD8BB36948B2D84456F4E7D963FB9672BBAEB39A45D2ACD154C44AAF1582075B1E4945D8DAFE7404D7983DDA26847498BFC4D64F7AA7B7D8663563AB03410575F896FFF15D96065E0F3199AF7D2DCFBC42559C90BB3D0A7068361CA1847F63C77B5D8879C977F4C4791FB5C5BC8C3B0C8DEE5D9443B73D13AECC0771B78AF1F43AAE24938FD9612901DCD35F9545DD773186F04C4F098650AABFA02F21D0845380E6518C99DB53B4D3A18475FCA27898F197E9F315993E711F23F1B73162AEDF07EFA432D60A432C9AFB311B5DDF454BF0F50F25D4799E293481FD3BECBF5678F097A9F3CCC577A48D5D7FE67007D321294D124FD7DBA2FAF46B532AE9727A95E21A8F0313E01960389FAF77A3198AA0C9F73B3C83FC42C42DCE47DBF64BDDE4D7A845DDF880A99B99DB57753373E7AA07A709FCB74797D4B98FF988069BFC0E93EC8003A1DE6455EE916389ABEE33AC23AA085E029258AB45318ED9A689B6531FD86A1E1477532DC389A88362E92B3A5DE89F2A1E9952315B9F49E7DE64EDFC82F8FC8EA17BEC87436441AC79EE62C39B0E588C390B2D60E60F86C709A8ABDB0E6B12444EFC3DD6751A8D532414573FE2648C17CD7A0C3E8115D310BC6B8A7BAC7B4A6F0FD984A8BF5232610A90426CC2FB7A89810A8095FCAC5B75FE772B44A1BB3DE10DDB6DA3AC9256378323983A076CC352F9536399796172B39A2F354D7D579468785BF4AF3D640473EFA240654EFE69110C6926E5FBA6C1CCF82D2BF711E4B01F13945B630F1567F4D7E6F7CFBAF9B538DFDDD54D77BD77DA9BCE95DD54165539EA81196947E75174D01DEFB306FF1F53A7CEEAE330092FEA669B754AACCFEC85ABFEE21D1F320F8BEB241410782D0B33B664D76D87C4556E7385D4749B2853CD03DA3C7A67ABB8C608ABFBC52EAF1E9D169B7ADB5F6FB238FF59F5ACF3B27D53E4F97237C22547C780214E98F1803BF8C236A0755EB999DA117C45E346CCE72D62068347EAAB3469A49DB3E9B0541E462A69094D6B279396907594E9A4A7C715ABCF3509807198206355979941558D3925DC9E9E2403CD72E776C8F416746EACF8F2DED82BFBF475A6FC01E7297D1E777099322677EA455122AD53C2656BDFCB766E84DEE5DCFCD75D5967B6FEB164B7A70E2F67EE71BC4B95F82564BCA79EE22F1C9683B1AACB7240553D3257A7D128C698F0AFB759A18E4573C11AEB21202337E568AA878E277C55E76A3FD1F36F9DF4F95F4ED5C377AA064CBFFDD876684BB25639BE563B56ED9D96D61B49AAAEA2B7DFDBCB4A9C730CD394690E7ED4E1442EC2AC1911BBCE1AAE7A048F067983AC7D40A8C3258DD3D3787D4597F579AE1871759EBBE710F0B154F57E917CBDDC71BFD7CD27EFB80BF2EE4CBCFCAFE15221DA4BBD53A0252D53D6C19612813C84C756C8C366137943BCD3EF087D0AEA9F200899E89D411E3D290BE3C1F335BD3E60E2CBB853C5ED1D1BBB675FBEE2EDF480D87FFFBBBEA3B59F03787118F354BB998E7D55A72388A5EAD774D218EA2DF953DCF32ACFA8ABB7AE8FBDB5E7A824C9602226C5EE2549FFE490CBBB6FAB4C0AA2C7577F0F31D5B36621B3D4CFC8A6F7D58F4078FB66DEEF3F9605360DE235F36B57E016FA917C8F9A0D8A7864708A1996DD2336E8C1A615F375D03FC8A7A9DBF675D654E4D185681CC153B421965F343D72DAB6F5A6E84797F2E98EDE474E95BCAEF227D73569602C1E29BA41E5DD37D3A7AB7DD915BBB2D8E0C670E7850E2E4848430C8AE1038BE0BF0808C6D40E5D4132F9546DD76478F8441557549B6297952CC11C18A80B1787ED098FF56446CB979CA31D5EF5305574DF7C5B9B91724A59C7819727D4A89A0CF6949D4E3ADE4643F5ED37DFE8869BD44D35E4C64330D9FC9EC3DDF7CD68C8F95C82AB8D3C21FE161715F75592E127FF12F00C1FBF0A11E8BB62D2DEC2F3C3900191706AE484518346ECD086DE78280860CAA197B79772C0C599613BDD0F6DC00F76AEAF3AC34FF36D51451EEDBE0D06C3F8E5E8C779E8C7E10FF23E2FBAB7F5FDEDE9066FABA3AEE25353EC70CF1F8F7FC4A7AE18ADE284DDAB8EFCAB6CF369BF23433F5FA3F335E354A33F37C760A1BE1EFDF82F7D315ACB17A6AF2B0675FDE976BE6B2DDFB4B397B1C76DFBF4D1420C7073AC04F41F226DDCA15E49C66302F61502D21DFFF6120CFC555665F778071F77811F5B6170CCDF8E7ECA4F3D39F8657E98E593F30024DB6DD0346B7ECAE96E3318236CB2C9CE717E3539986E0A449EF553330C92E5E3D1CFFBB92BC731F1DF3704618372986ED7813BA0B96F3520F445A414B35F60FFBAD2005FC2A4658089FC1E4560FC76C01200F44A2A00924BA471C65FD95CAA519F2362FF5AFC132FFE3CE757950392A2FD5624DD76D474C34E9A11900C1FA30CBFF16010C00003DF77C5AFBD94039EE2487E8D614F7D346F35F42B1FCF8F41448FB7E745BBCBBACD435CFFDED41C8364F978F426FFDC1593F66896AF6AFE4F16671F2B17C50160B20DEC5B07B71463C9576212B0BD3A4AC7404FFADBA242918D04A13DB9780CA5476F36887D3A78FB41148A1486C46188466AD3C24D3C56B631440101549FFF8270F8D262AFF9E78433C96546D172728B24AC43F2706D9135FC93F632B9BA7F72DEB32457236B6C5FD6511A561B997575C52C0ED7688386C7C7A34628FDB587650254269EAFBA839D68EEAD0BB803AE2368220A8245C1164411093B57C3081C48348CAD0875BBA90543620185B3130F5556DC4C4002985A60660AD77787CB69F71D558313B1D43E72E7991D4048ACFCE5FA7613C8487FBBF8F4735694D9C7A2C4E390C4E521B4CAE0034ABF0A9787D8AF2370790C77E58713FDDB257BB17CDD5940980587FA6C2927507C065B1067A591F455324C6C62675F29B18AEED0B49C5C48C2C5DB1CAC6C240FBEB1978A9583707A8223875DF66D8863FF15EC5C877EF835966A907F43CDF218F892DE29AAE74268589402B6F4EB9008A64F664E0D3E5BD73A4EF02EBBBBBB42244B7BEC48ECA525CEFEA0BE1FBD30D0BD396A25D17744614B5A0FE651E80DDBE1EBE157D61F0A1A5691216085B4B70C0E5F54CC0D01495EB20442A26839F5DE238D54ACB4EB5841166C761C0720068C234BDE01DFF13C7CD796DBE005DCAA5ABBB9F40424F29817D5FDAD9CFA7023ADF39B5369FD66B779FF2D9EC0580F1A5D2180EB5C78DBC0B9D984B292EAAE415A7958E3A681E9F0AFEC115F067ECC7F1AD9D7917EF0936611B11AF899E32BBB33FA8718A31C876884616C5A40D67FFBAACE41A65E856834A14CC4766FA51EFEB46E2DF3215F3DCB0059024082DD86EBB05600DBC987AA10B7CB4DD701658B09873FBAB761A57D40321F83CDA0AFEB5E60863CB95361253958C981602D1587E036F83D2BBAB268BB04C906A6A604444BC1D1DF1E64BAE3D7666A0148E510584B0CD6700D5889C201F8076661486821AE250FEBD88A5612B1B6C1F83E7BDC624CA765598FC79717481E894DCAE831EC7F9BCB82D018830C288D22154207254383E13CA541EC9157BBE9C4E11653B5416D1BDD8338B607C9C157E035987A62D21CC5F1551D0893046094FB2A7F270F81B1183A3321109E95892B08568333C0861107E32766A62150B69E502412590CE975427A3BC146F80ECF4400A9771BC1C3371756D3139666C3DA5A029B2DB7A07FC4CD5BA459330C2CD180BE05539F0FF5DEABC7F89B1AA8CAD6528C789F8728C9B37317FCD5CF8B781779535A8817C6E9850EE2B9B96B8C262B6FC58BA4AE376B55633E34C6BE59367E8A36D1CD534A6240CFB11F3BE3D75ABA414FB5254C3DEC29A7BBC5901FCA7E702019FF455FB010401D70193BBD008C2D02B8E692384F0E9A0FD1001A4424A63EF9369B4E1D24DA0CA6D606E9B7821602B7F64EB037F852A88094E65EEA096F6CF2AD3CCFFBC1FE3D4B90ECEB4F6FDC4F5C5E75A127FB779162DB613A986DBB31F32FFC93EB189F28AD994F878C6F28DD7D30A39C5C791B8EF4DABA7B1E6BD0B0F437B60F4C002CCD67029B5410946DA60801CF4A94C86B439A628DFE2CDA6BE1A93D367D570EDE5FD30F76A2ED59CAD14EBF35331EEFD5376658DF8BC4DA8ED3C1287573BE63C0448A5CDE54AAF14D33A3138E72F2F96C3CD62B4FE75FEA6E492712F7DA16DD1483882D38FABD38D31DBF3693845CB4ED97BAC9AF518BBAF17DFADB375995C7BFD30B35CDC55D4000472F2060B74CDA9E87E5F024C65F75FC252C6185E57064E443FD09917C6700EDA107D846887AB2E42D8CC59102BD1C0775840D2953433FBD1B5F47A8E25A2C87222EABE91B73D95857DBD46DB7AB2B448212A70529607CA05244C4A6591981CABF92E041A86FDEADAF242EA902CE0F465C560844771497B5B7C890B8444F7A70388292361982AB90AC9B1801D3735794E8D75D9E7528F1261A689A951410E0F8ED14A85B47B489862426B235FB97A4D849CACAC1B0BBBAE9AEF7D5EDCFA8424D888B0FEAE0C7B139EE607EFE7AF462B0F4C5A4418AE92B8B01C93D9E6A3733B4C689C0F0E92BD9B38CDD39866DCA38F46F8A1CB713FD2644DA814F3BF18D877CE2F52124D6A49F1B4B115EC1B40A618BFFD258E2900BB65BC7B015054423E54B748720232B66613DB2F7E8786149F322DDEA3292FC553A17E9583957DFF480F065F5B92E36E34BCA0AC9A09FAC97BEA06C25252C0120D2B92CFE1BCA4683D643FACA0AD737CF76571095446FDBAD2F2049733E3B88C5A1BC6AC7C9C7F45BAB4DC0213D4E1D62A8F34792D28B8ABAE944425237DD156ADBEC1E054B0E6AA043965605F9A08BBE8A1CA15CA77CDB4D2F17694E4CD6168BD40B8BA5481CC6F9082719D1C30BFF120AC75653C8C263DBA1ED0DEA3A927C7E38C789AE24E8365971604B8E5F1A98FE18353A8DC0BA2FEA165BD43E20D4BDAEBAE631613E38B66136CC832BFA4A0E4DB86E79379C523A923DC63E35080BC457A02896BE1CD1F6749183F8AFE2AE220189DFC3B59281951FC365170809E9EE0378C86B84E5384DD0E9D7094DD3A9DE4CC64BD50D3687C925C844A1165CB3A2EAA1CBBE163B82EBD7511812FDDB4A5D96EC5975DC14F05C56FFD5C02039863564EED0A1AD21586D61CEE23A1DAE819A39C3468E2E8AA6EDCEB32EFB98B562EA03520B6FA046F8D37C5BE0393B7CA60672FC7EB37940DBECA7A7F9C71A8F73F6B19CAAB48068B09807B354403C7C86F092123DDAC9B92A209E0A20D4431932239A0CBC84F0A148463C29D53721412F476D86F6749F17DDDBFA1E1ACBB9081CCEB154DFC4AB6CF369BF83DBA0CAA046E6628356E6E43A62237311D8C6505A18306B78C30DC42FC76D229B1DBAAF1B88F4A50896CFBED484F4ABACCAEEC1993597402D8C857AFCCBE196D0C05204B530959A3431C654002D8C2570037DA11EFF392A8BCFA8797CDBE7E1131A618BA1966808F3E6144D299B3119752E76413A3463B96A7C7A10CB16259C0460B42D9BF174B651C139DA97C8A6292ED4E3EF17EAD3CF5951661F8B1217012D0130509B009849E3F249C0164B9B349E0E74F89C38845421387673B92153651D52F4C410F36F78AA2C9957E0565818698B34980903B3BBBB2BB4FD084F3DBA14662106301A2554814A712A918D0F2E349B51CBB382E0B45A8A65736B8230508A783E3C5EA1AE2936905EA44B41D5B800E8DBEA5331096DF45F21DCB8408F1378454768018081DA13C08C5B97B7A96CC98063B03EBF90AAF00B23AD3D2545158DDBB100346FFB3253E473DE4D491B73B9BCA911C460DE67A0A53E7C0627636662A9B319A404F46C31D40C0D61224B50261240B0203058CA44484B32C6FC136A2246202D093D9C0101D025649102080A24410434A001BCB42812018281540090265369BE29074CA3B90C9E4263B1492BC39D1CA089A100C64FCACC8C1A448567CB564DF58E828131B789E7403DA9453C43A8ECE111C8AC5D3AA8036C960690B5BAC01834CA860E886DB2E5609334888195C71D3688261E0700DA77348CC9D68A3A329137A869CBD082A5BDE4B0FD4A4348ADD705C8B0DDC1212BD968F465F25D062E165BA15C92ACEF6C8A967C424150FE33309892712BB3EE3D8C7CF924F854F93371AACEF081A3FA8425DBB84BF34B6ED25EC16FBDD99328D4206DC4EA1869E5169716F795AA773C58E82ED2FED0B9D6F0315C37077C8A0EF600724205222102D7E9DAC06445D7A0B08B831EB5FE8041DA2FAA3464A7988390BECAF8C5BF3BA383FDF674D3FB41802EB11041BBC51D0B0C3D9B3F7A776E76EC4F291365AA04060CD955E178A2AF467DF5EF6C5D7FBA5D3CF940271900D52AC71E148CEBDCF451D549EAE462E85FFFC1BB6BE3F98074D631E521478D3BCFE82BCDDF028DD8B43CCF786523C70306213BC9F84D7B0AE900B2002147903F31EA6B2D1FC38CE1FB869C9337285F104B0651840C447A9271ECBB306F5A255D94DCDE85EE4733FD1ABFADD5AD39145B370F05C0E39987D311F5ED800FEEE002E049A050833E389F6B0D1FC3754DBEC7E34142EFF36277713AAFBD3D2FDA5DD6E14DB1C49A8101436A55FE54B9AFB57C0CB660F487B2464BA4A646F8390A1E7A332A7A2C09CB0C72422D9DBE12C890F3587AF22E767D288DD07DF91497C2869EEB87C08651BE4CB800A666F017E64362C9ADCAF69240C6B0BDD2E98559CD4B058183883BFEB1D784B933D76883C6076E557DA6C08E69E9A3C3BB6E17BCF28EB2808108076B0A939A2D08DB754A5168FABE4046534EABB063B65759BC0AC396010CDC81A4A6AE10B6A658F3A5B0A1D77C69585E8F401F69E7C80620459D8C0BBA6C76EC941022EF86B9407DD63103DAC8B305815920773D8070E15D1049BB2D759751A5219736264472E955A8EED0718DF45D62590765F0C1BB0CC56E2EDD674BFD8DD6252253EE0EE561827A44C580D171E253DFA38EF818736A37EC43A5A0DD5A5912C4DE8ED3CD8431E04533FBB97C082C98B4B6BAF72C54848EA7D2EA8CA9C2366062DC3035027769657367BC2071CBA285ED5D012E5A4704C3978A389BEDDEFE5B3006A87DD814440C17768AEE551B5496D2C378002CE40218B79363D8A6C65C07A06259EADCDD8DB96AFF2D5877E5A60C551ED68C89D9AD4906678C723195A45B77237585D9285D765980B02B6EC2AE69CEAB05B8986B6AFC6E4F97A09447BC2C50E8635EFE22D75C732908DB55F572C983C55833937759A39704B838CA2945B7850B6AB7FDDD39B1DF30A0BC03D4C5BC9EEEFEB7A2C3D24B777D6DA03454D76FDF37F506B5ADCC5E82C042AEB4DC7D3FBABB013B798DEEF655FE0EF40B0830418885EA0921EDE13BAA98BA3C48F8599B642CA92938E33699AF1370B4D14D3877B13699F35F019DA78B83980BB6AACDA54B7D3C85EABE09071152095DF0675517610ED9865BB7B7C3B995D823BA38D071185393BD94DCD79B3E85EA9A66F500A0428E5BFC0E8E37A66F27B4B22E72700148066A7137C0A9CA7349A87155AC231C44F86524E6A8F64A443E987471E8318CA5647A9AB14D2C0D5A61018E416F9205EC42128F3097799295624D434A5943F1442D72776675A3EAD504145CAB25E82A4967215DB697C2188B369D7A6350995990AB853DD50AB5CE948757EAB1BA45E46D4005CB625FE64956A2B9A5181CBA38FCD8C4E9129DAB45EAC1178142AE4F50BE99BE265B10606B2AE685A15EC901F7A7CA1A613D2CF22C38E30E150288C314991CC881BF3A56F4497B6E67E41A5EB0D091BB26C7C8A42D12F18DC531186524311468347949C50231BB92CAD7A3020FEFFB5164921A380295476189CAD1A9008FE0F43C189648CF5AE5C061CF5D57620590084CB3F8AA6B04D5208AB467034F4080384C91AA5229F0D7C08A396FDBEDCF783FD9C81DCC3060587F25977D6EDCECCE5F03749684B4AA160C0E22FC1AC126BF1B7B387C0AD5BD37458EF5B26C145990B0E317AF6BFC33F08A9D3A0C1961CF0E661AA4AB878B5E053AA50D704B7823654D56C8438C24908A6E38DE4C49DA7DF0D17BC5BD5B1650D5F970F790B93C980C96B92C382BD4F75A4C9F80F75482EBB2C0E422B6FA8D73B033602F0E8C0DE2CBDC121E689EF066BAE19EA1404CCB3AF79D2E0ADD75A5656FF650758009B052E75529AE94CF301F539725CF0843DD367971D8B7EB5032E0A1E76C89FF051EF8A55CE8E28EC19BBA416C79382FF1E00EE08AC2755F777D53FB64ACE7880B6991D9FE06EDAAFC5A9AEA41546F4F4DC22EBEE65EF5D48BB3F4514CF70EAC27D492572B65B7F1740F5C8699D57046EE4576E8B240D730979719A57730258F37FA0B3B9F087CA9377C35EEE2CB9301C1FCF4E05CF6F264C8233E7EC03F3176BC1E5ED5392ADBFEEBCB93EB3DAEBD45C3AF73D4E7959E50BCC4382BBC812AA8A70E6798CBEAAE9E9E5CE4289A40A6E229A929EA32BC1A66A74D57DC659B6E8CC0EB25E3B7ACDC6390D7DB8F28BFACDEEDBBDDBEC35D46DB8F2523F2E4E54655FB2F4F049A5FBEDB915F6D882E60320BB2A0BFAB5EED8B329FE9BEC8CA96B3336428C89390A32F8F8C65477C7AF78F33A65F6A3E2C48866864DFFC92E507B4DD951859FBAEBAC93E2317DAF032F416DD679B47FCFD73D1EF3B6548F403C1B2FDE57991DD37D9B61D712CF5F14F2CC3F9F68FFFFEFF03545C28B17E640500 , N'6.5.1')


GO
PRINT 'StudyLink schema created.';
