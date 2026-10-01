using System.IO;
using System.Web;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Handlers
{
    /// <summary>
    /// Streams a supplier invoice PDF from App_Data (outside the web root) to its supplier or an
    /// invoice reviewer only. Anyone else gets 404, so invoice ids cannot be probed.
    /// </summary>
    public sealed class InvoiceFile : IHttpHandler
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            var actor = WebAuth.ActorFor(context);
            if (actor == null || !int.TryParse(context.Request.QueryString["id"], out var id))
            {
                context.Response.StatusCode = 404;
                return;
            }
            var bytes = AppServices.Current.SupplierOrders.InvoiceFile(actor, id, out var fileName);
            if (bytes == null)
            {
                context.Response.StatusCode = 404;
                return;
            }
            var safeName = Path.GetFileName(fileName ?? "invoice.pdf").Replace("\"", "");
            context.Response.ContentType = "application/pdf";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Cache.SetCacheability(HttpCacheability.Private);
            context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + safeName + "\"");
            context.Response.BinaryWrite(bytes);
        }
    }
}
