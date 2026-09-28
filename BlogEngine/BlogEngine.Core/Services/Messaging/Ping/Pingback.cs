namespace BlogEngine.Core.Ping
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Sends pingbacks to website that the blog links to.
    /// Converted from HttpWebRequest to static HttpClient.
    /// </summary>
    public static class Pingback
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        });

        #region Events

        /// <summary>
        ///     Occurs just before a pingback is sent.
        /// </summary>
        public static event EventHandler<EventArgs> Sending;

        /// <summary>
        ///     Occurs when a pingback has been sent
        /// </summary>
        public static event EventHandler<EventArgs> Sent;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends pingbacks to the targetUrl.
        /// </summary>
        /// <param name="sourceUrl">The source Url.</param>
        /// <param name="targetUrl">The target Url.</param>
        public static void Send(Uri sourceUrl, Uri targetUrl)
        {
            if (!BlogSettings.Instance.EnablePingBackSend)
            {
                return;
            }

            if (sourceUrl == null || targetUrl == null)
            {
                return;
            }

            try
            {
                var response = _httpClient.GetAsync(targetUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string pingUrl = null;

                // Look for X-Pingback or Pingback header
                if (response.Headers.TryGetValues("X-Pingback", out var xpingValues))
                {
                    pingUrl = xpingValues.FirstOrDefault();
                }
                else if (response.Headers.TryGetValues("Pingback", out var pingValues))
                {
                    pingUrl = pingValues.FirstOrDefault();
                }

                Uri url;
                if (!string.IsNullOrEmpty(pingUrl) && Uri.TryCreate(pingUrl, UriKind.Absolute, out url))
                {
                    OnSending(url);

                    var xmlContent = BuildXmlContent(sourceUrl, targetUrl);
                    var content = new StringContent(xmlContent, Encoding.ASCII, "text/xml");
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = content;
                    request.Headers.Add("Accept-Language", "en-us");

                    var response2 = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                    OnSent(url);
                }
            }
            catch (Exception ex)
            {
                ex = new Exception();
                // Stops unhandled exceptions that can cause the app pool to recycle
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds the XML-RPC content for the pingback request.
        /// </summary>
        /// <param name="sourceUrl">The source Url.</param>
        /// <param name="targetUrl">The target Url.</param>
        /// <returns>The XML content as a string.</returns>
        private static string BuildXmlContent(Uri sourceUrl, Uri targetUrl)
        {
            using var sw = new StringWriter();
            using (var writer = new XmlTextWriter(sw))
            {
                writer.WriteStartDocument(true);
                writer.WriteStartElement("methodCall");
                writer.WriteElementString("methodName", "pingback.ping");
                writer.WriteStartElement("params");

                writer.WriteStartElement("param");
                writer.WriteStartElement("value");
                writer.WriteElementString("string", sourceUrl.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("param");
                writer.WriteStartElement("value");
                writer.WriteElementString("string", targetUrl.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            return sw.ToString();
        }

        /// <summary>
        /// Called when [sending].
        /// </summary>
        /// <param name="url">The URL Uri.</param>
        private static void OnSending(Uri url)
        {
            Sending?.Invoke(url, EventArgs.Empty);
        }

        /// <summary>
        /// Called when [sent].
        /// </summary>
        /// <param name="url">The URL Uri.</param>
        private static void OnSent(Uri url)
        {
            Sent?.Invoke(url, EventArgs.Empty);
        }

        #endregion
    }
}
