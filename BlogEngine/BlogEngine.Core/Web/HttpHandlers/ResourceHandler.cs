using BlogEngine.Core.Web;

namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    using HttpModules;

    /// <summary>
    /// Serves embedded resource scripts with caching.
    /// </summary>
    public static class ResourceEndpoint
    {
        /// <summary>
        /// Maps the resource handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapResourceHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/{lang}.res.axd", HandleAsync);
            endpoints.MapGet("/admin.res.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var request = context.Request;
            var lang = request.Path.Value ?? string.Empty;

            lang = lang.Replace(".res.axd", "");

            if (lang.IndexOf("/") >= 0)
                lang = lang.Substring(lang.LastIndexOf("/") + 1);

            if (string.IsNullOrEmpty(lang))
            {
                // Use the current Language if the lang query isn't set.
                lang = BlogSettings.Instance.Language;
            }

            lang = lang.ToLowerInvariant();
            var sb = new StringBuilder();
            string cacheKey;
            string script;

            if ((request.Path.Value ?? string.Empty).Contains("admin.res.axd"))
            {
                lang = BlogSettings.Instance.Culture;

                cacheKey = "admin.resource.axd - " + lang;
                script = (string)Blog.CurrentInstance.Cache[cacheKey];

                if (String.IsNullOrEmpty(script))
                {
                    System.Globalization.CultureInfo culture;
                    try
                    {
                        culture = new System.Globalization.CultureInfo(lang);
                    }
                    catch (Exception)
                    {
                        culture = Utils.GetDefaultCulture();
                    }

                    var jc = new BlogCulture(culture, BlogCulture.ResourceType.Admin);

                    // add SiteVars used to pass server-side values to JavaScript in admin UI
                    var sbSiteVars = new StringBuilder();

                    sbSiteVars.Append("ApplicationRelativeWebRoot: '" + Utils.ApplicationRelativeWebRoot + "',");
                    sbSiteVars.Append("RelativeWebRoot: '" + Utils.RelativeWebRoot + "',");
                    sbSiteVars.Append("AbsoluteWebRoot:  '" + Utils.AbsoluteWebRoot + "',");

                    sbSiteVars.Append("IsPrimary: '" + Blog.CurrentInstance.IsPrimary + "',");
                    sbSiteVars.Append("BlogInstanceId: '" + Blog.CurrentInstance.Id + "',");
                    sbSiteVars.Append("BlogStorageLocation: '" + Blog.CurrentInstance.StorageLocation + "',");
                    sbSiteVars.Append("BlogFilesFolder: '" + Utils.FilesFolder + "',");

                    sbSiteVars.Append("GenericPageSize:  '" + BlogConfig.GenericPageSize.ToString() + "',");
                    sbSiteVars.Append("GalleryFeedUrl:  '" + BlogConfig.GalleryFeedUrl + "',");
                    sbSiteVars.Append("Version: 'BlogEngine.NET " + BlogSettings.Instance.Version() + "'");

                    sb.Append("SiteVars = {" + sbSiteVars.ToString() + "}; BlogAdmin = { i18n: " + jc.ToJsonString() + "};");
                    script = sb.ToString();
                }
            }
            else
            {
                cacheKey = "resource.axd - " + lang;
                script = (string)Blog.CurrentInstance.Cache[cacheKey];

                if (String.IsNullOrEmpty(script))
                {
                    System.Globalization.CultureInfo culture;
                    try
                    {
                        culture = new System.Globalization.CultureInfo(lang);
                    }
                    catch (Exception)
                    {
                        culture = Utils.GetDefaultCulture();
                    }

                    var jc = new BlogCulture(culture, BlogCulture.ResourceType.Blog);

                    // Although this handler is intended to output resource strings,
                    // also outputting other non-resource variables.
                    sb.AppendFormat("webRoot: '{0}',", Utils.RelativeWebRoot);
                    sb.AppendFormat("applicationWebRoot: '{0}',", Utils.ApplicationRelativeWebRoot);
                    sb.AppendFormat("blogInstanceId: '{0}',", Blog.CurrentInstance.Id);
                    sb.AppendFormat("fileExtension: '{0}',", BlogConfig.FileExtension);
                    sb.AppendFormat("i18n: {0}", jc.ToJsonString());
                    script = "BlogEngineRes = {" + sb + "};";
                }
            }

            Blog.CurrentInstance.Cache.Insert(cacheKey, script);

            SetHeaders(script.GetHashCode(), context);
            await context.Response.WriteAsync(script);

            if (BlogSettings.Instance.EnableHttpCompression)
            {
                // Compression handled by ResponseCompression middleware
            }
        }

        /// <summary>
        /// Returns the script path used to load resources on a page.
        /// </summary>
        public static string GetScriptPath(System.Globalization.CultureInfo cultureInfo)
        {
            return $"{Utils.RelativeWebRoot}{cultureInfo.Name.ToLowerInvariant()}.res.axd";
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
            response.Headers.Expires = DateTime.UtcNow.AddDays(30).ToString("R");
            response.Headers.CacheControl = "public, max-age=2592000"; // 30 days

            var etag = $"\"{hash}\"";
            var incomingEtag = context.Request.Headers["If-None-Match"].ToString();

            response.Headers["ETag"] = etag;

            if (String.Compare(incomingEtag, etag) == 0)
            {
                response.StatusCode = (int)HttpStatusCode.NotModified;
                return;
            }
        }
    }
}
