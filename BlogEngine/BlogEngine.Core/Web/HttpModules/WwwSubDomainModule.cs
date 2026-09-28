namespace BlogEngine.Core.Web.HttpModules
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Removes or adds the www subdomain from all requests
    /// and makes a permanent redirection to the new location.
    /// Converted from IHttpModule to ASP.NET Core middleware.
    /// </summary>
    public class WwwSubDomainMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// The link regex.
        /// </summary>
        private static readonly Regex LinkRegex = new Regex(
            "(http|https)://www\\.", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="WwwSubDomainMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public WwwSubDomainMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processes the request and handles www subdomain redirection.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Clear the InstanceIdOverride to ensure any values are not
            // carried over from previous times this thread from the thread
            // pool was used.
            Blog.InstanceIdOverride = Guid.Empty;

            if (BlogSettings.Instance.HandleWwwSubdomain == "ignore" ||
                string.IsNullOrEmpty(BlogSettings.Instance.HandleWwwSubdomain))
            {
                await _next(context);
                return;
            }

            var request = context.Request;
            var requestMethod = request.Method;
            var rawUrl = request.Path.Value + request.QueryString.Value;
            var isLocal = IsLocalRequest(context);

            if (requestMethod != "GET" || (rawUrl?.Contains("/admin/") ?? false) || isLocal)
            {
                await _next(context);
                return;
            }

            var requestPath = request.Path.Value ?? string.Empty;
            var pathExtension = System.IO.Path.GetExtension(requestPath)?.Trim().ToLower() ?? string.Empty;

            if (requestPath.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ||
                pathExtension == BlogConfig.FileExtension.Trim().ToLower())
            {
                var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

                if (url.Contains("://www.") && BlogSettings.Instance.HandleWwwSubdomain == "remove")
                {
                    RemoveWww(context, url);
                    return;
                }

                if (!url.Contains("://www.") && BlogSettings.Instance.HandleWwwSubdomain == "add")
                {
                    AddWww(context, url);
                    return;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Adds the www subdomain to the request and redirects.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="url">The current URL.</param>
        private static void AddWww(HttpContext context, string url)
        {
            url = url.Replace("://", "://www.");
            PermanentRedirect(url, context);
        }

        /// <summary>
        /// Sends permanent redirection headers (301)
        /// </summary>
        /// <param name="url">The url to redirect to.</param>
        /// <param name="context">The HTTP context.</param>
        private static void PermanentRedirect(string url, HttpContext context)
        {
            if (url.EndsWith("default.aspx", StringComparison.OrdinalIgnoreCase))
            {
                url = url.ToLowerInvariant().Replace("default.aspx", string.Empty);
            }

            context.Response.StatusCode = 301;
            context.Response.Headers["Location"] = url;
        }

        /// <summary>
        /// Removes the www subdomain from the request and redirects.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="url">The current URL.</param>
        private static void RemoveWww(HttpContext context, string url)
        {
            if (!LinkRegex.IsMatch(url))
            {
                return;
            }

            url = LinkRegex.Replace(url, "$1://");
            PermanentRedirect(url, context);
        }

        /// <summary>
        /// Determines if the request is from localhost.
        /// </summary>
        private static bool IsLocalRequest(HttpContext context)
        {
            var connection = context.Connection;
            if (connection.RemoteIpAddress != null)
            {
                if (connection.LocalIpAddress != null)
                {
                    return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
                }
                return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
            }
            // If RemoteIpAddress is null, it's a local connection
            return connection.LocalIpAddress == null;
        }
    }
}
