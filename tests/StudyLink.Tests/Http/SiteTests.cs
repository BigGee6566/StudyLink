using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StudyLink.Tests.Http
{
    /// <summary>
    /// End-to-end HTTP tests against a running site with the demo data (IIS Express on port 51820 by
    /// default). They sign in as each demo account exactly like a browser (ViewState round-trip), so
    /// they exercise all three access-control layers: web.config folder rules, the page guard and the
    /// services. Skipped (inconclusive) when the site is not running.
    ///   STUDYLINK_BASEURL        default http://localhost:51820
    ///   STUDYLINK_DEMO_PASSWORD  default: Seed:DevPassword from src\StudyLink.Web\App_Data\local.settings.config
    /// </summary>
    [TestClass, TestCategory("Http")]
    public class SiteTests
    {
        private static readonly string BaseUrl = (Environment.GetEnvironmentVariable("STUDYLINK_BASEURL") ?? "http://localhost:51820").TrimEnd('/');

        private static readonly Dictionary<string, string[]> OwnPages = new Dictionary<string, string[]>
        {
            ["student.thando"] = new[]
            {
                "/Student/Dashboard.aspx", "/Catalogue/Books.aspx", "/Catalogue/Tutors.aspx", "/Student/Tutoring/MyBookings.aspx",
                "/Student/Tutoring/MyBookings.aspx?view=all", "/Student/Books/MyRentals.aspx", "/Student/Fees/MyFees.aspx",
                "/Student/Fees/PayFees.aspx", "/Account/Profile.aspx", "/Account/ChangePassword.aspx", "/Notifications/Index.aspx"
            },
            ["tutor.dlamini"] = new[]
            {
                "/Tutor/Dashboard.aspx", "/Tutor/Schedule.aspx", "/Tutor/Schedule.aspx?view=requests", "/Tutor/Schedule.aspx?view=past",
                "/Tutor/Availability.aspx", "/Tutor/Subjects.aspx", "/Tutor/Timesheet.aspx", "/Tutor/Earnings.aspx", "/Tutor/Reviews.aspx",
                "/Account/Profile.aspx"
            },
            ["tutor.pending"] = new[] { "/Tutor/Dashboard.aspx", "/Tutor/Availability.aspx", "/Tutor/Subjects.aspx", "/Account/Profile.aspx" },
            ["staff"] = new[]
            {
                "/Staff/Dashboard.aspx", "/Operations/TutorVerification.aspx", "/Operations/Users.aspx?role=Student", "/Operations/Users.aspx?new=1&role=Student",
                "/Operations/Returns.aspx", "/Operations/Returns.aspx?view=overdue", "/Operations/Fines.aspx", "/Operations/Support.aspx",
                "/Operations/Support.aspx?view=profile", "/Operations/PasswordResets.aspx", "/Operations/Timesheets.aspx",
                "/Operations/Deliveries.aspx", "/Operations/Reports.aspx", "/Operations/Reports.aspx?key=overdue", "/Account/Profile.aspx"
            },
            ["supplier"] = new[] { "/Supplier/Dashboard.aspx", "/Supplier/Orders.aspx", "/Supplier/Orders.aspx?view=all", "/Supplier/Deliveries.aspx", "/Supplier/Invoices.aspx", "/Supplier/Stock.aspx" },
            ["manager"] = new[]
            {
                "/Manager/Dashboard.aspx", "/Manager/Books.aspx", "/Manager/Books.aspx?view=inventory", "/Manager/BookEdit.aspx", "/Manager/Categories.aspx",
                "/Manager/Payments.aspx", "/Manager/SupplierOrders.aspx", "/Manager/SupplierOrders.aspx?tab=new", "/Manager/SupplierOrders.aspx?tab=invoices",
                "/Operations/Reports.aspx?key=revenue", "/Operations/Users.aspx?role=Staff", "/Operations/Subjects.aspx", "/Operations/Reviews.aspx",
                "/Operations/TutorVerification.aspx", "/Operations/Returns.aspx"
            },
            ["admin"] = new[]
            {
                "/Admin/Dashboard.aspx", "/Operations/Users.aspx", "/Operations/Users.aspx?new=1", "/Admin/Roles.aspx", "/Admin/Settings.aspx",
                "/Admin/AuditLog.aspx", "/Admin/Security.aspx", "/Admin/Backups.aspx", "/Operations/PasswordResets.aspx", "/Operations/Support.aspx",
                "/Operations/Subjects.aspx", "/Operations/Reviews.aspx"
            }
        };

        /// <summary>Pages each role must NOT reach (folder rule, page guard, or both).</summary>
        private static readonly Dictionary<string, string[]> ForbiddenPages = new Dictionary<string, string[]>
        {
            ["student.thando"] = new[] { "/Tutor/Dashboard.aspx", "/Staff/Dashboard.aspx", "/Operations/Users.aspx", "/Manager/Payments.aspx", "/Admin/AuditLog.aspx", "/Supplier/Orders.aspx" },
            ["tutor.dlamini"] = new[] { "/Student/Fees/MyFees.aspx", "/Operations/TutorVerification.aspx", "/Manager/Books.aspx", "/Admin/Settings.aspx" },
            ["staff"] = new[] { "/Manager/Payments.aspx", "/Admin/AuditLog.aspx", "/Operations/Subjects.aspx", "/Operations/Reviews.aspx", "/Student/Dashboard.aspx" },
            ["supplier"] = new[] { "/Operations/Returns.aspx", "/Manager/SupplierOrders.aspx", "/Student/Dashboard.aspx", "/Admin/Dashboard.aspx" },
            ["manager"] = new[] { "/Admin/Settings.aspx", "/Admin/AuditLog.aspx", "/Staff/Dashboard.aspx", "/Supplier/Orders.aspx" },
            ["admin"] = new[] { "/Operations/Returns.aspx", "/Operations/Fines.aspx", "/Manager/Payments.aspx", "/Student/Dashboard.aspx" }
        };

        private static readonly Dictionary<string, string> Homes = new Dictionary<string, string>
        {
            ["student.thando"] = "/Student/Dashboard.aspx",
            ["tutor.dlamini"] = "/Tutor/Dashboard.aspx",
            ["staff"] = "/Staff/Dashboard.aspx",
            ["supplier"] = "/Supplier/Dashboard.aspx",
            ["manager"] = "/Manager/Dashboard.aspx",
            ["admin"] = "/Admin/Dashboard.aspx"
        };

        [ClassInitialize]
        public static void RequireSite(TestContext _)
        {
            try
            {
                using (var c = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })
                    if (!c.GetAsync(BaseUrl + "/Default.aspx").Result.IsSuccessStatusCode)
                        Assert.Inconclusive("StudyLink is not answering at " + BaseUrl);
            }
            catch (AggregateException)
            {
                Assert.Inconclusive("StudyLink is not running at " + BaseUrl + " (start it with IIS Express to run the HTTP tests).");
            }
        }

        [TestMethod]
        public void Public_pages_load_for_visitors()
        {
            using (var c = Client())
                foreach (var url in new[] { "/Default.aspx", "/Catalogue/Books.aspx", "/Catalogue/Tutors.aspx", "/Public/HowItWorks.aspx", "/Public/Contact.aspx",
                                            "/Public/Privacy.aspx", "/Account/Login.aspx", "/Account/Register.aspx", "/Account/ForgotPassword.aspx" })
                    AssertOk(c, url);
        }

        [TestMethod]
        public void Visitors_are_sent_to_login_for_every_role_area()
        {
            using (var c = Client())
                foreach (var url in new[] { "/Student/Dashboard.aspx", "/Tutor/Dashboard.aspx", "/Staff/Dashboard.aspx", "/Supplier/Dashboard.aspx",
                                            "/Manager/Dashboard.aspx", "/Admin/Dashboard.aspx", "/Operations/Users.aspx", "/Notifications/Index.aspx",
                                            "/Account/Profile.aspx", "/Handlers/InvoiceFile.ashx?id=1" })
                {
                    var r = c.GetAsync(BaseUrl + url).Result;
                    Assert.AreEqual(HttpStatusCode.Found, r.StatusCode, url);
                    StringAssert.Contains(r.Headers.Location.ToString(), "Login.aspx", url);
                }
        }

        [TestMethod]
        public void Each_role_lands_on_its_own_dashboard()
        {
            foreach (var kv in Homes)
                using (var c = Client())
                {
                    var landed = SignIn(c, kv.Key);
                    StringAssert.EndsWith(landed, kv.Value, kv.Key + " landed on " + landed);
                }
        }

        [TestMethod]
        public void Each_role_can_open_all_of_its_pages()
        {
            var failures = new List<string>();
            foreach (var kv in OwnPages)
                using (var c = Client())
                {
                    SignIn(c, kv.Key);
                    foreach (var url in kv.Value)
                    {
                        var r = c.GetAsync(BaseUrl + url).Result;
                        if (r.StatusCode != HttpStatusCode.OK) failures.Add(kv.Key + " " + url + " -> " + (int)r.StatusCode + " " + r.Headers.Location);
                    }
                }
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        [TestMethod]
        public void Each_role_is_kept_out_of_other_roles_pages()
        {
            var leaks = new List<string>();
            foreach (var kv in ForbiddenPages)
                using (var c = Client())
                {
                    SignIn(c, kv.Key);
                    foreach (var url in kv.Value)
                    {
                        var r = c.GetAsync(BaseUrl + url).Result;
                        var location = r.Headers.Location?.ToString() ?? "";
                        var denied = (r.StatusCode == HttpStatusCode.Found && (location.Contains("Login.aspx") || location.Contains("AccessDenied.aspx")))
                                     || r.StatusCode == HttpStatusCode.Forbidden;
                        if (!denied) leaks.Add(kv.Key + " reached " + url + " (" + (int)r.StatusCode + ")");
                    }
                }
            Assert.AreEqual(0, leaks.Count, string.Join(Environment.NewLine, leaks));
        }

        /// <summary>Follows every in-app link a role can see (detail pages included) and fails on any server error.</summary>
        [TestMethod]
        public void Crawling_each_role_finds_no_server_errors()
        {
            var failures = new List<string>();
            foreach (var role in Homes.Keys)
                using (var c = Client())
                {
                    var queue = new Queue<string>(new[] { SignIn(c, role) }.Concat(OwnPages[role]));
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (queue.Count > 0 && seen.Count < 120)
                    {
                        var url = queue.Dequeue();
                        if (!seen.Add(Normalise(url))) continue;
                        var r = c.GetAsync(BaseUrl + url).Result;
                        if ((int)r.StatusCode >= 500) { failures.Add(role + " " + url + " -> " + (int)r.StatusCode); continue; }
                        if (r.StatusCode != HttpStatusCode.OK || !(r.Content.Headers.ContentType?.MediaType ?? "").Contains("html")) continue;
                        foreach (var link in Links(r.Content.ReadAsStringAsync().Result))
                            if (!seen.Contains(Normalise(link))) queue.Enqueue(link);
                    }
                }
            Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
        }

        [TestMethod]
        public void Locked_or_wrong_password_does_not_sign_in()
        {
            using (var c = Client())
            {
                var page = c.GetAsync(BaseUrl + "/Account/Login.aspx").Result.Content.ReadAsStringAsync().Result;
                var r = PostBack(c, "/Account/Login.aspx", page, "LoginButton",
                    new Dictionary<string, string> { ["Identifier"] = "student.thando", ["Password"] = "definitely-not-the-password-1" });
                Assert.AreEqual(HttpStatusCode.OK, r.StatusCode, "a failed sign-in re-shows the form");
                Assert.IsFalse(r.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(v => v.StartsWith(".STUDYLINKAUTH=") && !v.StartsWith(".STUDYLINKAUTH=;")),
                    "no auth cookie may be issued after a wrong password");
            }
        }

        /// <summary>
        /// Postbacks are bound to the session (ViewStateUserKey); this proves an honest one still
        /// validates on a fresh session (regression: an empty session changed SessionID every request).
        /// </summary>
        [TestMethod]
        public void A_signed_in_postback_is_accepted()
        {
            using (var c = Client())
            {
                SignIn(c, "student.thando");
                var page = c.GetAsync(BaseUrl + "/Account/Profile.aspx").Result.Content.ReadAsStringAsync().Result;
                // Re-submit the form unchanged.
                var r = PostBack(c, "/Account/Profile.aspx", page, "SaveContact", new Dictionary<string, string>());
                var body = r.Content.ReadAsStringAsync().Result;
                Assert.AreEqual(HttpStatusCode.OK, r.StatusCode, "an honest postback must not fail validation");
                StringAssert.Contains(body, "Your details were saved.");
            }
        }

        [TestMethod]
        public void A_tampered_postback_is_rejected()
        {
            using (var c = Client())
            {
                SignIn(c, "student.thando");
                var page = c.GetAsync(BaseUrl + "/Account/Profile.aspx").Result.Content.ReadAsStringAsync().Result;
                var forged = Regex.Replace(page, "id=\"__VIEWSTATE\" value=\"([^\"]{10})", m => m.Value.Substring(0, m.Value.Length - 4) + "AAAA");
                var r = PostBack(c, "/Account/Profile.aspx", forged, "SaveContact", new Dictionary<string, string>());
                Assert.IsTrue((int)r.StatusCode >= 400 || (r.Headers.Location?.ToString() ?? "").Contains("Error"),
                    "ViewState MAC must reject a modified postback (got " + (int)r.StatusCode + ")");
            }
        }

        // ------------------------------------------------------------------ helpers

        private static HttpClient Client() =>
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer(), UseCookies = true })
            { Timeout = TimeSpan.FromMinutes(3) };

        private static void AssertOk(HttpClient c, string url)
        {
            var r = c.GetAsync(BaseUrl + url).Result;
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode, url);
        }

        /// <summary>Signs in through the real login form; returns the dashboard path the site redirected to.</summary>
        private static string SignIn(HttpClient c, string userName)
        {
            var page = c.GetAsync(BaseUrl + "/Account/Login.aspx").Result.Content.ReadAsStringAsync().Result;
            var r = PostBack(c, "/Account/Login.aspx", page, "LoginButton",
                new Dictionary<string, string> { ["Identifier"] = userName, ["Password"] = DemoPassword() });
            Assert.AreEqual(HttpStatusCode.Found, r.StatusCode, "sign-in as " + userName + " should redirect");
            return new Uri(new Uri(BaseUrl), r.Headers.Location).AbsolutePath;
        }

        private static HttpResponseMessage PostBack(HttpClient c, string url, string page, string buttonId, IDictionary<string, string> fields)
        {
            var form = new List<KeyValuePair<string, string>>();
            foreach (Match m in Regex.Matches(page, "<input type=\"hidden\" name=\"(__[A-Z]+)\" id=\"\\1\" value=\"([^\"]*)\""))
                form.Add(new KeyValuePair<string, string>(m.Groups[1].Value, WebUtility.HtmlDecode(m.Groups[2].Value)));
            string NameOf(string id) => Regex.Match(page, "name=\"([^\"]*\\$" + id + ")\"").Groups[1].Value;
            var overridden = new HashSet<string>(fields.Keys.Select(NameOf));
            // Like a browser: send every text box and drop-down with its current value.
            foreach (Match m in Regex.Matches(page, "<input name=\"([^\"]+\\$[^\"]+)\" type=\"(?:text|email|number|date|time)\"(?:[^>]*?value=\"([^\"]*)\")?"))
                if (!overridden.Contains(m.Groups[1].Value))
                    form.Add(new KeyValuePair<string, string>(m.Groups[1].Value, WebUtility.HtmlDecode(m.Groups[2].Value)));
            foreach (Match m in Regex.Matches(page, "<select name=\"([^\"]+)\"[^>]*>(.*?)</select>", RegexOptions.Singleline))
            {
                var selected = Regex.Match(m.Groups[2].Value, "<option selected=\"selected\" value=\"([^\"]*)\"");
                var first = Regex.Match(m.Groups[2].Value, "<option[^>]*value=\"([^\"]*)\"");
                if (!overridden.Contains(m.Groups[1].Value))
                    form.Add(new KeyValuePair<string, string>(m.Groups[1].Value, WebUtility.HtmlDecode((selected.Success ? selected : first).Groups[1].Value)));
            }
            foreach (var f in fields) form.Add(new KeyValuePair<string, string>(NameOf(f.Key), f.Value));
            var button = NameOf(buttonId);
            form.Add(new KeyValuePair<string, string>(button, "submit"));
            return c.PostAsync(BaseUrl + url, new FormUrlEncodedContent(form)).Result;
        }

        private static IEnumerable<string> Links(string html)
        {
            foreach (Match m in Regex.Matches(html, "href=\"(/[^\"#]*\\.(aspx|ashx)[^\"#]*)\""))
            {
                var link = WebUtility.HtmlDecode(m.Groups[1].Value);
                // Skip side-effect-free-but-noisy targets (downloads) and the sign-out postback.
                if (link.Contains("export=csv") || link.Contains("InvoiceFile.ashx") || link.Contains("BookCover.ashx")) continue;
                yield return link;
            }
        }

        private static string Normalise(string url) => Regex.Replace(url, "[?&]page=\\d+", "");

        private static string DemoPassword([CallerFilePath] string thisFile = "")
        {
            var fromEnv = Environment.GetEnvironmentVariable("STUDYLINK_DEMO_PASSWORD");
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
            var config = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile), @"..\..\..\src\StudyLink.Web\App_Data\local.settings.config"));
            if (!File.Exists(config)) Assert.Inconclusive("Set STUDYLINK_DEMO_PASSWORD or create " + config);
            return XDocument.Load(config).Root.Elements("add").First(e => (string)e.Attribute("key") == "Seed:DevPassword").Attribute("value").Value;
        }
    }
}
