using System.Collections.Generic;
using System.Linq;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>Inline SVG icons (24×24 line icons drawn in currentColor). No icon font, no CDN.</summary>
    public static class Icons
    {
        private static readonly Dictionary<string, string> Paths = new Dictionary<string, string>
        {
            ["home"] = "<path d='M3 11l9-7 9 7'/><path d='M5 10v10h5v-6h4v6h5V10'/>",
            ["book"] = "<path d='M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2z'/><path d='M4 21V5'/><path d='M8 7h8'/>",
            ["cap"] = "<path d='M2 9l10-5 10 5-10 5z'/><path d='M6 11v5c0 1.5 3 3 6 3s6-1.5 6-3v-5'/><path d='M22 9v6'/>",
            ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M16 3v4M8 3v4M3 10h18'/>",
            ["library"] = "<path d='M4 4h4v16H4zM10 4h4v16h-4z'/><path d='M16.5 5.5l3.5-1 3 15-3.5 1z'/>",
            ["card"] = "<rect x='2' y='5' width='20' height='14' rx='2'/><path d='M2 10h20M6 15h4'/>",
            ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21c0-4 4-6 8-6s8 2 8 6'/>",
            ["users"] = "<circle cx='9' cy='8' r='3.5'/><path d='M2 20c0-3.5 3-5.5 7-5.5s7 2 7 5.5'/><path d='M16 4.5a3.5 3.5 0 0 1 0 7M18 14.8c2.4.6 4 2.4 4 5.2'/>",
            ["clock"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l3 2'/>",
            ["list"] = "<path d='M9 6h12M9 12h12M9 18h12'/><circle cx='4' cy='6' r='1'/><circle cx='4' cy='12' r='1'/><circle cx='4' cy='18' r='1'/>",
            ["check"] = "<path d='M4 12l5 5L20 6'/>",
            ["sheet"] = "<rect x='5' y='3' width='14' height='18' rx='2'/><path d='M9 3v2h6V3M9 10h6M9 14h6M9 18h3'/>",
            ["coins"] = "<ellipse cx='9' cy='7' rx='6' ry='3'/><path d='M3 7v5c0 1.7 2.7 3 6 3s6-1.3 6-3V7'/><path d='M9 15v2c0 1.7 2.7 3 6 3s6-1.3 6-3v-5c0-1.4-1.9-2.6-4.6-2.9'/>",
            ["shield"] = "<path d='M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z'/><path d='M9 12l2 2 4-4'/>",
            ["undo"] = "<path d='M9 14L4 9l5-5'/><path d='M4 9h11a5 5 0 0 1 0 10h-3'/>",
            ["alert"] = "<path d='M12 3l10 18H2z'/><path d='M12 10v4M12 17.5v.5'/>",
            ["support"] = "<circle cx='12' cy='12' r='9'/><path d='M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .8-1 1.5v.7M12 17v.5'/>",
            ["truck"] = "<path d='M2 6h11v10H2zM13 10h4l4 4v2h-8'/><circle cx='6' cy='18' r='2'/><circle cx='17' cy='18' r='2'/>",
            ["box"] = "<path d='M3 7l9-4 9 4v10l-9 4-9-4z'/><path d='M3 7l9 4 9-4M12 11v10'/>",
            ["invoice"] = "<path d='M6 3h9l4 4v14H6z'/><path d='M15 3v4h4M9 12h7M9 16h7'/>",
            ["chart"] = "<path d='M4 20V4M4 20h16'/><path d='M8 16v-5M12 16V8M16 16v-3'/>",
            ["key"] = "<circle cx='8' cy='14' r='4'/><path d='M11 11l9-9M16 6l2 2M14 8l2 2'/>",
            ["settings"] = "<circle cx='12' cy='12' r='3'/><path d='M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1'/>",
            ["audit"] = "<path d='M5 3h10l4 4v14H5z'/><circle cx='11' cy='13' r='3'/><path d='M13.2 15.2L16 18'/>",
            ["lock"] = "<rect x='4' y='11' width='16' height='10' rx='2'/><path d='M8 11V7a4 4 0 0 1 8 0v4'/>",
            ["database"] = "<ellipse cx='12' cy='5' rx='8' ry='3'/><path d='M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3'/>",
            ["tag"] = "<path d='M3 3h8l10 10-8 8L3 11z'/><circle cx='7.5' cy='7.5' r='1.5'/>",
            ["star"] = "<path d='M12 3l2.8 5.8 6.2.9-4.5 4.4 1 6.2L12 17.4 6.5 20.3l1-6.2L3 9.7l6.2-.9z'/>",
            ["bell"] = "<path d='M6 16V11a6 6 0 0 1 12 0v5l2 2H4z'/><path d='M10 20a2 2 0 0 0 4 0'/>",
            ["logout"] = "<path d='M15 4h4v16h-4M10 17l5-5-5-5M15 12H3'/>",
            ["menu"] = "<path d='M3 6h18M3 12h18M3 18h18'/>",
            ["search"] = "<circle cx='11' cy='11' r='7'/><path d='M20 20l-4-4'/>",
            ["arrow"] = "<path d='M5 12h14M13 6l6 6-6 6'/>",
            ["plus"] = "<path d='M12 5v14M5 12h14'/>",
            ["pin"] = "<path d='M12 21s7-6.5 7-12a7 7 0 0 0-14 0c0 5.5 7 12 7 12z'/><circle cx='12' cy='9' r='2.5'/>",
            ["mail"] = "<rect x='3' y='5' width='18' height='14' rx='2'/><path d='M3 7l9 6 9-6'/>",
            ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 11v6M12 7.5v.5'/>",
            ["phone"] = "<path d='M5 3h4l2 5-2.5 1.5a11 11 0 0 0 6 6L16 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 5a2 2 0 0 1 2-2z'/>",

            // Subjects
            ["calculator"] = "<rect x='5' y='2.5' width='14' height='19' rx='2'/><path d='M8 6.5h8v3H8z'/><path d='M8.5 13h.01M12 13h.01M15.5 13h.01M8.5 16.5h.01M12 16.5h.01M15.5 16.5h.01'/>",
            ["flask"] = "<path d='M9 3h6M10 3v6L4.5 18.5A1.7 1.7 0 0 0 6 21h12a1.7 1.7 0 0 0 1.5-2.5L14 9V3'/><path d='M7 15h10'/>",
            ["scale"] = "<path d='M12 3v18M7 21h10M5 7h14M5 7l-3 7a3 3 0 0 0 6 0zM19 7l-3 7a3 3 0 0 0 6 0z'/>",
            ["briefcase"] = "<rect x='3' y='7' width='18' height='13' rx='2'/><path d='M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2M3 13h18'/>",
            ["laptop"] = "<rect x='4' y='5' width='16' height='11' rx='1.5'/><path d='M2 19h20'/>",
            ["code"] = "<path d='M8 8l-4 4 4 4M16 8l4 4-4 4M13.5 5l-3 14'/>",
            ["brain"] = "<path d='M9 4a3 3 0 0 0-3 3 3 3 0 0 0-2 5 3 3 0 0 0 2 5 3 3 0 0 0 6 1V5a2 2 0 0 0-3-1z'/><path d='M15 4a3 3 0 0 1 3 3 3 3 0 0 1 2 5 3 3 0 0 1-2 5 3 3 0 0 1-6 1'/>",
            ["ledger"] = "<path d='M5 3h12a2 2 0 0 1 2 2v16H7a2 2 0 0 1-2-2z'/><path d='M9 8h6M9 12h6M9 16h3'/>",
            ["trend"] = "<path d='M3 17l6-6 4 4 8-8'/><path d='M15 7h6v6'/>",
            ["book-open"] = "<path d='M12 6c-2-1.5-5-2-8-1.5V19c3-.5 6 0 8 1.5 2-1.5 5-2 8-1.5V4.5c-3-.5-6 0-8 1.5zM12 6v14.5'/>",

            // Social (outline glyphs, same stroke style as the rest of the set)
            ["facebook"] = "<path d='M14 8h3V4h-3a4 4 0 0 0-4 4v3H7v4h3v6h4v-6h3l1-4h-4V8z'/>",
            ["instagram"] = "<rect x='3' y='3' width='18' height='18' rx='5'/><circle cx='12' cy='12' r='4'/><path d='M17.5 6.5h.01'/>",
            ["x"] = "<path d='M4 4l16 16M20 4L4 20'/>",
            ["linkedin"] = "<rect x='3' y='3' width='18' height='18' rx='3'/><path d='M8 10v7M8 7v.01M12 17v-4a2 2 0 0 1 4 0v4M12 10v7'/>",
            ["youtube"] = "<rect x='2.5' y='5.5' width='19' height='13' rx='4'/><path d='M10 9.5v5l4.5-2.5z'/>",
            ["whatsapp"] = "<path d='M4 20l1.3-3.8A8.5 8.5 0 1 1 8 19z'/><path d='M9 9.2c0 3 2.8 5.8 5.8 5.8l1.2-1.4-1.9-1-.9.7a4.6 4.6 0 0 1-2.5-2.5l.7-.9-1-1.9z'/>"
        };

        public static string Svg(string name, int size = 20)
        {
            Paths.TryGetValue(name ?? "", out var path);
            return "<svg class=\"icon\" width=\"" + size + "\" height=\"" + size + "\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">" + (path ?? Paths["info"]).Replace('\'', '"') + "</svg>";
        }

        /// <summary>Subject name → (icon, tint). Unknown subjects get a plain book in navy.</summary>
        private static readonly (string Match, string Icon, string Tone)[] SubjectIcons =
        {
            ("statistic", "chart", "indigo"),
            ("math", "calculator", "blue"),
            ("accounting", "ledger", "green"),
            ("finance", "ledger", "green"),
            ("econom", "trend", "amber"),
            ("business", "briefcase", "orange"),
            ("management", "briefcase", "orange"),
            ("information", "laptop", "teal"),
            ("computer", "code", "teal"),
            ("law", "scale", "purple"),
            ("psych", "brain", "pink"),
            ("english", "book-open", "red"),
            ("language", "book-open", "red"),
            ("science", "flask", "green"),
            ("chemistry", "flask", "green"),
            ("biology", "flask", "green"),
            ("physics", "flask", "green")
        };

        /// <summary>A subject's icon in a small tinted tile (tutor cards, slot picker, bookings).</summary>
        public static string Subject(string subject, int size = 18)
        {
            var name = (subject ?? "").ToLowerInvariant();
            var match = SubjectIcons.FirstOrDefault(s => name.Contains(s.Match));
            var icon = match.Icon ?? "book";
            var tone = match.Tone ?? "navy";
            return "<span class=\"subject-icon subject-icon--" + tone + "\" title=\"" + System.Web.HttpUtility.HtmlAttributeEncode(subject) + "\">" + Svg(icon, size) + "</span>";
        }
    }
}