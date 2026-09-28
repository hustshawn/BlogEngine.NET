namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// A blog sitemap suitable for Google Sitemap as well as
    /// other big search engines such as MSN/Live, Yahoo and Ask.
    /// </summary>
    public static class SiteMapEndpoint
    {
        /// <summary>
        /// Maps the sitemap handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapSiteMapHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/sitemap.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            context.Response.ContentType = "text/xml";

            using (var writer = XmlWriter.Create(context.Response.Body))
            {
                writer.WriteStartElement("urlset", BlogConfig.SiteMapUrlSet);

                // Posts
                foreach (var post in Post.Posts.Where(post => post.IsVisibleToPublic))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", post.AbsoluteLink.AbsoluteUri.ToString());
                    writer.WriteElementString(
                        "lastmod", post.DateModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", "monthly");
                    writer.WriteEndElement();
                }

                // Pages
                foreach (var page in Page.Pages.Where(page => page.IsVisibleToPublic))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", page.AbsoluteLink.AbsoluteUri);
                    writer.WriteElementString(
                        "lastmod", page.DateModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", "monthly");
                    writer.WriteEndElement();
                }

                // Contact
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{Utils.AbsoluteWebRoot}contact.aspx");
                writer.WriteElementString("lastmod", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", "monthly");
                writer.WriteEndElement();

                // Blog
                if (Page.GetFrontPage() != null)
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{Utils.AbsoluteWebRoot}blog.aspx");
                    writer.WriteElementString(
                        "lastmod", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", "daily");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            await Task.CompletedTask;
        }
    }
}
