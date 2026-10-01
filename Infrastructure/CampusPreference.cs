using System;
using System.Linq;
using System.Web;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>
    /// The campus a visitor chose on the landing page (Alice / East London / Bisho). A preference
    /// only: it pre-filters book stock and tutors. Signed-in users default to their own campus.
    /// </summary>
    public static class CampusPreference
    {
        private const string Cookie = "StudyLink.Campus";

        public static int? Get(HttpContext ctx)
        {
            if (int.TryParse(ctx.Request.Cookies[Cookie]?.Value, out var id)) return id;
            var user = WebAuth.Current(ctx);
            if (user == null) return null;
            return AppServices.Current.Db.Users.Where(u => u.UserId == user.UserId).Select(u => (int?)u.CampusId).FirstOrDefault();
        }

        public static void Set(HttpContext ctx, int campusId)
        {
            ctx.Response.Cookies.Add(new HttpCookie(Cookie, campusId.ToString())
            {
                Expires = DateTime.Now.AddDays(180), HttpOnly = true, SameSite = SameSiteMode.Lax,
                Secure = ctx.Request.IsSecureConnection
            });
        }
    }
}