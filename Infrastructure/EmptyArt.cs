using System.Collections.Generic;

namespace StudyLink.Web.Infrastructure
{
    /// <summary>The small flat illustrations shown when a list is empty (one style, brand colours).</summary>
    public enum EmptyArt
    {
        Calendar,
        Receipt,
        Books,
        Search,
        Bell
    }

    internal static class EmptyArtSvg
    {
        // Shared backdrop: a soft blue disc and a ground shadow, so every illustration sits the same way.
        private const string Backdrop =
            "<circle cx='70' cy='54' r='46' fill='#eef3ff'/><ellipse cx='70' cy='100' rx='38' ry='5' fill='#dfe6f3'/>";

        private const string CheckBadge =
            "<circle cx='98' cy='76' r='13' fill='#1c8038'/><path d='M92 76l4 4 8-8' stroke='#fff' stroke-width='3' fill='none' stroke-linecap='round' stroke-linejoin='round'/>";

        private static readonly Dictionary<EmptyArt, string> Art = new Dictionary<EmptyArt, string>
        {
            [EmptyArt.Calendar] = Backdrop +
                "<rect x='38' y='26' width='64' height='58' rx='8' fill='#fff' stroke='#c9d3e3' stroke-width='2'/>" +
                "<path d='M38 34a8 8 0 0 1 8-8h48a8 8 0 0 1 8 8v8H38z' fill='#0c256c'/>" +
                "<rect x='50' y='20' width='4' height='12' rx='2' fill='#0f6af8'/><rect x='86' y='20' width='4' height='12' rx='2' fill='#0f6af8'/>" +
                "<g fill='#c9d3e3'><rect x='48' y='50' width='8' height='7' rx='2'/><rect x='62' y='50' width='8' height='7' rx='2'/><rect x='76' y='50' width='8' height='7' rx='2'/>" +
                "<rect x='48' y='63' width='8' height='7' rx='2'/><rect x='62' y='63' width='8' height='7' rx='2'/></g>" + CheckBadge,
            [EmptyArt.Receipt] = Backdrop +
                "<path d='M46 20h48v66l-6-4-6 4-6-4-6 4-6-4-6 4-6-4-6 4z' fill='#fff' stroke='#c9d3e3' stroke-width='2' stroke-linejoin='round'/>" +
                "<g stroke='#c9d3e3' stroke-width='3' stroke-linecap='round'><path d='M55 34h30M55 44h22M55 54h26'/></g>" +
                "<path d='M55 66h16' stroke='#0c256c' stroke-width='3' stroke-linecap='round'/>" + CheckBadge,
            [EmptyArt.Books] = Backdrop +
                "<rect x='40' y='34' width='14' height='60' rx='2' fill='#0c256c'/><rect x='56' y='26' width='14' height='68' rx='2' fill='#0f6af8'/>" +
                "<rect x='72' y='40' width='12' height='54' rx='2' fill='#f36c39'/>" +
                "<rect x='86' y='34' width='13' height='60' rx='2' fill='#fdc635' transform='rotate(12 92 94)'/>" +
                "<g stroke='#fff' stroke-width='2' opacity='.7'><path d='M43 44h8M59 36h8M74 50h8'/></g>",
            [EmptyArt.Search] = Backdrop +
                "<rect x='34' y='28' width='58' height='50' rx='8' fill='#fff' stroke='#c9d3e3' stroke-width='2'/>" +
                "<g stroke='#c9d3e3' stroke-width='3' stroke-linecap='round'><path d='M44 42h30M44 52h20M44 62h24'/></g>" +
                "<circle cx='88' cy='66' r='14' fill='#fff' stroke='#0c256c' stroke-width='4'/><path d='M98 76l10 10' stroke='#0c256c' stroke-width='5' stroke-linecap='round'/>",
            [EmptyArt.Bell] = Backdrop +
                "<path d='M50 76V56a20 20 0 0 1 40 0v20l6 6H44z' fill='#fff' stroke='#c9d3e3' stroke-width='2' stroke-linejoin='round'/>" +
                "<path d='M64 88a6 6 0 0 0 12 0' fill='#0c256c'/><circle cx='70' cy='32' r='4' fill='#0c256c'/>" +
                "<g stroke='#0f6af8' stroke-width='3' stroke-linecap='round'><path d='M38 46c-3 4-4 9-3 14M102 46c3 4 4 9 3 14'/></g>"
        };

        public static string Render(EmptyArt art) =>
            "<svg class=\"empty-state__art\" viewBox=\"0 0 140 110\" aria-hidden=\"true\" focusable=\"false\">" + Art[art].Replace('\'', '"') + "</svg>";
    }
}
