namespace BlogEngine.Core.Ping
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Xml;

    using BlogEngine.Core.Providers;

    /// <summary>
    /// Pings various blog ping services.
    /// Converted from HttpWebRequest to static HttpClient.
    /// </summary>
    public static class PingService
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        })
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        #region Public Methods

        /// <summary>
        /// Sends a ping to various ping services asynchronously.
        /// </summary>
        /// <param name="url">The url Uri.</param>
        public static void Send(Uri url)
        {
            var services = BlogService.LoadPingServices();
            foreach (var service in services)
            {
                Execute(service, url);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the XML-RPC content for the ping request.
        /// </summary>
        /// <param name="url">The url Uri.</param>
        /// <returns>The XML content as a string.</returns>
        private static string BuildXmlContent(Uri url)
        {
            using var sw = new StringWriter();
            using (var writer = new XmlTextWriter(sw))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("methodCall");
                writer.WriteElementString("methodName", "weblogUpdates.ping");
                writer.WriteStartElement("params");
                writer.WriteStartElement("param");
                writer.WriteElementString("value", BlogSettings.Instance.Name);
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteElementString("value", url.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            return sw.ToString();
        }

        /// <summary>
        /// Creates a request and invokes the ping service.
        /// </summary>
        /// <param name="service">The service URL.</param>
        /// <param name="url">The url Uri.</param>
        private static void Execute(string service, Uri url)
        {
            try
            {
                var xmlContent = BuildXmlContent(url);
                var content = new StringContent(xmlContent, Encoding.ASCII, "text/xml");
                var response = _httpClient.PostAsync(service, content).GetAwaiter().GetResult();
                // We don't need to process the response for ping services
            }
            catch (Exception)
            {
                // Log the error.
            }
        }

        #endregion
    }
}
