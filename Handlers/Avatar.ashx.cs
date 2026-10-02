using System;
using System.Web;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Handlers
{
    /// <summary>
    /// Serves a profile photo when the viewer may see it (AvatarPolicy). A photo that is hidden from the
    /// viewer answers 404, exactly like a user without a photo, so the response reveals nothing.
    /// </summary>
    public sealed class Avatar : IHttpHandler
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            var response = context.Response; // nosniff comes from Web.config for every response
            if (!int.TryParse(context.Request.QueryString["id"], out var userId))
            {
                response.StatusCode = 404;
                return;
            }

            var photo = AppServices.Current.Profiles.Photo(WebAuth.ActorFor(context), userId);
            if (photo == null)
            {
                response.StatusCode = 404;
                response.Cache.SetCacheability(HttpCacheability.NoCache);
                return;
            }

            response.ContentType = photo.ContentType;
            if (photo.IsPublic)
            {
                response.Cache.SetCacheability(HttpCacheability.Public);
                response.Cache.SetMaxAge(TimeSpan.FromDays(1));
            }
            else
            {
                response.Cache.SetCacheability(HttpCacheability.Private);
            }
            response.Cache.SetLastModified(photo.UpdatedAt);
            response.BinaryWrite(photo.Data);
        }
    }
}
