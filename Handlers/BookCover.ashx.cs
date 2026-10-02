using System;
using System.Web;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Handlers
{
    /// <summary>Serves book cover images stored in the database (public: covers are catalogue content).</summary>
    public sealed class BookCover : IHttpHandler
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            if (!int.TryParse(context.Request.QueryString["id"], out var id))
            {
                context.Response.StatusCode = 400;
                return;
            }
            var bytes = AppServices.Current.Catalogue.Cover(id, out var contentType);
            if (bytes == null || bytes.Length == 0)
            {
                context.Response.StatusCode = 404;
                return;
            }
            context.Response.ContentType = contentType ?? "image/jpeg";
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetMaxAge(TimeSpan.FromHours(1));
            context.Response.BinaryWrite(bytes);
        }
    }
}