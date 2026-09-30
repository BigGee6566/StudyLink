using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyLink.Data.Entities
{
    /// <summary>
    /// SC27. A user asks for a reset; Staff verify who they are and approve, which emails a
    /// single-use link. UserId is null when the identifier matched nobody - the request is still
    /// stored so the response never reveals whether an account exists.
    /// </summary>
    public class PasswordResetRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int? UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string Identifier { get; set; }

        [StringLength(300)]
        public string Message { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public DateTime CreatedAt { get; set; }

        public int? HandledByUserId { get; set; }
        public virtual User HandledBy { get; set; }

        public DateTime? HandledAt { get; set; }

        [StringLength(200)]
        public string ResponseNote { get; set; }

        [StringLength(45)]
        public string RequestIp { get; set; }
    }

    /// <summary>Only the SHA-256 of the emailed token is stored.</summary>
    public class PasswordResetToken
    {
        [Key]
        public int TokenId { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, MaxLength(32), Index(IsUnique = true)]
        public byte[] TokenHash { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public int? RequestId { get; set; }
        public virtual PasswordResetRequest Request { get; set; }
    }

    /// <summary>Fields a user may not change alone (name, student number) go through Staff.</summary>
    public class ProfileUpdateRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(50)]
        public string FieldName { get; set; }

        [StringLength(200)]
        public string CurrentValue { get; set; }

        [Required, StringLength(200)]
        public string RequestedValue { get; set; }

        [StringLength(300)]
        public string Reason { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public DateTime CreatedAt { get; set; }

        public int? HandledByUserId { get; set; }
        public virtual User HandledBy { get; set; }

        public DateTime? HandledAt { get; set; }

        [StringLength(200)]
        public string ResponseNote { get; set; }
    }

    /// <summary>Landing page "Contact / Support" and the in-app support form.</summary>
    public class SupportMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int? UserId { get; set; }
        public virtual User User { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        public int? CampusId { get; set; }
        public virtual Campus Campus { get; set; }

        [Required, StringLength(150)]
        public string Subject { get; set; }

        [Required, StringLength(2000)]
        public string Body { get; set; }

        public SupportStatus Status { get; set; } = SupportStatus.Open;
        public DateTime CreatedAt { get; set; }

        public int? HandledByUserId { get; set; }
        public virtual User HandledBy { get; set; }

        public DateTime? HandledAt { get; set; }

        [StringLength(500)]
        public string ResponseNote { get; set; }
    }

    /// <summary>Business rules a System Admin may tune without a code change (rental period, fees…).</summary>
    public class SystemSetting
    {
        [Key, StringLength(60)]
        public string SettingKey { get; set; }

        [Required, StringLength(200)]
        public string Value { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int? UpdatedByUserId { get; set; }
        public virtual User UpdatedBy { get; set; }
    }
}
