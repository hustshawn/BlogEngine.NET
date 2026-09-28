namespace BlogEngine.Core.Web.HttpModules
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Response compression middleware.
    /// In ASP.NET Core, built-in ResponseCompression middleware handles gzip/deflate.
    /// This middleware preserves the original logic that determines WHEN to compress
    /// (checking BlogSettings, content types, browser support) and delegates the
    /// actual compression to the built-in middleware.
    /// </summary>
    public class CompressionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string Deflate = "deflate";
        private const string Gzip = "gzip";

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public CompressionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processes the request and determines if compression should be applied.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!BlogSettings.Instance.EnableHttpCompression)
            {
                await _next(context);
                return;
            }

            var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();
            bool supportsCompression = !string.IsNullOrEmpty(acceptEncoding) &&
                (acceptEncoding.Contains(Gzip, StringComparison.OrdinalIgnoreCase) ||
                 acceptEncoding.Contains(Deflate, StringComparison.OrdinalIgnoreCase));

            if (!supportsCompression)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;

            // bundled javascripts and styles - set caching headers
            if (path.Contains("/Scripts/") || path.Contains("/Styles/") || path.EndsWith(".js.axd"))
            {
                if (!path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
                {
                    SetCacheHeaders(context, path);
                }
            }

            // Skip admin pages for WebResourceFilter behavior (WebForms-only, not applicable)
            // The built-in ResponseCompression middleware will handle actual compression

            await _next(context);
        }

        /// <summary>
        /// Checks if the specified encoding is accepted by the client.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="encoding">The encoding to check.</param>
        /// <returns>True if the encoding is accepted.</returns>
        public static bool IsEncodingAccepted(HttpContext context, string encoding)
        {
            var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString();
            return !string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Contains(encoding);
        }

        private static void SetCacheHeaders(HttpContext context, string path)
        {
            var response = context.Response;
            response.Headers["Vary"] = "Accept-Encoding";
            response.Headers["Cache-Control"] = "public, max-age=31536000";
            response.Headers["Expires"] = DateTime.UtcNow.AddDays(30).ToString("R");

            var etag = $"\"{path.GetHashCode()}\"";
            var incomingEtag = context.Request.Headers["If-None-Match"].ToString();

            response.Headers["ETag"] = etag;

            if (string.Equals(incomingEtag, etag, StringComparison.Ordinal))
            {
                response.StatusCode = 304;
                return;
            }
        }
    }
}
