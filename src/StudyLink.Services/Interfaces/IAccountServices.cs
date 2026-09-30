using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>Username or email + password. Locks after repeated failures (SEC-08).</summary>
        SignInResult SignIn(string identifier, string password, string ipAddress);

        /// <summary>BR-25: re-checked on every authenticated request so deactivation takes effect at once.</summary>
        bool IsActive(int userId);

        IList<string> RolesOf(int userId);
        ServiceResult ChangePassword(Actor actor, string currentPassword, string newPassword, string confirmPassword);

        /// <summary>True while no System Admin exists (first-run setup, ported from TutorConnect).</summary>
        bool SetupRequired();
        ServiceResult CreateFirstAdmin(PersonalDetails details, string password, string confirm, string ipAddress);
    }

    public interface IRegistrationService
    {
        ServiceResult<int> RegisterStudent(StudentRegistration registration, string ipAddress);

        /// <summary>Creates the tutor as PendingVerification and notifies everyone who can verify.</summary>
        ServiceResult<int> RegisterTutor(TutorRegistration registration, string ipAddress);

        /// <summary>Wizard step 1 check; the final submit re-checks (and the unique index is the backstop).</summary>
        string CheckAccountAvailable(string userName, string email);
    }

    public interface IUserManagementService
    {
        PagedResult<UserRow> Search(Actor actor, UserFilter filter);
        UserDetail Get(Actor actor, int userId);
        ServiceResult<int> CreateAccount(Actor actor, CreateAccountRequest request, string setupLinkBase);
        ServiceResult UpdateDetails(Actor actor, int userId, CreateAccountRequest details);
        ServiceResult SetActive(Actor actor, int userId, bool active, string reason);
        ServiceResult AssignRole(Actor actor, int userId, string role);
        ServiceResult RemoveRole(Actor actor, int userId, string role);
        ServiceResult Unlock(Actor actor, int userId);
        IList<RoleRow> Roles(Actor actor);
        IList<UserRow> LockedAccounts(Actor actor);
        IList<string> CreatableRoles(Actor actor);
    }

    public interface IProfileService
    {
        UserDetail Get(int userId);
        ServiceResult UpdateContact(Actor actor, string email, string cellNumber, int campusId, Gender gender);
        ServiceResult UpdateTutorDetails(Actor actor, string qualification, int experienceYears, decimal hourlyRate, string bio);
        ServiceResult RequestChange(Actor actor, string fieldName, string requestedValue, string reason);
        IList<ProfileChangeRow> MyRequests(int userId);
        IList<ProfileChangeRow> Pending(Actor actor);
        ServiceResult Review(Actor actor, int requestId, bool approve, string note);

        /// <summary>Fields users cannot change on their own (they go through <see cref="RequestChange"/>).</summary>
        IList<string> RequestableFields(int userId);
    }

    public interface IPasswordResetService
    {
        /// <summary>Always "succeeds" from the caller's view so it never reveals whether an account exists.</summary>
        ServiceResult Request(string identifier, string message, string ipAddress);

        IList<ResetRequestRow> List(Actor actor, RequestStatus? status);
        ServiceResult Approve(Actor actor, int requestId, string note, string resetLinkBase);
        ServiceResult Reject(Actor actor, int requestId, string note);

        /// <summary>For accounts created by staff: email a first "set your password" link.</summary>
        void IssueSetupLink(Actor actor, int userId, string resetLinkBase);

        bool IsTokenValid(string token);
        ServiceResult ResetWithToken(string token, string newPassword, string confirmPassword, string ipAddress);
    }

    public interface ITutorVerificationService
    {
        IList<VerificationRow> Queue(Actor actor, TutorVerificationStatus? status);
        TutorApplication Get(Actor actor, int tutorId);
        ServiceResult Decide(Actor actor, int tutorId, bool approve, bool identityChecked, bool qualificationChecked, string notes);
        ServiceResult Suspend(Actor actor, int tutorId, string reason);
        ServiceResult Reinstate(Actor actor, int tutorId, string reason);
    }

    /// <summary>Drop-down data shared by many pages.</summary>
    public interface ILookupService
    {
        IList<LookupItem> Campuses();
        IList<LookupItem> Subjects(bool activeOnly = true);
        IList<LookupItem> StudyLevels();
        IList<LookupItem> Categories(bool activeOnly = true);
        IList<LookupItem> Suppliers();
    }
}
