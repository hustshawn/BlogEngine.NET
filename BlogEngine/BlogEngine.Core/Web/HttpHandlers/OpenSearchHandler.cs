namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Displays the open search XML provider as described at http://opensearch.a9.com/
    /// </summary>
    public static class OpenSearchEndpoint
    {
        /// <summary>
        /// Maps the OpenSearch handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapOpenSearchHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/opensearch.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(context.Response.Body, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("OpenSearchDescription", "http://a9.com/-/spec/opensearch/1.1/");
                writer.WriteAttributeString("xmlns", "http://a9.com/-/spec/opensearch/1.1/");

                writer.WriteElementString("ShortName", BlogSettings.Instance.Name);
                writer.WriteElementString("Description", BlogSettings.Instance.Description);

                writer.WriteStartElement("Image");
                writer.WriteAttributeString("height", "16");
                writer.WriteAttributeString("width", "16");
                writer.WriteAttributeString("type", "image/vnd.microsoft.icon");
                writer.WriteValue($"{Utils.AbsoluteWebRoot}Content/images/blog/favicon.ico");
                writer.WriteEndElement();

                writer.WriteStartElement("Url");
                writer.WriteAttributeString("type", "text/html");
                writer.WriteAttributeString("template", $"{Utils.AbsoluteWebRoot}search{BlogConfig.FileExtension}?q={{searchTerms}}");
                writer.WriteEndElement();

                writer.WriteStartElement("Url");
                writer.WriteAttributeString("type", "application/rss+xml");
                writer.WriteAttributeString("template", $"{Utils.AbsoluteWebRoot}syndication.axd?q={{searchTerms}}");
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            context.Response.ContentType = "text/xml; charset=UTF-8";
            context.Response.Headers.Expires = DateTime.Now.AddDays(30).ToString("R");
            context.Response.Headers["ETag"] = Guid.NewGuid().ToString();

            await Task.CompletedTask;
        }
    }
}
