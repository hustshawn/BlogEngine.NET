namespace BlogEngine.Core.Ping
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// The trackback.
    /// Converted from HttpWebRequest to static HttpClient.
    /// </summary>
    public static class Trackback
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        #region Events

        /// <summary>
        ///     Occurs just before a trackback is sent.
        /// </summary>
        public static event EventHandler<EventArgs> Sending;

        /// <summary>
        ///     Occurs when a trackback has been sent
        /// </summary>
        public static event EventHandler<EventArgs> Sent;

        #endregion

        #region Public Methods

        /// <summary>
        /// The send.
        /// </summary>
        /// <param name="message">The trackback message.</param>
        /// <returns>True if the trackback was sent successfully.</returns>
        public static bool Send(TrackbackMessage message)
        {
            if (!BlogSettings.Instance.EnableTrackBackSend)
            {
                return false;
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            OnSending(message.UrlToNotifyTrackback);

            try
            {
                var content = new StringContent(message.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = _httpClient.PostAsync(message.UrlToNotifyTrackback, content).GetAwaiter().GetResult();

                OnSent(message.UrlToNotifyTrackback);

                string answer = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return response.StatusCode == HttpStatusCode.OK && answer.Contains("<error>0</error>");
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when [sending].
        /// </summary>
        /// <param name="url">The URL.</param>
        private static void OnSending(Uri url)
        {
            Sending?.Invoke(url, EventArgs.Empty);
        }

        /// <summary>
        /// Called when [sent].
        /// </summary>
        /// <param name="url">The URL.</param>
        private static void OnSent(Uri url)
        {
            Sent?.Invoke(url, EventArgs.Empty);
        }

        #endregion
    }
}
