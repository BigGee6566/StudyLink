using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>The signed-in user as seen by pages: id, display name and current roles.</summary>
    public sealed class StudyLinkPrincipal : GenericPrincipal
    {
        public StudyLinkPrincipal(IIdentity identity, int userId, string displayName, string[] roles)
            : base(identity, roles)
        {
            UserId = userId;
            DisplayName = displayName;
            Roles = roles;
        }

        public int UserId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> Roles { get; }
        public string Initials => string.Concat((DisplayName ?? "?").Split(' ').Where(p => p.Length > 0).Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }

    /// <summary>
    /// Forms authentication for StudyLink (ported from TutorConnect RoleHelper). The ticket carries
    /// only the user id; roles and the active flag are re-read from the database on each page
    /// request, so a deactivation (BR-25) or a role removal takes effect immediately rather than
    /// when the ticket expires.
    /// </summary>
    public static class WebAuth
    {
        private const string PrincipalKey = "StudyLink.Principal";

        public static void SignIn(HttpContext ctx, SignInResult result, bool remember)
        {
            // SEC-07: a new session id at sign-in, so a planted session id is worthless.
            ctx.Session?.Clear();
            ctx.Session?.Abandon();
            ctx.Response.Cookies.Add(new HttpCookie("StudyLink.Session", string.Empty) { Expires = DateTime.Now.AddDays(-1), HttpOnly = true });

            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(2, result.UserName, now,
                remember ? now.AddDays(7) : now.Add(FormsAuthentication.Timeout), remember,
                result.UserId.ToString(), FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL || ctx.Request.IsSecureConnection,
                Path = FormsAuthentication.FormsCookiePath,
                SameSite = SameSiteMode.Lax
            };
            if (remember) cookie.Expires = ticket.Expiration;
            ctx.Response.Cookies.Add(cookie);
        }

        public static void SignOut(HttpContext ctx)
        {
            FormsAuthentication.SignOut();
            ctx.Session?.Clear();
            ctx.Session?.Abandon();
            ctx.Response.Cookies.Add(new HttpCookie("StudyLink.Session", string.Empty) { Expires = DateTime.Now.AddDays(-1), HttpOnly = true });
        }

        public static void AttachPrincipal(HttpContext ctx)
        {
            if (!(ctx.User?.Identity is FormsIdentity identity) || !identity.IsAuthenticated)
                return;
            // Static files do not need a database round trip.
            var ext = ctx.Request.CurrentExecutionFilePathExtension;
            if (!string.IsNullOrEmpty(ext) && ext != ".aspx" && ext != ".ashx")
                return;

            if (!int.TryParse(identity.Ticket.UserData, out var userId))
            {
                FormsAuthentication.SignOut();
                return;
            }

            var services = AppServices.Current;
            var user = services.Db.Users.Where(u => u.UserId == userId)
                .Select(u => new { u.IsActive, u.FirstName, u.LastName, Roles = u.UserRoles.Select(r => r.Role.Name) })
                .FirstOrDefault();
            if (user == null || !user.IsActive)
            {
                // BR-25: deactivated while signed in → out on the next request.
                FormsAuthentication.SignOut();
                ctx.User = new GenericPrincipal(new GenericIdentity(string.Empty), new string[0]);
                if (ctx.Request.CurrentExecutionFilePath.IndexOf("/Account/", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    ctx.Response.Redirect("~/Account/Login.aspx?deactivated=1", false);
                    ctx.ApplicationInstance.CompleteRequest();
                }
                return;
            }

            var principal = new StudyLinkPrincipal(identity, userId, user.FirstName + " " + user.LastName, user.Roles.ToArray());
            ctx.User = principal;
            ctx.Items[PrincipalKey] = principal;
        }

        public static StudyLinkPrincipal Current(HttpContext ctx) => ctx?.User as StudyLinkPrincipal;

        /// <summary>The Actor passed to every service call, or null for an anonymous visitor.</summary>
        public static Actor ActorFor(HttpContext ctx)
        {
            var p = Current(ctx);
            return p == null ? null : new Actor(p.UserId, p.Identity.Name, p.Roles, ClientIp(ctx));
        }

        public static string ClientIp(HttpContext ctx) => ctx?.Request.UserHostAddress ?? "unknown";

        /// <summary>Where each role lands after sign-in (the most privileged role wins).</summary>
        public static string HomeUrl(IEnumerable<string> roles)
        {
            var set = new HashSet<string>(roles ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (set.Contains(RoleNames.SystemAdmin)) return "~/Admin/Dashboard.aspx";
            if (set.Contains(RoleNames.Manager)) return "~/Manager/Dashboard.aspx";
            if (set.Contains(RoleNames.Staff)) return "~/Staff/Dashboard.aspx";
            if (set.Contains(RoleNames.Supplier)) return "~/Supplier/Dashboard.aspx";
            if (set.Contains(RoleNames.Tutor)) return "~/Tutor/Dashboard.aspx";
            if (set.Contains(RoleNames.Student)) return "~/Student/Dashboard.aspx";
            return "~/Default.aspx";
        }

        /// <summary>Only same-site relative paths are followed after sign-in (open-redirect guard).</summary>
        public static bool IsLocalUrl(string url) =>
            !string.IsNullOrEmpty(url) && url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\");
    }
}