namespace BlogEngine.Core.Web.HttpModules
{
    using System;
    using System.Collections;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Referrer Module — logs referring URLs.
    /// Converted from IHttpModule to ASP.NET Core middleware.
    /// Uses HttpClient instead of WebClient for spam checking.
    /// </summary>
    public class ReferrerMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        });

        #region Events

        /// <summary>
        ///     Occurs when a visitor enters the website and the referrer is logged.
        /// </summary>
        public static event EventHandler<EventArgs> ReferrerRegistered;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferrerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public ReferrerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Processes the request and logs referrer information.
        /// Runs after the next middleware completes (EndRequest equivalent).
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // EndRequest logic
            if (!BlogSettings.Instance.EnableReferrerTracking)
                return;

            var path = context.Request.Path.Value?.ToUpperInvariant() ?? string.Empty;
            if (path.Contains(".") && !path.EndsWith("/"))
                return;

            var referrerHeader = context.Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referrerHeader) || !Uri.TryCreate(referrerHeader, UriKind.Absolute, out var referrer))
                return;

            var requestUrl = new Uri($"{context.Request.Scheme}://{context.Request.Host}{Utils.GetRawUrl(context)}");

            if (!referrer.Host.Equals(Utils.AbsoluteWebRoot.Host, StringComparison.OrdinalIgnoreCase) &&
                !IsSearchEngine(referrer.ToString()))
            {
                Guid blogId = Blog.CurrentInstance.Id;

                ThreadPool.QueueUserWorkItem(state =>
                {
                    // because HttpContext is not available within this BG thread
                    // needed to determine the current blog instance,
                    // set override value here.
                    Blog.InstanceIdOverride = blogId;

                    BeginRegisterClick(new DictionaryEntry(referrer, requestUrl));
                });
            }
        }

        #region Methods

        /// <summary>
        /// The begin register click.
        /// </summary>
        /// <param name="entry">Contains data to register.</param>
        private static void BeginRegisterClick(DictionaryEntry entry)
        {
            try
            {
                var referrer = (Uri)entry.Key;
                var url = (Uri)entry.Value;

                RegisterClick(url, referrer);
                OnReferrerRegistered(referrer);
            }
            catch (Exception)
            {
                // Could write to the file.
            }
        }

        /// <summary>
        /// Determines whether [is search engine] [the specified referrer].
        /// </summary>
        /// <param name="referrer">The referrer.</param>
        /// <returns>
        ///     <c>true</c> if [is search engine] [the specified referrer]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSearchEngine(string referrer)
        {
            var lower = referrer.ToUpperInvariant();
            return (lower.Contains("YAHOO") && lower.Contains("P=")) || (lower.Contains("?Q=") || lower.Contains("&Q="));
        }

        /// <summary>
        /// Determines whether the specified referrer is spam using HttpClient.
        /// </summary>
        /// <param name="referrer">The referrer Uri.</param>
        /// <param name="url">The URL Uri.</param>
        /// <returns>
        /// <c>true</c> if the specified referrer is spam; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSpam(Uri referrer, Uri url)
        {
            try
            {
                var html = _httpClient.GetStringAsync(referrer).GetAwaiter().GetResult();
                html = html.ToUpperInvariant();
                var subdomain = Utils.GetSubDomain(url);
                var host = url.Host.ToUpperInvariant();

                if (subdomain != null)
                {
                    host = host.Replace($"{subdomain.ToUpperInvariant()}.", string.Empty);
                }

                return !html.Contains(host);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Raises the event in a safe way
        /// </summary>
        /// <param name="referrer">The referrer.</param>
        private static void OnReferrerRegistered(Uri referrer)
        {
            ReferrerRegistered?.Invoke(referrer, EventArgs.Empty);
        }

        /// <summary>
        /// Registers click.
        /// </summary>
        /// <param name="url">The url Uri.</param>
        /// <param name="referrer">The referrer Uri.</param>
        private static void RegisterClick(Uri url, Uri referrer)
        {
            Referrer refer = null;
            if (Referrer.Referrers != null && Referrer.Referrers.Count > 0)
            {
                refer =
                    Referrer.Referrers.Find(
                        r => r.ReferrerUrl.Equals(referrer) && r.Url.Equals(url) && r.Day == DateTime.Today);
            }

            if (refer == null)
            {
                refer = new Referrer
                    {
                        Day = DateTime.Today,
                        ReferrerUrl = referrer,
                        Url = url,
                        PossibleSpam = IsSpam(referrer, url)
                    };
            }

            refer.Count += 1;

            refer.Save();
        }

        #endregion
    }
}
