using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>Small rendering and form helpers shared by the pages (drop-downs, KPI cards, pagers).</summary>
    public static class Ui
    {
        private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

        public static void Fill(ListControl list, IEnumerable<LookupItem> items, string emptyText = null, int? selected = null)
        {
            list.Items.Clear();
            if (emptyText != null) list.Items.Add(new ListItem(emptyText, ""));
            foreach (var item in items) list.Items.Add(new ListItem(item.Name, item.Id.ToString(CultureInfo.InvariantCulture)));
            if (selected.HasValue) Select(list, selected.Value.ToString(CultureInfo.InvariantCulture));
        }

        public static void FillEnum<T>(ListControl list, string emptyText = null, IEnumerable<T> only = null) where T : struct
        {
            list.Items.Clear();
            if (emptyText != null) list.Items.Add(new ListItem(emptyText, ""));
            foreach (var value in only ?? Enum.GetValues(typeof(T)).Cast<T>())
                list.Items.Add(new ListItem(Fmt.Words(value), Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture)));
        }

        public static void FillText(ListControl list, IEnumerable<string> values, string emptyText = null, Func<string, string> label = null)
        {
            list.Items.Clear();
            if (emptyText != null) list.Items.Add(new ListItem(emptyText, ""));
            foreach (var v in values) list.Items.Add(new ListItem(label == null ? v : label(v), v));
        }

        public static void Select(ListControl list, string value)
        {
            var item = list.Items.FindByValue(value ?? "");
            if (item == null) return;
            list.ClearSelection();
            item.Selected = true;
        }

        public static int? Int(ListControl list) => int.TryParse(list.SelectedValue, out var v) ? v : (int?)null;

        public static T? EnumOf<T>(ListControl list) where T : struct =>
            int.TryParse(list.SelectedValue, out var v) && System.Enum.IsDefined(typeof(T), v) ? (T)System.Enum.ToObject(typeof(T), v) : (T?)null;

        public static T? EnumOf<T>(string value) where T : struct =>
            int.TryParse(value, out var v) && System.Enum.IsDefined(typeof(T), v) ? (T)System.Enum.ToObject(typeof(T), v) :
            System.Enum.TryParse<T>(value, true, out var named) ? named : (T?)null;

        /// <summary>Parses money typed as "1 234,50", "1234.50" or "R 1,234.50".</summary>
        public static decimal? Money(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var s = text.Replace("R", "").Replace(" ", "").Replace(" ", "").Trim();
            if (s.Count(c => c == ',') == 1 && !s.Contains(".")) s = s.Replace(',', '.');
            else s = s.Replace(",", "");
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null;
        }

        public static int? Int(string text) => int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;

        /// <summary>HTML date inputs post yyyy-MM-dd.</summary>
        public static DateTime? Date(string text) =>
            DateTime.TryParseExact((text ?? "").Trim(), new[] { "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

        public static TimeSpan? Time(string text) =>
            TimeSpan.TryParseExact((text ?? "").Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out var t) ? t : (TimeSpan?)null;

        public static string IsoDate(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static string Kpis(Control resolver, IEnumerable<Kpi> kpis)
        {
            var sb = new StringBuilder("<div class=\"kpi-grid\">");
            foreach (var k in kpis)
            {
                var tone = string.IsNullOrEmpty(k.Tone) || k.Tone == "blue" ? "" : " kpi--" + k.Tone;
                var tag = string.IsNullOrEmpty(k.Link) ? "div" : "a";
                sb.Append('<').Append(tag).Append(" class=\"kpi").Append(tone).Append('"');
                if (tag == "a") sb.Append(" href=\"").Append(HttpUtility.HtmlAttributeEncode(resolver.ResolveUrl(k.Link))).Append('"');
                sb.Append("><div class=\"kpi__value\">").Append(Fmt.H(k.Value)).Append("</div><div class=\"kpi__label\">")
                  .Append(Fmt.H(k.Label)).Append("</div>");
                if (!string.IsNullOrEmpty(k.Hint)) sb.Append("<div class=\"kpi__hint\">").Append(Fmt.H(k.Hint)).Append("</div>");
                sb.Append("</").Append(tag).Append('>');
            }
            return sb.Append("</div>").ToString();
        }

        public static string Stars(decimal? rating, int count = -1)
        {
            if (!rating.HasValue) return "<span class=\"muted\">No reviews yet</span>";
            var full = (int)Math.Round(rating.Value, MidpointRounding.AwayFromZero);
            var label = rating.Value.ToString("0.0", Gb) + " out of 5";
            return "<span class=\"stars\" aria-label=\"" + label + "\">" + new string('★', full) + new string('☆', 5 - full) + "</span> " +
                   "<span class=\"muted\">" + rating.Value.ToString("0.0", Gb) + (count >= 0 ? " (" + count + ")" : "") + "</span>";
        }

        public static string Stars(byte rating) => Stars((decimal)rating);

        public static string Empty(string icon, string message, string actionHtml = null) =>
            "<div class=\"empty-state\">" + Icons.Svg(icon, 32) + "<p class=\"mb-2\">" + Fmt.H(message) + "</p>" + (actionHtml ?? "") + "</div>";

        /// <summary>Previous/next pager that keeps the current query string (filters) intact.</summary>
        public static string Pager<T>(HttpRequest request, PagedResult<T> result)
        {
            if (result.PageCount <= 1) return "";
            string Link(int page, string text, bool enabled)
            {
                if (!enabled) return "<span class=\"btn btn-sm btn-outline-secondary disabled\" aria-disabled=\"true\">" + text + "</span>";
                var q = HttpUtility.ParseQueryString(request.QueryString.ToString());
                q["page"] = page.ToString(CultureInfo.InvariantCulture);
                return "<a class=\"btn btn-sm btn-outline-primary\" href=\"" + HttpUtility.HtmlAttributeEncode(request.Path + "?" + q) + "\">" + text + "</a>";
            }
            return "<nav class=\"pager\" aria-label=\"Pages\"><span class=\"muted small me-2\">Page " + result.Page + " of " + result.PageCount +
                   " · " + result.Total + " total</span>" + Link(result.Page - 1, "&larr; Previous", result.Page > 1) +
                   Link(result.Page + 1, "Next &rarr;", result.Page < result.PageCount) + "</nav>";
        }

        public static string WithQuery(HttpRequest request, string key, string value)
        {
            NameValueCollection q = HttpUtility.ParseQueryString(request.QueryString.ToString());
            if (string.IsNullOrEmpty(value)) q.Remove(key); else q[key] = value;
            q.Remove("page");
            var qs = q.ToString();
            return request.Path + (qs.Length > 0 ? "?" + qs : "");
        }

        /// <summary>Tab strip whose links switch a query-string value (view=…, status=…).</summary>
        public static string Tabs(HttpRequest request, string key, params (string Value, string Text)[] tabs)
        {
            var current = request.QueryString[key] ?? "";
            var sb = new StringBuilder("<nav class=\"tabs\">");
            foreach (var t in tabs)
            {
                sb.Append("<a href=\"").Append(HttpUtility.HtmlAttributeEncode(WithQuery(request, key, t.Value))).Append('"');
                if (string.Equals(current, t.Value ?? "", StringComparison.OrdinalIgnoreCase)) sb.Append(" aria-current=\"page\"");
                sb.Append('>').Append(Fmt.H(t.Text)).Append("</a>");
            }
            return sb.Append("</nav>").ToString();
        }

        /// <summary>Simple CSS bar chart for trend data (no chart library, works in print).</summary>
        public static string BarChart(IList<TrendPoint> points, Func<decimal, string> format)
        {
            if (points == null || points.Count == 0) return Empty("chart", "No data for this period yet.");
            var max = Math.Max(points.Max(p => p.Value), 1m);
            var bars = new StringBuilder("<div class=\"bar-chart\" role=\"img\" aria-label=\"Trend chart\">");
            var labels = new StringBuilder("<div class=\"bar-chart__labels\">");
            foreach (var p in points)
            {
                var pct = (int)Math.Round(p.Value / max * 100m);
                bars.Append("<div class=\"bar-chart__bar\" style=\"height:").Append(Math.Max(pct, 2)).Append("%\" title=\"")
                    .Append(Fmt.H(Fmt.Day(p.Date) + ": " + format(p.Value))).Append("\"></div>");
                labels.Append("<span>").Append(p.Date.ToString("d/M", Gb)).Append("</span>");
            }
            return bars.Append("</div>").ToString() + labels.Append("</div>");
        }

        public static string Initials(string name) =>
            string.Concat((name ?? "?").Split(' ').Where(p => p.Length > 0).Take(2).Select(p => char.ToUpperInvariant(p[0])));

        public static string Bytes(long? bytes)
        {
            if (!bytes.HasValue) return "–";
            var b = (double)bytes.Value;
            return b > 1 << 30 ? (b / (1 << 30)).ToString("0.0", Gb) + " GB" : b > 1 << 20 ? (b / (1 << 20)).ToString("0.0", Gb) + " MB" : (b / 1024).ToString("0", Gb) + " KB";
        }
    }
}
