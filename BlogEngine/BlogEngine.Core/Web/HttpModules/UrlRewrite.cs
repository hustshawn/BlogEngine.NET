namespace BlogEngine.Core.Web.HttpModules
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using BlogEngine.Core.Web;

    /// <summary>
    /// Handles pretty URL's and redirects them to the permalinks.
    /// Converted from IHttpModule to ASP.NET Core middleware.
    /// </summary>
    public class UrlRewriteMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlRewriteMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public UrlRewriteMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processes the request and applies URL rewriting rules.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToUpperInvariant() ?? string.Empty;
            var url = GetUrlWithQueryString(context).ToUpperInvariant();

            path = path.Replace(".ASPX.CS", string.Empty);
            url = url.Replace(".ASPX.CS", string.Empty);

            // skip url rewrite for web api calls
            if (url.ToLower().StartsWith($"{Utils.ApplicationRelativeWebRoot}api/"))
            {
                context.Request.Path = new PathString(GetUrlWithQueryString(context));
                await _next(context);
                return;
            }

            // to prevent XSS
            url = System.Net.WebUtility.HtmlEncode(url);

            Blog blogInstance = Blog.CurrentInstance;

            // bundled scripts and styles are in the ~/scripts and ~/styles
            // redirect path from ~/child/scripts/js to ~/scripts/js etc.
            if (!blogInstance.IsPrimary)
            {
                if (url.Contains("/SCRIPTS/") || url.Contains("/STYLES/"))
                {
                    var npath = url.Replace(Blog.CurrentInstance.RelativeWebRoot.ToUpper(), Utils.ApplicationRelativeWebRoot);
                    context.Request.Path = new PathString(npath);
                    await _next(context);
                    return;
                }
            }

            if (Utils.IsCurrentRequestForHomepage)
            {
                // custom front page
                if (!string.IsNullOrEmpty(BlogSettings.CustomFrontPage))
                {
                    url = Utils.RelativeOrAbsoluteWebRoot + BlogSettings.CustomFrontPage;
                    context.Request.Path = new PathString(url);
                }
                else
                {
                    var front = Page.GetFrontPage();
                    if (front != null)
                    {
                        url = front.RelativeLink.ToUpperInvariant();
                    }
                }
            }

            RedirectToRemoveFileExtension(context, blogInstance, url, path);

            Rewrite(context, blogInstance, url, path);

            await _next(context);
        }

        /// <summary>
        /// Gets the URL with query string from the current request.
        /// </summary>
        private static string GetUrlWithQueryString(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var qs = context.Request.QueryString.Value ?? string.Empty;
            return path + qs;
        }

        /// <summary>
        /// Checks to see if the incoming request is for a Post, Page or Category and contains the old .ASPX extension.
        /// If so and both RemoveExtensionsFromUrls and RedirectToRemoveFileExtension are true, the person is 301
        /// redirected to a URL with the .ASPX extension removed.
        /// </summary>
        private static void RedirectToRemoveFileExtension(HttpContext context, Blog blogInstance, string url, string path)
        {
            if (!BlogSettings.Instance.RedirectToRemoveFileExtension)
                return;

            if (!BlogSettings.Instance.RemoveExtensionsFromUrls)
                return;

            string rawUrl = context.Request.Path.Value + context.Request.QueryString.Value;
            int queryPos = rawUrl.IndexOf("?");
            if (queryPos != -1)  // remove any querystring for the purpose of determining if the .ASPX extension is present.
                rawUrl = rawUrl.Substring(0, queryPos);

            if (Path.GetExtension(rawUrl).ToUpperInvariant() != ".ASPX")
                return;

            if (url.Contains("/POST/") ||
                url.Contains("/PAGE/") ||
                url.Contains("/CATEGORY/") ||
                url.Contains("/AUTHOR/") ||
                url.Contains("/CALENDAR/") ||
                url.StartsWith(blogInstance.RelativeWebRoot + "contact.aspx", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith(blogInstance.RelativeWebRoot + "search.aspx", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith(blogInstance.RelativeWebRoot + "archive.aspx", StringComparison.OrdinalIgnoreCase))
            {
                string redirectUrl = context.Request.Path.Value + context.Request.QueryString.Value;
                int firstInstance = redirectUrl.IndexOf(".aspx", StringComparison.OrdinalIgnoreCase);
                if (firstInstance != -1)
                {
                    redirectUrl = string.Format("{0}{1}", redirectUrl.Substring(0, firstInstance), redirectUrl.Substring(firstInstance + ".aspx".Length)).Trim();
                    context.Response.StatusCode = 301;
                    context.Response.Headers["Location"] = redirectUrl;
                    // Note: In middleware, we don't call Response.End(); instead, we return without calling _next
                }
            }
        }

        private static void Rewrite(HttpContext context, Blog blogInstance, string url, string path)
        {
            var urlContainsFileExtension = BlogSettings.Instance.RemoveExtensionsFromUrls ? true :
                url.IndexOf(BlogConfig.FileExtension, StringComparison.OrdinalIgnoreCase) != -1;

            if (url.Contains("/FILES/") && url.Contains(".AXDX"))
            {
                UrlRules.RewriteFilePath(context, url);
            }
            if (url.Contains("/IMAGES/") && url.Contains(".JPGX"))
            {
                UrlRules.RewriteImagePath(context, url);
            }
            if (url.Contains("/POST/"))
            {
                UrlRules.RewritePost(context, url);
            }
            else if (urlContainsFileExtension && url.Contains("/CATEGORY/"))
            {
                UrlRules.RewriteCategory(context, url);
            }
            else if (urlContainsFileExtension && url.Contains("/PAGE/"))
            {
                UrlRules.RewritePage(context, url);
            }
            else if (urlContainsFileExtension && url.Contains("/CALENDAR/"))
            {
                if (url.Contains(".PNG") || url.Contains(".JPG") || url.Contains(".GIF"))
                {
                    // do not rewrite path to images
                    return;
                }
                UrlRules.RewriteCalendar(context, url);
            }
            else if (urlContainsFileExtension && UrlRules.DefaultPageRequested(context))
            {
                UrlRules.RewriteDefault(context);
            }
            else if (urlContainsFileExtension && url.Contains("/AUTHOR/"))
            {
                UrlRules.RewriteAuthor(context, url);
            }
            else if (urlContainsFileExtension && (path.Contains("/BLOG.ASPX") || path.EndsWith("/BLOG")))
            {
                UrlRules.RewriteBlog(context, url);
            }
            else if (BlogSettings.Instance.RemoveExtensionsFromUrls && path.StartsWith(blogInstance.RelativeWebRoot + "contact", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            {
                UrlRules.RewriteContact(context, url);
            }
            else if (BlogSettings.Instance.RemoveExtensionsFromUrls && path.StartsWith(blogInstance.RelativeWebRoot + "archive", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            {
                UrlRules.RewriteArchive(context, url);
            }
            else if (BlogSettings.Instance.RemoveExtensionsFromUrls && path.StartsWith(blogInstance.RelativeWebRoot + "search", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            {
                UrlRules.RewriteSearch(context, url);
            }
            else
            {
                // If this is blog instance that is in a virtual sub-folder, we will
                // need to rewrite the path for URL to a physical file.
                if (blogInstance.IsSubfolderOfApplicationWebRoot &&
                    (path + "/").IndexOf(blogInstance.RelativeWebRoot, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    bool skipRewrite = false;
                    string rewriteQs = string.Empty;
                    string rewriteUrl = GetUrlWithQueryString(context);

                    int qsStart = rewriteUrl.IndexOf("?");
                    if (qsStart != -1)  // remove querystring.
                    {
                        rewriteQs = rewriteUrl.Substring(qsStart);
                        rewriteUrl = rewriteUrl.Substring(0, qsStart);
                    }

                    string pathAfterAppWebRoot = rewriteUrl.Substring(Utils.ApplicationRelativeWebRoot.Length);

                    if (!pathAfterAppWebRoot.Contains("."))
                    {
                        if (!rewriteUrl.EndsWith("/"))
                            rewriteUrl += "/";

                        if (rewriteUrl.EndsWith("/admin/") || rewriteUrl.EndsWith("/admin"))
                            rewriteUrl += "index.cshtml";
                        else
                            rewriteUrl += "default.aspx";
                    }
                    else
                    {
                        if (Path.GetExtension(pathAfterAppWebRoot).ToUpperInvariant() == ".AXD")
                            skipRewrite = true;
                    }

                    if (!skipRewrite)
                    {
                        // remove the subfolder portion.  so /subfolder/ becomes /.
                        rewriteUrl = new Regex(Regex.Escape(blogInstance.RelativeWebRoot), RegexOptions.IgnoreCase).Replace(rewriteUrl, Utils.ApplicationRelativeWebRoot);

                        context.Request.Path = new PathString(rewriteUrl);
                        if (!string.IsNullOrEmpty(rewriteQs))
                        {
                            context.Request.QueryString = new QueryString(rewriteQs);
                        }
                    }

                    return;
                }
            }
        }
    }
}
