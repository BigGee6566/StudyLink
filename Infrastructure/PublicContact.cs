using System;
using System.Collections.Generic;
using StudyLink.Data.Seed;
using StudyLink.Services.Interfaces;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// The hub's public contact details and social links, as the System Admin set them in System
    /// Settings. Shown in the public footer and on the Contact page.
    /// </summary>
    public sealed class PublicContact
    {
        public string Phone { get; private set; }
        public string Email { get; private set; }
        public string WhatsApp { get; private set; }
        public string Hours { get; private set; }
        public IList<KeyValuePair<string, string>> Campuses { get; private set; }

        /// <summary>(network name, icon, link). The link is "#" until a real profile URL is set.</summary>
        public IList<Tuple<string, string, string>> Social { get; private set; }

        public static PublicContact Load(ISettingsService settings)
        {
            Tuple<string, string, string> Network(string name, string icon, string key) =>
                Tuple.Create(name, icon, SafeUrl(settings.GetString(key, "")) ?? "#");

            return new PublicContact
            {
                Phone = settings.GetString(SettingKeys.ContactPhone, ""),
                Email = settings.GetString(SettingKeys.ContactEmail, ""),
                WhatsApp = settings.GetString(SettingKeys.ContactWhatsApp, ""),
                Hours = settings.GetString(SettingKeys.ContactHours, ""),
                Campuses = new[]
                {
                    new KeyValuePair<string, string>("Alice", settings.GetString(SettingKeys.AddressAlice, "")),
                    new KeyValuePair<string, string>("East London", settings.GetString(SettingKeys.AddressEastLondon, "")),
                    new KeyValuePair<string, string>("Bisho", settings.GetString(SettingKeys.AddressBisho, ""))
                },
                Social = new[]
                {
                    Network("Facebook", "facebook", SettingKeys.SocialFacebook),
                    Network("Instagram", "instagram", SettingKeys.SocialInstagram),
                    Network("X (Twitter)", "x", SettingKeys.SocialX),
                    Network("LinkedIn", "linkedin", SettingKeys.SocialLinkedIn),
                    Network("YouTube", "youtube", SettingKeys.SocialYouTube)
                }
            };
        }

        /// <summary>Only absolute http(s) links reach an href, so a settings value can never be a script URL.</summary>
        public static string SafeUrl(string url) =>
            Uri.TryCreate((url ?? "").Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp)
                ? u.AbsoluteUri : null;

        /// <summary>An email setting is shown as a mailto link only when it looks like an address.</summary>
        public bool EmailIsAddress => Email != null && Email.Contains("@") && !Email.Contains(" ");
    }
}
