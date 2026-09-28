using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.IO;
using System.Threading.Tasks;

namespace BlogEngine.Core
{
    /// <summary>
    /// Class used to download files from a website address.
    /// Converted from HttpWebRequest to static HttpClient with SocketsHttpHandler.
    /// </summary>
    /// <remarks>
    /// 
    /// The purpose of this class is so there's one core way of downloading remote files with urls that are from
    /// outside users. There's various areas in BlogEngine where an attacker could supply an external url to the server
    /// and tie up resources.
    /// 
    /// RemoteFile makes use of two BlogSettings properties: AllowServerToDownloadRemoteFiles and RemoteFileDownloadTimeout.
    /// 
    /// This class will not throw an exception if the Uri supplied points to a resource local to the running BlogEngine instance.
    /// 
    /// </remarks>
    internal sealed class RemoteFile
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            DefaultRequestHeaders =
            {
                { "Accept-Encoding", "gzip" },
                { "Accept-Language", "en-us" },
                { "User-Agent", "Mozilla / 5.0(Windows NT 6.2; WOW64; rv: 40.0) Gecko / 20100101 Firefox / 40.0" }
            }
        };

        /// <summary>
        /// Creates a new RemoteFile instance that can be used to download files from another server.
        /// </summary>
        /// <param name="filePath">The url of the file to be downloaded.</param>
        /// <param name="ignoreRemoteDownloadSettings">Set to true if RemoteFile should ignore the current BlogEngine instance's remote download settings.</param>
        internal RemoteFile(Uri filePath, bool ignoreRemoteDownloadSettings)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this.url = filePath;
            this._ignoreRemoteDownloadSettings = ignoreRemoteDownloadSettings;
            this._timeoutLength = BlogSettings.Instance.RemoteFileDownloadTimeout;
        }


        #region "Public Methods"

        /// <summary>
        /// Returns the HttpResponseMessage used to download this file.
        /// </summary>
        /// <returns>An HttpResponseMessage, or null on failure.</returns>
        /// <remarks>
        /// This method is meant for outside users who need specific access to the response.
        /// They're responsible for disposing of it.
        /// </remarks>
        public HttpResponseMessage GetWebResponse()
        {
            HttpResponseMessage response;
            try
            {
                CheckCanDownload();

                using var cts = CreateTimeoutCts();
                var request = new HttpRequestMessage(HttpMethod.Get, this.Uri);
                response = _httpClient.SendAsync(request, cts?.Token ?? default).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return null;
            }

            long contentLength = response.Content.Headers.ContentLength ?? -1;

            // -1 means an unknown ContentLength was found
            if (contentLength <= -1)
            {
                response.Dispose();
                return null;
            }
            else if ((BlogSettings.Instance.RemoteMaxFileSize > 0) && (contentLength > BlogSettings.Instance.RemoteMaxFileSize))
            {
                response.Dispose();
                Utils.Log("An attempt to download a remote file has been halted because the file is larger than allowed.");
                return null;
            }

            return response;
        }

        /// <summary>
        /// Returns the remote file as a string.
        /// </summary>
        /// <returns>The file content as a string.</returns>
        public string GetFileAsString()
        {
            using (var response = this.GetWebResponse())
            {
                if (response == null)
                    return string.Empty;

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Returns the remote file as a string asynchronously.
        /// </summary>
        /// <returns>The file content as a string.</returns>
        public async Task<string> GetFileAsStringAsync()
        {
            using var response = GetWebResponse();
            if (response == null)
                return string.Empty;

            return await response.Content.ReadAsStringAsync();
        }

        #endregion

        #region "Private Methods"

        private void CheckCanDownload()
        {
            if (!this.IgnoreRemoteDownloadSettings && !BlogSettings.Instance.AllowServerToDownloadRemoteFiles)
            {
                if (!this.UriPointsToLocalResource)
                {
                    throw new SecurityException("BlogEngine is not configured to allow remote file downloads.");
                }
            }
        }

        private System.Threading.CancellationTokenSource CreateTimeoutCts()
        {
            if (this.TimeoutLength > 0)
            {
                var cts = new System.Threading.CancellationTokenSource();
                cts.CancelAfter(this.TimeoutLength);
                return cts;
            }
            return null;
        }

         #endregion


        #region "Properties"

        #region "IgnoreRemoteDownloadSettings"

        private readonly bool _ignoreRemoteDownloadSettings;

        /// <summary>
        /// Gets whether this RemoteFile instance is ignoring remote download rules set in the current BlogSettings instance.
        /// </summary>
        public bool IgnoreRemoteDownloadSettings
        {
            get
            {
                return this._ignoreRemoteDownloadSettings;
            }
        }


        #endregion

        /// <summary>
        /// Gets whether the Uri property is pointed at a resource local to the running BlogEngine instance.
        /// </summary>
        public bool UriPointsToLocalResource
        {
            get
            {
                return (this.Uri.AbsoluteUri.StartsWith(Utils.AbsoluteWebRoot.AbsoluteUri));
            }
        }

        #region "Uri"
        private readonly Uri url;

        /// <summary>
        /// Gets the Uri of the remote file being downloaded.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return this.url;
            }
        }

        #endregion

        #region "TimeoutLength"

        private int _timeoutLength;

        /// <summary>
        /// Gets or sets the length of time, in milliseconds, that a remote file download attempt can last before timing out.
        /// </summary>
        public int TimeoutLength
        {
            get
            {
                return (this.IgnoreRemoteDownloadSettings ? this._timeoutLength : BlogSettings.Instance.RemoteFileDownloadTimeout);
            }
            set
            {
                if (!this.IgnoreRemoteDownloadSettings)
                {
                    throw new SecurityException("TimeoutLength can not be adjusted on RemoteFiles that are abiding by remote download rules");
                }
                else
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException("TimeoutLength must be a value greater than or equal to 0 milliseconds");
                    }
                    else
                    {
                        this._timeoutLength = value;
                    }

                }
            }
        }

        #endregion

        #endregion

    }
}
