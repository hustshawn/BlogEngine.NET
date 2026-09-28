namespace BlogEngine.Core.Web.HttpHandlers
{
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// RSD (Really Simple Discoverability) Handler
    /// http://cyber.law.harvard.edu/blogs/gems/tech/rsd.html
    /// </summary>
    public static class RsdEndpoint
    {
        /// <summary>
        /// Maps the RSD handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapRsdHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/rsd.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            context.Response.ContentType = "text/xml";
            using (var rsd = new XmlTextWriter(context.Response.Body, Encoding.UTF8))
            {
                rsd.Formatting = Formatting.Indented;
                rsd.WriteStartDocument();

                // Rsd tag
                rsd.WriteStartElement("rsd");
                rsd.WriteAttributeString("version", "1.0");

                // Service
                rsd.WriteStartElement("service");
                rsd.WriteElementString("engineName", $"BlogEngine.NET {BlogSettings.Instance.Version()}");
                rsd.WriteElementString("engineLink", "https://blogengine.io");
                rsd.WriteElementString("homePageLink", Utils.AbsoluteWebRoot.ToString());

                // APIs
                rsd.WriteStartElement("apis");

                // MetaWeblog
                rsd.WriteStartElement("api");
                rsd.WriteAttributeString("name", "MetaWeblog");
                rsd.WriteAttributeString("preferred", "true");
                var prefix = BlogSettings.Instance.RequireSslMetaWeblogApi ? "https://" : "http://";
                rsd.WriteAttributeString(
                    "apiLink", $"{prefix}{context.Request.Host}{Utils.RelativeWebRoot}metaweblog.axd");
                rsd.WriteAttributeString("blogID", Utils.AbsoluteWebRoot.ToString());
                rsd.WriteEndElement();

                // BlogML
                rsd.WriteStartElement("api");
                rsd.WriteAttributeString("name", "BlogML");
                rsd.WriteAttributeString("preferred", "false");
                rsd.WriteAttributeString("apiLink", $"{Utils.AbsoluteWebRoot}api/BlogImporter.asmx");
                rsd.WriteAttributeString("blogID", Utils.AbsoluteWebRoot.ToString());
                rsd.WriteEndElement();

                // End APIs
                rsd.WriteEndElement();

                // End Service
                rsd.WriteEndElement();

                // End Rsd
                rsd.WriteEndElement();

                rsd.WriteEndDocument();
            }

            await Task.CompletedTask;
        }
    }
}
