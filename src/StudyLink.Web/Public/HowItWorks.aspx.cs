using System;
using StudyLink.Data.Seed;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Public
{
    /// <summary>Public explanation of the student and tutor journeys, quoting the live settings.</summary>
    public partial class HowItWorks : CataloguePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "From registering to paying your fees, step by step.";
            Period.Text = S.Settings.GetInt(SettingKeys.RentalPeriodDays, 7).ToString();
            Cutoff.Text = S.Settings.GetInt(SettingKeys.CancellationCutoffHours, 24).ToString();
            Hold.Text = S.Settings.GetInt(SettingKeys.BookingHoldMinutes, 30).ToString();
        }
    }
}