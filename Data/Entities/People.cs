using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    [Table("Campuses")]
    public class Campus
    {
        public int CampusId { get; set; }

        [Required, StringLength(50), Index(IsUnique = true)]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// The single account record (TutorConnect Person). Role-specific details live in the 1:1
    /// profile tables; roles themselves are many-to-many through UserRoles.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }

        [Required, StringLength(50), Index(IsUnique = true)]
        public string UserName { get; set; }

        [Required, StringLength(100), Index(IsUnique = true)]
        public string Email { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        public Gender Gender { get; set; }

        [Required, StringLength(15)]
        public string CellNumber { get; set; }

        public int CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        // PBKDF2 output, never a password. Iterations stored per row so the work factor can rise
        // without invalidating existing accounts (ported from TutorConnect D-07).
        [Required, MaxLength(64)]
        public byte[] PasswordHash { get; set; }

        [Required, MaxLength(32)]
        public byte[] PasswordSalt { get; set; }

        public int HashIterations { get; set; }

        public bool IsActive { get; set; } = true;
        public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Admin → Users: which administrator or manager created a non-self-registered account.</summary>
        public int? CreatedByUserId { get; set; }
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        [NotMapped]
        public string FullName => FirstName + " " + LastName;
    }

    /// <summary>
    /// A profile picture, in its own table so that reading a User (which happens on every signed-in
    /// request) never loads image bytes. One row per user at most; no row means the default avatar.
    /// </summary>
    [Table("UserPhotos")]
    public class UserPhoto
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        public byte[] Photo { get; set; }

        [Required, StringLength(50)]
        public string ContentType { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class Role
    {
        public int RoleId { get; set; }

        [Required, StringLength(30), Index(IsUnique = true)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; }

        public DateTime AssignedAt { get; set; }

        /// <summary>Admin → Roles: who granted this role (null for self-registration and seeding).</summary>
        public int? AssignedByUserId { get; set; }
        public virtual User AssignedBy { get; set; }
    }

    public class Student
    {
        public int StudentId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string StudentNumber { get; set; }

        public int? StudyLevelId { get; set; }
        public virtual StudyLevel StudyLevel { get; set; }

        [StringLength(100)]
        public string Faculty { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class Tutor
    {
        public int TutorId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string Qualification { get; set; }

        public int ExperienceYears { get; set; }

        /// <summary>Session fee = HourlyRate × session length. Replaces TutorConnect RatePerSession.</summary>
        public decimal HourlyRate { get; set; }

        [StringLength(1000)]
        public string Bio { get; set; }

        public TutorVerificationStatus VerificationStatus { get; set; } = TutorVerificationStatus.PendingVerification;
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<TutorSubject> Subjects { get; set; } = new List<TutorSubject>();
        public virtual ICollection<TutorVerification> Verifications { get; set; } = new List<TutorVerification>();
    }

    [Table("Staff")]
    public class StaffMember
    {
        [Key]
        public int StaffId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string EmployeeNumber { get; set; }

        [StringLength(60)]
        public string JobTitle { get; set; }
    }

    public class Supplier
    {
        public int SupplierId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(120)]
        public string CompanyName { get; set; }

        [Required, StringLength(100)]
        public string ContactPerson { get; set; }

        [StringLength(20)]
        public string VatNumber { get; set; }

        [Required, StringLength(20)]
        public string Phone { get; set; }

        [StringLength(250)]
        public string Address { get; set; }
    }

    public class Manager
    {
        public int ManagerId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string EmployeeNumber { get; set; }
    }

    public class Admin
    {
        public int AdminId { get; set; }

        [Index(IsUnique = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }
    }

    /// <summary>
    /// One row per verification decision, so a tutor who is rejected and re-applies keeps history.
    /// Staff → TutorVerification via ReviewedByStaffId; a Manager's decision leaves it null but still
    /// records ReviewedByUserId.
    /// </summary>
    public class TutorVerification
    {
        [Key]
        public int VerificationId { get; set; }

        public int TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        public DateTime SubmittedAt { get; set; }
        public VerificationDecision Decision { get; set; } = VerificationDecision.Pending;

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedBy { get; set; }

        public int? ReviewedByStaffId { get; set; }
        public virtual StaffMember ReviewedByStaff { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public bool IdentityChecked { get; set; }
        public bool QualificationChecked { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}
