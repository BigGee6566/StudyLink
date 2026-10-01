using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using StudyLink.Services.DTOs;
using StudyLink.Services.Security;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Operations
{
    /// <summary>
    /// SC24 - Reports. Managers see every report; Staff see the operational subset. Results are built
    /// from live tables (each run is logged in ReportRuns) and can be exported as CSV.
    /// </summary>
    [RequirePermission(Permission.ViewOperationalReports, Permission.ViewAllReports)]
    public partial class Reports : BasePage
    {
        private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

        protected void Page_Load(object sender, EventArgs e)
        {
            Subheading = "Live figures for tutoring, books, money and stock.";
            var key = Query("key");
            var from = Ui.Date(Query("from"));
            var to = Ui.Date(Query("to"));

            if (!IsPostBack && Query("export") == "csv" && !string.IsNullOrEmpty(key))
            {
                Export(key, from, to);
                return;
            }
            if (IsPostBack) return;

            var available = S.Reports.Available(Actor);
            Ui.FillText(Report, available.Select(d => d.Key), null, k => available.First(d => d.Key == k).Title);
            Definitions.DataSource = available;
            Definitions.DataBind();
            var today = DateTime.Today;
            From.Text = Ui.IsoDate(from ?? today.AddDays(-30));
            To.Text = Ui.IsoDate(to ?? today);
            if (string.IsNullOrEmpty(key)) return;

            Ui.Select(Report, key);
            var def = available.FirstOrDefault(d => d.Key == key);
            if (def == null) return;
            Description.Text = H(def.Description) + (def.UsesDateRange ? "" : " This report shows the current position (dates are ignored).");
            Show(S.Reports.Run(Actor, key, Ui.Date(From.Text), Ui.Date(To.Text)));
        }

        protected void Run_Click(object sender, EventArgs e) =>
            Go("~/Operations/Reports.aspx?key=" + Report.SelectedValue + "&from=" + From.Text + "&to=" + To.Text);

        private void Show(ReportResult r)
        {
            Catalogue.Visible = false;
            ResultPanel.Visible = true;
            ResultTitle.Text = H(r.Definition.Title);
            Summary.Text = r.Summary.Count == 0 ? "" : Ui.Kpis(this, r.Summary.Select(kv => new Kpi(kv.Key, kv.Value)));
            Table.Text = Render(r.Table);
            Generated.Text = "Generated " + Fmt.DateTime(r.GeneratedAt) +
                (r.Definition.UsesDateRange && r.From.HasValue ? " · " + Fmt.Date(r.From) + " to " + Fmt.Date(r.To) : "") +
                " · " + r.Table.Rows.Count + " row" + (r.Table.Rows.Count == 1 ? "" : "s");
            CsvLink.HRef = ResolveUrl("~/Operations/Reports.aspx?export=csv&key=" + Server.UrlEncode(r.Definition.Key) +
                "&from=" + (r.From.HasValue ? Ui.IsoDate(r.From.Value) : "") + "&to=" + (r.To.HasValue ? Ui.IsoDate(r.To.Value) : ""));
        }

        private static string Render(DataTable table)
        {
            if (table.Rows.Count == 0) return Ui.Empty("chart", "No data for this report and period.");
            var sb = new StringBuilder("<table class=\"data-table stack\"><thead><tr>");
            foreach (DataColumn c in table.Columns)
                sb.Append("<th").Append(IsNumeric(c) ? " class=\"col-num\"" : "").Append('>').Append(Fmt.H(c.ColumnName)).Append("</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (DataRow row in table.Rows)
            {
                sb.Append("<tr>");
                foreach (DataColumn c in table.Columns)
                    sb.Append("<td data-label=\"").Append(Fmt.H(c.ColumnName)).Append('"').Append(IsNumeric(c) ? " class=\"col-num\"" : "").Append('>')
                      .Append(Fmt.H(Cell(row[c]))).Append("</td>");
                sb.Append("</tr>");
            }
            return sb.Append("</tbody></table>").ToString();
        }

        private static bool IsNumeric(DataColumn c) =>
            c.DataType == typeof(decimal) || c.DataType == typeof(int) || c.DataType == typeof(long) || c.DataType == typeof(double);

        private static string Cell(object value)
        {
            switch (value)
            {
                case null: case DBNull _: return "–";
                case decimal d: return d.ToString("N2", Gb);
                case double f: return f.ToString("N2", Gb);
                case DateTime t: return t.TimeOfDay == TimeSpan.Zero ? Fmt.Date(t) : Fmt.DateTime(t);
                case bool b: return b ? "Yes" : "No";
                default: return Convert.ToString(value, Gb);
            }
        }

        private void Export(string key, DateTime? from, DateTime? to)
        {
            var result = S.Reports.Run(Actor, key, from, to);
            var csv = S.Reports.ToCsv(result);
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.ContentEncoding = Encoding.UTF8;
            Response.AddHeader("Content-Disposition", "attachment; filename=\"studylink-" + key + "-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".csv\"");
            Response.Write("﻿" + csv);
            Response.Flush();
            Response.SuppressContent = true;
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
