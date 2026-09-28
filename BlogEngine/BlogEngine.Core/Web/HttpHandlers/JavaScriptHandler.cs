namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Serves cached JavaScript resources.
    /// </summary>
    public static class JavaScriptEndpoint
    {
        /// <summary>
        /// Maps the JavaScript handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapJavaScriptHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/js.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var request = context.Request;
            string fileName = request.Path.Value ?? string.Empty;
            if (fileName.StartsWith(Utils.RelativeWebRoot))
            {
                fileName = fileName.Substring(Utils.RelativeWebRoot.Length);
            }

            string resKey = "";
            string script = "";

            if (fileName.StartsWith("res-"))
            {
                resKey = fileName.Substring(4);
                resKey = resKey.Substring(0, resKey.IndexOf("."));

                if (Blog.CurrentInstance.Cache[resKey] != null)
                {
                    script = (string)Blog.CurrentInstance.Cache[resKey];
                }
            }

            SetHeaders(script.GetHashCode(), context);
            await context.Response.WriteAsync(script);
        }

        /// <summary>
        /// This will make the browser and server keep the output
        /// in its cache and thereby improve performance.
        /// </summary>
        private static void SetHeaders(int hash, HttpContext context)
        {
            var response = context.Response;
            response.ContentType = "text/javascript";

            response.Headers["Vary"] = "Accept-Encoding";
            response.Headers.Expires = DateTime.Now.ToUniversalTime().AddDays(7).ToString("R");
            response.Headers.CacheControl = "public, max-age=2592000"; // 30 days

            var etag = $"\"{hash}\"";
            var incomingEtag = context.Request.Headers["If-None-Match"].ToString();

            if (String.Compare(incomingEtag, etag) == 0)
            {
                response.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
        }
    }
}
