namespace App_Code.Extensions
{
    using System;
    using System.IO;
    using System.Net.Http;

    using BlogEngine.Core;
    using BlogEngine.Core.Web.Controls;
    using BlogEngine.Core.Web.Extensions;

    /// <summary>
    /// StopForumSpam.com custom comment filter
    /// </summary>
    [Extension("Checks comment against stopforumspam.com spammers list.", "1.0", "BlogEngine.NET", 0, false)]
    public class StopForumSpam : ICustomFilter
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private bool passThrough = true;

        public bool FallThrough
        {
            get
            {
                return passThrough;
            }
        }

        public bool Check(Comment comment)
        {
            try
            {
                var url = $"http://www.stopforumspam.com/api?ip={comment.IP}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = _httpClient.Send(request);
                using var responseStream = response.Content.ReadAsStream();
                var reader = new StreamReader(responseStream);

                var value = reader.ReadToEnd();
                reader.Close();

                var spam = value.ToLowerInvariant().Contains("<appears>yes</appears>");

                // if comment IP appears in the stopforumspam list
                // it is for sure spam; no need to pass to others.
                passThrough = spam ? false : true;

                return spam;
            }
            catch (Exception e)
            {
                Utils.Log($"Error checking stopforumspam.com: {e.Message}");
                return false;
            }
        }

        public bool Initialize()
        {
            return true;
        }

        public void Report(Comment comment)
        {
            // if we needed report mistakes back to
            // service, we would put code here
        }
    }
}
