namespace BlogEngine.Core
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Xml;

    /// <summary>
    /// Derived XmlUrlResolver class designed to prevent security problems with
    /// dangerous XML input, by limiting the amount of data that can be retrieved.
    /// Converted from WebRequest.Create to HttpClient.
    /// </summary>
    public class XmlSafeResolver : XmlUrlResolver
    {
        #region Constants and Fields

        /// <summary>
        /// The buffer size. 1 KB.
        /// </summary>
        private const int BufferSize = 1024;

        /// <summary>
        /// The max response size. 1 MB.
        /// </summary>
        private const int MaxResponseSize = 1024 * 1024;

        /// <summary>
        /// The timeout. 10 seconds.
        /// </summary>
        private const int Timeout = 10000;

        /// <summary>
        /// Static HttpClient instance for XML resolution.
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        })
        {
            Timeout = TimeSpan.FromMilliseconds(Timeout)
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Maps a URI to an object containing the actual resource.
        /// </summary>
        /// <param name="absoluteUri">The URI returned from <see cref="M:System.Xml.XmlResolver.ResolveUri(System.Uri,System.String)"/></param>
        /// <param name="role">The current implementation does not use this parameter when resolving URIs.</param>
        /// <param name="typeOfObjectToReturn">The type of object to return. The current implementation only returns System.IO.Stream objects.</param>
        /// <returns>A System.IO.Stream object or null if a type other than stream is specified.</returns>
        public override object GetEntity(Uri absoluteUri, string role, Type typeOfObjectToReturn)
        {
            if (absoluteUri.IsLoopback)
            {
                return null;
            }

            Stream responseStream;
            try
            {
                responseStream = _httpClient.GetStreamAsync(absoluteUri).GetAwaiter().GetResult();
                if (responseStream == null)
                {
                    Utils.Log($"Could not resolve external entity ({absoluteUri})");
                    return null;
                }
            }
            catch (Exception)
            {
                Utils.Log($"Could not resolve external entity ({absoluteUri})");
                return null;
            }

            var copyStream = new MemoryStream();
            var buffer = new byte[BufferSize];
            int bytesRead;
            var totalBytesRead = 0;
            do
            {
                bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;
                if (totalBytesRead > MaxResponseSize)
                {
                    throw new XmlException("Could not resolve external entity");
                }

                copyStream.Write(buffer, 0, bytesRead);
            }
            while (bytesRead > 0);

            responseStream.Dispose();

            copyStream.Seek(0, SeekOrigin.Begin);
            return copyStream;
        }

        #endregion
    }
}
