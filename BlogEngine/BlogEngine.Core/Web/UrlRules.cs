using System.Net;
using Microsoft.AspNetCore.Http;

namespace BlogEngine.Core.Web
{
    /// <summary>
    /// URL helpers of the former UrlRewrite module (rewriting itself is done by Razor component routes
    /// and the web application's FrontEndUrlRewrite middleware)
    /// </summary>
    public static class UrlRules
    {
        /// <summary>
        /// Extracts the title from the requested URL.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="url">
        /// The url string.
        /// </param>
        /// <returns>
        /// The extract title.
        /// </returns>
        public static string ExtractTitle(HttpContext context, string url)
        {
            url = url.ToLowerInvariant().Replace("---", "-");

            if (string.IsNullOrEmpty(BlogConfig.FileExtension))
            {
                if (url.Contains("?"))
                    url = url.Substring(0, url.LastIndexOf("?"));

                if (url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);

                if (url.Contains("/"))
                    url = url.Substring(url.LastIndexOf("/") + 1);

                return WebUtility.HtmlEncode(url);
            }

            if (url.Contains(BlogConfig.FileExtension) && url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
                context.Response.Headers.Append("location", url);
                context.Response.StatusCode = 301;
            }

            if (url.Contains(BlogConfig.FileExtension))
                url = url.Substring(0, url.IndexOf(BlogConfig.FileExtension));

            var index = url.LastIndexOf("/") + 1;
            var title = url.Substring(index);
            return WebUtility.HtmlEncode(title);
        }

        /// <summary>
        /// Gets the query string from the requested URL.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The query string.
        /// </returns>
        public static string GetQueryString(HttpContext context)
        {
            var query = context.Request.QueryString.ToString().TrimStart('?');
            return !string.IsNullOrEmpty(query) ? $"&{query}" : string.Empty;
        }
    }
}
