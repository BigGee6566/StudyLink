USE StudyLink;
SET XACT_ABORT ON;

DECLARE @UserName  nvarchar(50)  = N'Inga.admin';
DECLARE @Email     nvarchar(100) = N'ingamaqulwana@admin.ac.za';
DECLARE @FirstName nvarchar(50)  = N'Inga';
DECLARE @LastName  nvarchar(50)  = N'Maqulwana';
DECLARE @Cell      nvarchar(15)  = N'0449678009';
DECLARE @Campus    nvarchar(50)  = N'Alice';          -- Alice, East London or Bisho
DECLARE @CopyPasswordFrom nvarchar(50) = N'admin';    -- an account whose password you know

BEGIN TRAN;

IF EXISTS (SELECT 1 FROM Users WHERE UserName = @UserName OR Email = @Email)
    THROW 50001, 'That username or email is already taken.', 1;

INSERT INTO Users (UserName, Email, FirstName, LastName, Gender, CellNumber, CampusId,
                   PasswordHash, PasswordSalt, HashIterations, IsActive, FailedLoginCount,
                   MustChangePassword, CreatedAt)
SELECT @UserName, @Email, @FirstName, @LastName, 4, @Cell,
       (SELECT CampusId FROM Campuses WHERE Name = @Campus),
       src.PasswordHash, src.PasswordSalt, src.HashIterations, 1, 0,
       1, GETDATE()
FROM Users src
WHERE src.UserName = @CopyPasswordFrom;

IF @@ROWCOUNT = 0
    THROW 50002, 'The account named in @CopyPasswordFrom was not found.', 1;

DECLARE @UserId int = SCOPE_IDENTITY();

INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
SELECT @UserId, RoleId, GETDATE() FROM Roles WHERE Name = N'SystemAdmin';

INSERT INTO Admins (UserId) VALUES (@UserId);

COMMIT;

SELECT u.UserId, u.UserName, u.Email, r.Name AS Role, u.MustChangePassword
FROM Users u JOIN UserRoles ur ON ur.UserId = u.UserId JOIN Roles r ON r.RoleId = ur.RoleId
WHERE u.UserId = @UserId;