using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Validation;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>
    /// SC2-SC4 multi-step registration. Only students and tutors self-register (brief §7); tutors
    /// are created PendingVerification. Each step validates on the server, and the final submit
    /// re-validates everything inside RegistrationService.
    /// </summary>
    public partial class Register : BasePage
    {
        private const string PasswordKey = "StudyLink.Register.Password";

        private bool IsTutor
        {
            get => ViewState["tutor"] as bool? ?? false;
            set => ViewState["tutor"] = value;
        }

        protected string LoginUrl => "~/Account/Login.aspx?registered=" + (IsTutor ? "tutor" : "student");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            if (IsSignedIn)
            {
                Go(WebAuth.HomeUrl(CurrentUser.Roles));
                return;
            }
            Campus.DataSource = S.Lookups.Campuses();
            Campus.DataBind();
            var preferred = CampusPreference.Get(Context);
            if (preferred.HasValue) Campus.SelectedValue = preferred.Value.ToString(CultureInfo.InvariantCulture);
            StudyLevel.DataSource = S.Lookups.StudyLevels();
            StudyLevel.DataBind();

            var levels = S.Lookups.StudyLevels();
            SubjectPicks.ItemDataBound += (s, a) =>
            {
                var ddl = (DropDownList)a.Item.FindControl("Level");
                ddl.DataSource = levels;
                ddl.DataTextField = "Name";
                ddl.DataValueField = "Id";
                ddl.DataBind();
            };
            SubjectPicks.DataSource = S.Lookups.Subjects();
            SubjectPicks.DataBind();

            var role = (Query("role") ?? "").ToLowerInvariant();
            if (role == "student" || role == "tutor")
            {
                IsTutor = role == "tutor";
                RoleTutor.Checked = IsTutor;
                RoleStudent.Checked = !IsTutor;
                Show(AccountStep);
            }
            else
            {
                Show(RoleStep);
            }
        }

        private void Show(View view)
        {
            Steps.SetActiveView(view);
            var index = view == RoleStep ? 0 : view == AccountStep ? 1 : view == DoneStep ? 3 : 2;
            Stepper.Text = string.Concat(Enumerable.Range(0, 4).Select(i => i <= index ? "<span class=\"is-done\"></span>" : "<span></span>"));
            AccountHeading.Text = IsTutor ? "Register as a tutor" : "Register as a student";
            Title = view == RoleStep ? "Choose your role" : view == DoneStep ? "Registration complete" : AccountHeading.Text;
        }

        protected void RoleNext_Click(object sender, EventArgs e)
        {
            IsTutor = RoleTutor.Checked;
            Show(AccountStep);
        }

        protected void Back_Click(object sender, EventArgs e)
        {
            Show(Steps.GetActiveView() == AccountStep ? RoleStep : AccountStep);
        }

        protected void AccountNext_Click(object sender, EventArgs e)
        {
            var error = UserValidator.CheckPersonalDetails(UserName.Text, FirstName.Text, LastName.Text, Email.Text, Cell.Text)
                        ?? UserValidator.CheckPassword(Password.Text, ConfirmPassword.Text)
                        ?? S.Registration.CheckAccountAvailable(UserName.Text, Email.Text);
            if (error != null)
            {
                Fail(error);
                Show(AccountStep);
                return;
            }
            // Password boxes are not round-tripped to the browser (not even in encrypted ViewState):
            // hold it server-side in Session until the final step, then discard it.
            Session[PasswordKey] = Password.Text;
            Show(IsTutor ? TutorStep : StudentStep);
        }

        protected void Create_Click(object sender, EventArgs e)
        {
            var password = Session[PasswordKey] as string;
            if (string.IsNullOrEmpty(password))
            {
                Fail("Please enter your password again.");
                Show(AccountStep);
                return;
            }

            var result = IsTutor ? CreateTutor(password) : CreateStudent(password);
            if (!result.Succeeded)
            {
                Fail(result.Error);
                Show(IsTutor ? TutorStep : StudentStep);
                return;
            }

            Session.Remove(PasswordKey);
            DoneHeading.Text = IsTutor ? "Application submitted" : "You're registered!";
            DoneText.Text = IsTutor
                ? "Your tutor profile is Pending Verification. Hub staff will check your identity and qualification and notify you in StudyLink. You can log in meanwhile to complete your profile."
                : "Your StudyLink student account has been created. Log in to book tutors, rent textbooks and view your fees.";
            Show(DoneStep);
        }

        private T Fill<T>(T d) where T : PersonalDetails
        {
            d.UserName = UserName.Text;
            d.FirstName = FirstName.Text;
            d.LastName = LastName.Text;
            d.Email = Email.Text;
            d.CellNumber = Cell.Text;
            d.CampusId = int.TryParse(Campus.SelectedValue, out var c) ? c : 0;
            d.Gender = Enum.TryParse(Gender.SelectedValue, out Gender g) ? g : StudyLink.Data.Entities.Gender.PreferNotToSay;
            return d;
        }

        private Services.Infrastructure.ServiceResult<int> CreateStudent(string password) =>
            S.Registration.RegisterStudent(Fill(new StudentRegistration
            {
                StudentNumber = StudentNumber.Text,
                StudyLevelId = int.TryParse(StudyLevel.SelectedValue, out var l) ? l : (int?)null,
                Faculty = Faculty.Text,
                Password = password,
                ConfirmPassword = password,
                AcceptedTerms = StudentTerms.Checked
            }), WebAuth.ClientIp(Context));

        private Services.Infrastructure.ServiceResult<int> CreateTutor(string password)
        {
            var subjects = new List<SubjectChoice>();
            foreach (RepeaterItem item in SubjectPicks.Items)
            {
                if (!((CheckBox)item.FindControl("Pick")).Checked) continue;
                subjects.Add(new SubjectChoice
                {
                    SubjectId = int.Parse(((HiddenField)item.FindControl("SubjectId")).Value, CultureInfo.InvariantCulture),
                    StudyLevelId = int.Parse(((DropDownList)item.FindControl("Level")).SelectedValue, CultureInfo.InvariantCulture)
                });
            }
            return S.Registration.RegisterTutor(Fill(new TutorRegistration
            {
                Qualification = Qualification.Text,
                ExperienceYears = int.TryParse(Experience.Text, out var y) ? y : -1,
                HourlyRate = decimal.TryParse(HourlyRate.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var r) ? r : -1,
                Bio = Bio.Text,
                Subjects = subjects,
                Password = password,
                ConfirmPassword = password,
                AcceptedTerms = TutorTerms.Checked
            }), WebAuth.ClientIp(Context));
        }
    }
}