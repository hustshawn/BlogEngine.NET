namespace BlogEngine.Core.Web.HttpHandlers
{
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// The OPML handler endpoint.
    /// </summary>
    public static class OpmlEndpoint
    {
        /// <summary>
        /// Maps the OPML handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapOpmlHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/opml.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            context.Response.ContentType = "text/xml";

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

            using (var writer = XmlWriter.Create(context.Response.Body, writerSettings))
            {
                // open OPML
                writer.WriteStartElement("opml");

                writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("version", "1.0");

                // open BODY
                writer.WriteStartElement("body");

                var blogroll = new Data.ViewModels.BlogRollVM();
                foreach (var br in blogroll.BlogRolls)
                {
                    // open OUTLINE
                    writer.WriteStartElement("outline");

                    if (!string.IsNullOrEmpty(br.Xfn))
                    {
                        writer.WriteAttributeString("xfn", br.Xfn);
                    }

                    writer.WriteAttributeString("title", br.Title);
                    writer.WriteAttributeString("description", br.Description);
                    writer.WriteAttributeString("xmlUrl", br.FeedUrl.ToString());
                    writer.WriteAttributeString("htmlUrl", br.BlogUrl.ToString());

                    // close OUTLINE
                    writer.WriteEndElement();
                }

                // close BODY
                writer.WriteEndElement();

                // close OPML
                writer.WriteEndElement();
            }

            await Task.CompletedTask;
        }
    }
}
