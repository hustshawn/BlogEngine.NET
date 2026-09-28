using BlogEngine.Core;
using BlogEngine.Core.Web;

namespace BlogEngine.NET.AppCode
{
    /// <summary>
    /// The rewrites of BlogEngine.Core's UrlRewrite module that are not handled by the Razor component
    /// routes (the pretty URLs of posts, pages, categories, authors, dates are routes of the pages):
    /// the front page (a page marked as front page or the CustomFrontPage setting), post/category feeds
    /// and the legacy /files/*.axdx and /images/*.jpgx links.
    /// </summary>
    public static class FrontEndUrlRewrite
    {
        public static IApplicationBuilder UseFrontEndUrlRewrite(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsPost(context.Request.Method))
                {
                    Rewrite(context);
                }

                await next();
            });
        }

        private static void Rewrite(HttpContext context)
        {
            // full path: Utils.RelativeWebRoot includes the PathBase (child blog virtual path)
            var path = Utils.GetRequestPath(context);
            var root = Utils.RelativeWebRoot;

            if (path.StartsWith($"{root}api/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith($"{root}admin", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Utils.IsCurrentRequestForHomepage)
            {
                if (!string.IsNullOrEmpty(BlogSettings.CustomFrontPage))
                {
                    SetPath(context, root + BlogSettings.CustomFrontPage.TrimStart('~', '/'));
                    return;
                }

                var front = BlogEngine.Core.Page.GetFrontPage();
                if (front != null && front.IsVisible)
                {
                    SetPath(context, front.RelativeLink);
                }

                return;
            }

            var url = path.ToUpperInvariant();

            if (url.Contains("/FILES/") && url.EndsWith(".AXDX"))
            {
                var file = FileNameBetween(path, "/files/");
                SetPath(context, $"{Utils.ApplicationRelativeWebRoot}file.axd", $"?file={Uri.EscapeDataString(file)}");
            }
            else if (url.Contains("/IMAGES/") && url.EndsWith(".JPGX"))
            {
                var file = FileNameBetween(path, "/images/");
                SetPath(context, $"{Utils.ApplicationRelativeWebRoot}image.axd", $"?picture={Uri.EscapeDataString(file)}");
            }
            else if (url.Contains("/POST/FEED/"))
            {
                var slug = UrlRules.ExtractTitle(context, path);
                var post = Post.ApplicablePosts.Find(p => slug.Equals(Utils.RemoveIllegalCharacters(p.Slug), StringComparison.OrdinalIgnoreCase));
                if (post != null)
                {
                    SetPath(context, $"{Utils.ApplicationRelativeWebRoot}syndication.axd", $"?post={post.Id}{UrlRules.GetQueryString(context)}");
                }
            }
            else if (url.Contains("/CATEGORY/FEED/"))
            {
                var title = UrlRules.ExtractTitle(context, path);
                var category = Category.ApplicableCategories.FirstOrDefault(c =>
                    title.Equals(Utils.RemoveIllegalCharacters(c.Title), StringComparison.OrdinalIgnoreCase));
                if (category != null)
                {
                    SetPath(context, $"{Utils.ApplicationRelativeWebRoot}syndication.axd", $"?category={category.Id}{UrlRules.GetQueryString(context)}");
                }
            }
        }

        private static string FileNameBetween(string path, string folder)
        {
            var start = path.IndexOf(folder, StringComparison.OrdinalIgnoreCase) + folder.Length - 1;
            var file = path.Substring(start);
            return file.Substring(0, file.LastIndexOf('.'));
        }

        private static void SetPath(HttpContext context, string pathAndQuery, string query = null)
        {
            var index = pathAndQuery.IndexOf('?');
            if (index >= 0)
            {
                query = pathAndQuery.Substring(index) + (query ?? string.Empty);
                pathAndQuery = pathAndQuery.Substring(0, index);
            }

            // targets are app-absolute; routing sees the path below PathBase
            var pathBase = context.Request.PathBase.Value;
            if (!string.IsNullOrEmpty(pathBase) && pathAndQuery.StartsWith(pathBase + "/", StringComparison.OrdinalIgnoreCase))
                pathAndQuery = pathAndQuery.Substring(pathBase.Length);

            context.Request.Path = new PathString(pathAndQuery);
            if (query != null)
            {
                if (query.StartsWith("?&"))
                {
                    query = "?" + query.Substring(2);
                }

                context.Request.QueryString = new QueryString(query);
            }
        }
    }
}
