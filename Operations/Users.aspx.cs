using System;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>
    /// SC23 - User Management. Staff capture students; Managers create Staff and Supplier accounts;
    /// System Admins manage everyone and assign roles (brief §7). The service enforces the same scope.
    /// </summary>
    [RequirePermission(Permission.CaptureStudents, Permission.CreateStaffAndSuppliers, Permission.ManageAllUsers)]
    public partial class Users : BasePage
    {
        private int? EditedUserId => QueryInt("id");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            if (EditedUserId.HasValue || Query("new") == "1") ShowEditor();
            else ShowList();
        }

        // ------------------------------------------------------------------ list

        private void ShowList()
        {
            Views.SetActiveView(ListView);
            var role = Query("role");
            Heading = role == RoleNames.Student ? "Students" : role == RoleNames.Staff ? "Staff" : role == RoleNames.Supplier ? "Suppliers" : "User Management";
            Subheading = "Search, create and manage accounts.";

            var visibleRoles = RolePermissions.Has(Actor, Permission.ManageAllUsers)
                ? new[] { RoleNames.Student, RoleNames.Tutor, RoleNames.Staff, RoleNames.Supplier, RoleNames.Manager, RoleNames.SystemAdmin }
                : S.Users.CreatableRoles(Actor).Concat(new[] { RoleNames.Tutor }).Distinct().ToArray();
            Ui.FillText(RoleFilter, visibleRoles, "All roles", Fmt.Words);
            Ui.Select(RoleFilter, role);
            Ui.Select(ActiveFilter, Query("active"));
            SearchText.Text = Query("q");
            NewLink.HRef = ResolveUrl("~/Operations/Users.aspx?new=1" + (string.IsNullOrEmpty(role) ? "" : "&role=" + role));
            NewLink.Visible = S.Users.CreatableRoles(Actor).Count > 0;

            var result = S.Users.Search(Actor, new UserFilter
            {
                Search = SearchText.Text,
                Role = string.IsNullOrEmpty(RoleFilter.SelectedValue) ? null : RoleFilter.SelectedValue,
                Active = ActiveFilter.SelectedValue == "" ? (bool?)null : ActiveFilter.SelectedValue == "1",
                Page = QueryInt("page") ?? 1
            });
            Rows.DataSource = result.Items;
            Rows.DataBind();
            EmptyState.Text = result.Total == 0 ? Ui.Empty("users", "No accounts match.") : "";
            Pager.Text = Ui.Pager(Request, result);
        }

        protected void Search_Click(object sender, EventArgs e) =>
            Go("~/Operations/Users.aspx?q=" + Server.UrlEncode(SearchText.Text.Trim()) + "&role=" + RoleFilter.SelectedValue + "&active=" + ActiveFilter.SelectedValue);

        // ------------------------------------------------------------------ create / edit

        private void ShowEditor()
        {
            Views.SetActiveView(EditView);
            Ui.Fill(Campus, S.Lookups.Campuses());
            Ui.FillEnum<Gender>(Gender);
            Ui.Select(Gender, ((int)Data.Entities.Gender.PreferNotToSay).ToString(CultureInfo.InvariantCulture));
            Ui.Fill(StudyLevel, S.Lookups.StudyLevels(), "Not set");

            if (!EditedUserId.HasValue)
            {
                Heading = "New account";
                Subheading = "Staff, supplier and manager accounts are created here, never self-registered.";
                FormTitle.Text = "Account details";
                Ui.FillText(NewRole, S.Users.CreatableRoles(Actor), null, Fmt.Words);
                Ui.Select(NewRole, Query("role"));
                RoleChoice.Visible = CreateButton.Visible = CreateHint.Visible = true;
                SaveButton.Visible = AccountSide.Visible = false;
                ToggleRoleFields(NewRole.SelectedValue);
                return;
            }

            var u = S.Users.Get(Actor, EditedUserId.Value);
            if (u == null)
            {
                RedirectWith("~/Operations/Users.aspx", "That account could not be found.", AlertKind.Warning);
                return;
            }
            Heading = u.FullName;
            Subheading = string.Join(", ", u.Roles.Select(RoleNames.DisplayName)) + " · " + u.UserName;
            FormTitle.Text = "Details";
            RoleChoice.Visible = CreateButton.Visible = CreateHint.Visible = false;
            SaveButton.Visible = AccountSide.Visible = true;

            FirstName.Text = u.FirstName; LastName.Text = u.LastName; UserName.Text = u.UserName; UserName.Enabled = false;
            Email.Text = u.Email; Cell.Text = u.CellNumber;
            Ui.Select(Campus, u.CampusId.ToString(CultureInfo.InvariantCulture));
            Ui.Select(Gender, ((int)u.Gender).ToString(CultureInfo.InvariantCulture));
            EmployeeNumber.Text = u.EmployeeNumber; JobTitle.Text = u.JobTitle;
            CompanyName.Text = u.CompanyName; ContactPerson.Text = u.ContactPerson; VatNumber.Text = u.VatNumber; Phone.Text = u.SupplierPhone; Address.Text = u.SupplierAddress;
            StudentNumber.Text = u.StudentNumber; Faculty.Text = u.Faculty;
            if (u.StudyLevelId.HasValue) Ui.Select(StudyLevel, u.StudyLevelId.Value.ToString(CultureInfo.InvariantCulture));
            EmployeeFields.Visible = u.StaffId.HasValue || u.ManagerId.HasValue;
            SupplierFields.Visible = u.SupplierId.HasValue;
            StudentFields.Visible = u.StudentId.HasValue;

            var locked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTime.Now;
            StatusBadge.Text = Fmt.Badge(u.IsActive ? "Active" : "Inactive") + (locked ? " " + Fmt.Badge("Locked") : "");
            Created.Text = Fmt.DateTime(u.CreatedAt) + (string.IsNullOrEmpty(u.CreatedBy) ? "" : " by " + H(u.CreatedBy));
            LastLogin.Text = Fmt.DateTime(u.LastLoginAt);
            Failed.Text = u.FailedLoginCount.ToString(CultureInfo.InvariantCulture);
            DeactivateButton.Visible = u.IsActive && u.UserId != CurrentUser.UserId;
            ActivateButton.Visible = !u.IsActive;
            UnlockButton.Visible = locked && RolePermissions.Has(Actor, Permission.ManageSecurity);

            RolesPanel.Visible = RolePermissions.Has(Actor, Permission.AssignRoles);
            if (RolesPanel.Visible)
            {
                RoleList.DataSource = u.Roles;
                RoleList.DataBind();
                Ui.FillText(AddRole, new[] { RoleNames.Staff, RoleNames.Manager, RoleNames.SystemAdmin }.Where(r => !u.Roles.Contains(r)), null, Fmt.Words);
                AddRoleButton.Visible = AddRole.Items.Count > 0;
            }
        }

        private void ToggleRoleFields(string role)
        {
            EmployeeFields.Visible = role == RoleNames.Staff || role == RoleNames.Manager;
            SupplierFields.Visible = role == RoleNames.Supplier;
            StudentFields.Visible = role == RoleNames.Student;
        }

        protected void NewRole_Changed(object sender, EventArgs e) => ToggleRoleFields(NewRole.SelectedValue);

        private CreateAccountRequest ReadForm() => new CreateAccountRequest
        {
            Role = NewRole.SelectedValue,
            UserName = UserName.Text, FirstName = FirstName.Text, LastName = LastName.Text, Email = Email.Text, CellNumber = Cell.Text,
            CampusId = Ui.Int(Campus) ?? 0, Gender = Ui.EnumOf<Gender>(Gender) ?? Data.Entities.Gender.PreferNotToSay,
            EmployeeNumber = EmployeeNumber.Text, JobTitle = JobTitle.Text,
            CompanyName = CompanyName.Text, ContactPerson = ContactPerson.Text, VatNumber = VatNumber.Text, Phone = Phone.Text, Address = Address.Text,
            StudentNumber = StudentNumber.Text, StudyLevelId = Ui.Int(StudyLevel), Faculty = Faculty.Text
        };

        protected void Create_Click(object sender, EventArgs e)
        {
            var result = S.Users.CreateAccount(Actor, ReadForm(), ResetLinkBase);
            if (Show(result))
                RedirectWith("~/Operations/Users.aspx?id=" + result.Value, "Account created. A link to set a password has been emailed to the user.");
        }

        protected void Save_Click(object sender, EventArgs e) =>
            After(S.Users.UpdateDetails(Actor, EditedUserId ?? 0, ReadForm()), "Details saved.");

        protected void Deactivate_Click(object sender, EventArgs e) => After(S.Users.SetActive(Actor, EditedUserId ?? 0, false, Reason.Text), "Account deactivated.");
        protected void Activate_Click(object sender, EventArgs e) => After(S.Users.SetActive(Actor, EditedUserId ?? 0, true, Reason.Text), "Account reactivated.");
        protected void Unlock_Click(object sender, EventArgs e) => After(S.Users.Unlock(Actor, EditedUserId ?? 0), "Account unlocked.");
        protected void AddRole_Click(object sender, EventArgs e) => After(S.Users.AssignRole(Actor, EditedUserId ?? 0, AddRole.SelectedValue), "Role assigned.");

        protected void Role_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "remove") After(S.Users.RemoveRole(Actor, EditedUserId ?? 0, (string)e.CommandArgument), "Role removed.");
        }

        private void After(ServiceResult result, string message)
        {
            if (Show(result)) RedirectWith("~/Operations/Users.aspx?id=" + EditedUserId, message);
        }
    }
}
