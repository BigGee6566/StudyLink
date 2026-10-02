using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using StudyLink.Data.Entities;
using StudyLink.Data.Seed;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Account
{
    /// <summary>Every role's profile: own contact details, tutor details, student subjects, and change requests.</summary>
    [RequireSignIn]
    public partial class Profile : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Your details and account settings.";
            if (!IsPostBack) Bind(true);
        }

        private void Bind(bool fillForms)
        {
            var u = S.Profiles.Get(CurrentUser.UserId);
            Heading = u.FullName;
            FullName.Text = H(u.FullName);
            UserName.Text = H(u.UserName);
            Roles.Text = H(string.Join(", ", u.Roles.Select(RoleNames.DisplayName)));
            Since.Text = Fmt.Date(u.CreatedAt);

            var photo = S.Profiles.PhotoVersion(u.UserId);
            PhotoPreview.Text = Ui.Avatar(u.UserId, photo, "avatar--lg");
            RemovePhotoButton.Visible = photo.HasValue;
            PhotoHint.Text = u.Roles.Contains(RoleNames.Tutor) && !photo.HasValue
                ? "<p class=\"mb-2\">Students choose tutors they can recognise. Add a professional, friendly headshot.</p>" : "";

            var facts = new StringBuilder();
            void Fact(string label, string value) { if (!string.IsNullOrEmpty(value)) facts.Append("<dt>").Append(label).Append("</dt><dd>").Append(H(value)).Append("</dd>"); }
            Fact("Student number", u.StudentNumber);
            Fact("Faculty", u.Faculty);
            Fact("Study level", u.StudyLevel);
            Fact("Employee number", u.EmployeeNumber);
            Fact("Job title", u.JobTitle);
            Fact("Company", u.CompanyName);
            Fact("VAT number", u.VatNumber);
            RoleFacts.Text = facts.ToString();

            if (fillForms)
            {
                Email.Text = u.Email;
                Cell.Text = u.CellNumber;
                Ui.Fill(Campus, S.Lookups.Campuses(), null, u.CampusId);
                Ui.FillEnum<Gender>(Gender);
                Ui.Select(Gender, ((int)u.Gender).ToString(CultureInfo.InvariantCulture));
                Ui.FillText(Field, S.Profiles.RequestableFields(u.UserId), null, Fmt.Words);

                if (u.TutorId.HasValue)
                {
                    Qualification.Text = u.Qualification;
                    Experience.Text = u.ExperienceYears.ToString(CultureInfo.InvariantCulture);
                    Rate.Text = u.HourlyRate.ToString("0.00", CultureInfo.InvariantCulture);
                    Bio.Text = u.Bio;
                }
                if (u.StudentId.HasValue)
                {
                    Ui.Fill(AddSubject, S.Lookups.Subjects());
                    Ui.Fill(AddLevel, S.Lookups.StudyLevels(), null, u.StudyLevelId);
                }
            }

            TutorPanel.Visible = u.TutorId.HasValue;
            TutorStatus.Text = u.TutorStatus.HasValue ? Fmt.Badge(u.TutorStatus.Value) : "";
            StudentPanel.Visible = u.StudentId.HasValue;
            if (u.StudentId.HasValue)
            {
                StudentSubjects.DataSource = S.Subjects.StudentSubjects(Actor);
                StudentSubjects.DataBind();
            }
            Requests.DataSource = S.Profiles.MyRequests(u.UserId).Take(5);
            Requests.DataBind();
        }

        protected void UploadPhoto_Click(object sender, EventArgs e)
        {
            if (!Photo.HasFile)
            {
                Fail("Choose a photo to upload.");
                return;
            }
            byte[] image;
            using (var ms = new MemoryStream())
            {
                Photo.PostedFile.InputStream.CopyTo(ms);
                image = ms.ToArray();
            }
            // Redirect so the sidebar avatar (drawn by the master page) shows the new photo at once.
            if (Show(S.Profiles.SetPhoto(Actor, image))) RedirectWith("~/Account/Profile.aspx", "Your profile photo was updated.");
        }

        protected void RemovePhoto_Click(object sender, EventArgs e)
        {
            if (Show(S.Profiles.RemovePhoto(Actor))) RedirectWith("~/Account/Profile.aspx", "Your profile photo was removed.");
        }

        protected void SaveContact_Click(object sender, EventArgs e)
        {
            var gender = Ui.EnumOf<Gender>(Gender) ?? Data.Entities.Gender.PreferNotToSay;
            Show(S.Profiles.UpdateContact(Actor, Email.Text, Cell.Text, Ui.Int(Campus) ?? 0, gender), "Your details were saved.");
            Bind(false);
        }

        protected void SaveTutor_Click(object sender, EventArgs e)
        {
            var rate = Ui.Money(Rate.Text);
            var years = Ui.Int(Experience.Text);
            if (!rate.HasValue || !years.HasValue) Fail("Enter your rate and years of experience as numbers.");
            else Show(S.Profiles.UpdateTutorDetails(Actor, Qualification.Text, years.Value, rate.Value, Bio.Text), "Your tutor profile was saved.");
            Bind(false);
        }

        protected void Request_Click(object sender, EventArgs e)
        {
            if (Show(S.Profiles.RequestChange(Actor, Field.SelectedValue, NewValue.Text, Reason.Text), "Your request was sent to the hub."))
                NewValue.Text = Reason.Text = "";
            Bind(false);
        }

        protected void AddSubject_Click(object sender, EventArgs e)
        {
            Show(S.Subjects.AddStudentSubject(Actor, Ui.Int(AddSubject) ?? 0, Ui.Int(AddLevel) ?? 0), "Subject added.");
            Bind(false);
        }

        protected void StudentSubject_Command(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "remove" && int.TryParse((string)e.CommandArgument, out var id))
                Show(S.Subjects.RemoveStudentSubject(Actor, id), "Subject removed.");
            Bind(false);
        }
    }
}