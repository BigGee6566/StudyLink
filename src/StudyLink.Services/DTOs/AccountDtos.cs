using System;
using System.Collections.Generic;
using StudyLink.Data.Entities;

namespace StudyLink.Services.DTOs
{
    public class PersonalDetails
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string CellNumber { get; set; }
        public Gender Gender { get; set; } = Gender.PreferNotToSay;
        public int CampusId { get; set; }
    }

    public sealed class StudentRegistration : PersonalDetails
    {
        public string StudentNumber { get; set; }
        public int? StudyLevelId { get; set; }
        public string Faculty { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool AcceptedTerms { get; set; }
    }

    public sealed class SubjectChoice
    {
        public int SubjectId { get; set; }
        public int StudyLevelId { get; set; }
    }

    public sealed class TutorRegistration : PersonalDetails
    {
        public string Qualification { get; set; }
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Bio { get; set; }
        public IList<SubjectChoice> Subjects { get; set; } = new List<SubjectChoice>();
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool AcceptedTerms { get; set; }
    }

    public sealed class SignInResult
    {
        public bool Succeeded { get; set; }
        public string Error { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool MustChangePassword { get; set; }
    }

    /// <summary>Account created by an Admin, Manager or Staff member (never self-service).</summary>
    public sealed class CreateAccountRequest : PersonalDetails
    {
        public string Role { get; set; }

        // Staff / Manager
        public string EmployeeNumber { get; set; }
        public string JobTitle { get; set; }

        // Supplier
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string VatNumber { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        // Student (captured by Staff, UC6)
        public string StudentNumber { get; set; }
        public int? StudyLevelId { get; set; }
        public string Faculty { get; set; }
    }

    public sealed class UserFilter
    {
        public string Search { get; set; }
        public string Role { get; set; }
        public bool? Active { get; set; }
        public int? CampusId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class UserRow
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CellNumber { get; set; }
        public string Campus { get; set; }
        public string Roles { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Extra { get; set; }
    }

    public sealed class UserDetail
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string CellNumber { get; set; }
        public Gender Gender { get; set; }
        public int CampusId { get; set; }
        public string Campus { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();

        public int? StudentId { get; set; }
        public string StudentNumber { get; set; }
        public int? StudyLevelId { get; set; }
        public string StudyLevel { get; set; }
        public string Faculty { get; set; }

        public int? TutorId { get; set; }
        public TutorVerificationStatus? TutorStatus { get; set; }
        public string Qualification { get; set; }
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Bio { get; set; }

        public int? StaffId { get; set; }
        public string EmployeeNumber { get; set; }
        public string JobTitle { get; set; }

        public int? SupplierId { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string VatNumber { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierAddress { get; set; }

        public int? ManagerId { get; set; }
        public int? AdminId { get; set; }

        public string FullName => FirstName + " " + LastName;
    }

    public sealed class RoleRow
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalMembers { get; set; }
        public IList<string> Permissions { get; set; } = new List<string>();
        public bool SelfRegister { get; set; }
    }

    public sealed class ProfileChangeRow
    {
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string FieldName { get; set; }
        public string CurrentValue { get; set; }
        public string RequestedValue { get; set; }
        public string Reason { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string HandledBy { get; set; }
        public string ResponseNote { get; set; }
    }

    public sealed class ResetRequestRow
    {
        public int RequestId { get; set; }
        public string Identifier { get; set; }
        public int? UserId { get; set; }
        public string MatchedUser { get; set; }
        public string MatchedEmail { get; set; }
        public string Roles { get; set; }
        public string Message { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string HandledBy { get; set; }
        public DateTime? HandledAt { get; set; }
        public string ResponseNote { get; set; }
        public string RequestIp { get; set; }
    }

    public sealed class VerificationRow
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Campus { get; set; }
        public string Qualification { get; set; }
        public int ExperienceYears { get; set; }
        public decimal HourlyRate { get; set; }
        public string Subjects { get; set; }
        public TutorVerificationStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public sealed class VerificationHistoryRow
    {
        public DateTime SubmittedAt { get; set; }
        public VerificationDecision Decision { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool IdentityChecked { get; set; }
        public bool QualificationChecked { get; set; }
        public string Notes { get; set; }
    }

    public sealed class TutorApplication
    {
        public VerificationRow Summary { get; set; }
        public string CellNumber { get; set; }
        public string Bio { get; set; }
        public IList<VerificationHistoryRow> History { get; set; } = new List<VerificationHistoryRow>();
    }

    public sealed class LookupItem
    {
        public LookupItem() { }

        public LookupItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }
}
