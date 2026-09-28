/* Author:      Joel Thoms (http://joel.net)
 * Copyright:   2006 Joel Thoms (http://joel.net)
 * About:       Akismet (http://akismet.com) .Net 2.0 API allow you to check
 *              Akismet's spam database to verify your comments and prevent spam.
*/

namespace Joel.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    
    #region - public class AkismetComment -

    /// <summary>
    /// The akismet comment.
    /// </summary>
    public class AkismetComment
    {
        public string Blog { get; set; }
        public string CommentAuthor { get; set; }
        public string CommentAuthorEmail { get; set; }
        public string CommentAuthorUrl { get; set; }
        public string CommentContent { get; set; }
        public string CommentType { get; set; }
        public string Permalink { get; set; }
        public string Referrer { get; set; }
        public string UserAgent { get; set; }
        public string UserIp { get; set; }
    }

    #endregion

    /// <summary>
    /// The akismet.
    /// </summary>
    public class Akismet
    {
        private readonly string apiKey;
        private readonly string blog;
        private readonly string commentCheckUrl = "http://{0}.rest.akismet.com/1.1/comment-check";
        private readonly string submitHamUrl = "http://{0}.rest.akismet.com/1.1/submit-ham";
        private readonly string submitSpamUrl = "http://{0}.rest.akismet.com/1.1/submit-spam";
        private readonly string userAgent = "Joel.Net's Akismet API/1.0";
        private readonly string verifyUrl = "http://rest.akismet.com/1.1/verify-key";
        private static readonly HttpClient _httpClient = new HttpClient();

        public Akismet(string apiKey, string blog, string userAgent)
        {
            this.CharSet = "UTF-8";
            this.apiKey = apiKey;
            if (userAgent != null)
            {
                this.userAgent = $"{userAgent} | Akismet/1.11";
            }

            this.blog = blog;
        }

        public Akismet(string apiKey, string blog, string userAgent, string serviceUrl)
            : this(apiKey, blog, userAgent)
        {
            this.verifyUrl = string.Format("http://{0}/1.1/verify-key", serviceUrl);
            this.commentCheckUrl = string.Format("http://{{0}}.{0}/1.1/comment-check", serviceUrl);
            this.submitSpamUrl = string.Format("http://{{0}}.{0}/1.1/submit-spam", serviceUrl);
            this.submitHamUrl = string.Format("http://{{0}}.{0}/1.1/submit-ham", serviceUrl);
        }

        public string CharSet { get; set; }

        public bool CommentCheck(AkismetComment comment)
        {
            var value =
                Convert.ToBoolean(
                    this.HttpPost(String.Format(this.commentCheckUrl, this.apiKey), CreateData(comment), this.CharSet));
            return value;
        }

        public void SubmitHam(AkismetComment comment)
        {
            this.HttpPost(String.Format(this.submitHamUrl, this.apiKey), CreateData(comment), this.CharSet);
        }

        public void SubmitSpam(AkismetComment comment)
        {
            this.HttpPost(String.Format(this.submitSpamUrl, this.apiKey), CreateData(comment), this.CharSet);
        }

        public bool VerifyKey()
        {
            var response = this.HttpPost(
                this.verifyUrl, 
                String.Format("key={0}&blog={1}", this.apiKey, Uri.EscapeDataString(this.blog)), 
                this.CharSet);
            var value = (response == "valid") ? true : false;
            return value;
        }

        private static string CreateData(AkismetComment comment)
        {
            var value =
                String.Format(
                    "blog={0}&user_ip={1}&user_agent={2}&referrer={3}&permalink={4}&comment_type={5}" +
                    "&comment_author={6}&comment_author_email={7}&comment_author_url={8}&comment_content={9}", 
                    Uri.EscapeDataString(comment.Blog ?? ""), 
                    Uri.EscapeDataString(comment.UserIp ?? ""), 
                    Uri.EscapeDataString(comment.UserAgent ?? ""), 
                    Uri.EscapeDataString(comment.Referrer ?? ""), 
                    Uri.EscapeDataString(comment.Permalink ?? ""), 
                    Uri.EscapeDataString(comment.CommentType ?? ""), 
                    Uri.EscapeDataString(comment.CommentAuthor ?? ""), 
                    Uri.EscapeDataString(comment.CommentAuthorEmail ?? ""), 
                    Uri.EscapeDataString(comment.CommentAuthorUrl ?? ""), 
                    Uri.EscapeDataString(comment.CommentContent ?? ""));

            return value;
        }

        private string HttpPost(string url, string data, string charset)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(data, System.Text.Encoding.GetEncoding(charset), "application/x-www-form-urlencoded");
                request.Headers.TryAddWithoutValidation("User-Agent", this.userAgent);
                
                var response = _httpClient.Send(request);
                using var reader = new StreamReader(response.Content.ReadAsStream());
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

namespace App_Code.Extensions
{
    using System;
    using BlogEngine.Core;
    using BlogEngine.Core.Web.Controls;
    using BlogEngine.Core.Web.Extensions;
    using System.Collections.Generic;
    using Joel.Net;

    /// <summary>
    /// Akismet Filter
    /// </summary>
    [Extension("Akismet anti-spam comment filter", "1.0", "<a href=\"https://blogengine.io\">BlogEngine.NET</a>", 0, false)]
    public class AkismetFilter : ICustomFilter
    {
        private static readonly object syncRoot = new object();
        private static Dictionary<Guid, Akismet> blogsApi = new Dictionary<Guid, Akismet>();
        private static Dictionary<Guid, string> blogsKey = new Dictionary<Guid, string>();
        private static Dictionary<Guid, ExtensionSettings> blogsSettings = new Dictionary<Guid, ExtensionSettings>();
        private static Dictionary<Guid, string> blogsSite = new Dictionary<Guid, string>();

        public AkismetFilter()
        {
            this.InitSettings();
        }

        private static Akismet Api
        {
            get
            {
                blogsApi.TryGetValue(Blog.CurrentInstance.Id, out var akismet);
                return akismet;
            }
            set => blogsApi[Blog.CurrentInstance.Id] = value;
        }

        private static string Key
        {
            get
            {
                blogsKey.TryGetValue(Blog.CurrentInstance.Id, out var key);
                return key;
            }
            set => blogsKey[Blog.CurrentInstance.Id] = value;
        }

        private static string Site
        {
            get
            {
                blogsSite.TryGetValue(Blog.CurrentInstance.Id, out var key);
                return key;
            }
            set => blogsSite[Blog.CurrentInstance.Id] = value;
        }

        private static ExtensionSettings Settings
        {
            get
            {
                Guid blogId = Blog.CurrentInstance.Id;
                blogsSettings.TryGetValue(blogId, out var settings);

                if (settings == null)
                {
                    lock (syncRoot)
                    {
                        blogsSettings.TryGetValue(blogId, out settings);

                        if (settings == null)
                        {
                            var extensionSettings = new ExtensionSettings("AkismetFilter") { IsScalar = true };

                            extensionSettings.AddParameter("SiteURL", "Site URL");
                            extensionSettings.AddParameter("ApiKey", "API Key");

                            extensionSettings.AddValue("SiteURL", "http://example.com/blog");
                            extensionSettings.AddValue("ApiKey", "123456789");

                            blogsSettings[blogId] = ExtensionManager.InitSettings("AkismetFilter", extensionSettings);
                            ExtensionManager.SetStatus("AkismetFilter", false);
                        }
                    }
                }

                return settings;
            }
        }

        public bool FallThrough { get; private set; }

        public bool Check(Comment comment)
        {
            if (Api == null)
            {
                this.Initialize();
            }

            if (Settings == null)
            {
                this.InitSettings();
            }

            var akismetComment = GetAkismetComment(comment);
            var spam = Api.CommentCheck(akismetComment);
            this.FallThrough = !spam;
            return spam;
        }

        public bool Initialize()
        {
            if (!ExtensionManager.ExtensionEnabled("AkismetFilter"))
            {
                return false;
            }

            if (Settings == null)
            {
                this.InitSettings();
            }

            Site = Settings.GetSingleValue("SiteURL");
            Key = Settings.GetSingleValue("ApiKey");
            Api = new Akismet(Key, Site, $"BlogEngine.NET {BlogSettings.Instance.Version()}");

            return Api.VerifyKey();
        }

        public void Report(Comment comment)
        {
            if (Api == null && !this.Initialize())
            {
                return;
            }

            if (Settings == null)
            {
                this.InitSettings();
            }

            var akismetComment = GetAkismetComment(comment);

            if (comment.IsApproved)
            {
                Utils.Log(
                    $"Akismet: Reporting NOT spam from \"{comment.Author}\" at \"{comment.IP}\"");
                Api.SubmitHam(akismetComment);
            }
            else
            {
                Utils.Log($"Akismet: Reporting SPAM from \"{comment.Author}\" at \"{comment.IP}\"");
                Api.SubmitSpam(akismetComment);
            }
        }

        private static AkismetComment GetAkismetComment(Comment comment)
        {
            var akismetComment = new AkismetComment
                {
                    Blog = Settings.GetSingleValue("SiteURL"), 
                    UserIp = comment.IP, 
                    CommentContent = comment.Content, 
                    CommentType = "comment", 
                    CommentAuthor = comment.Author, 
                    CommentAuthorEmail = comment.Email
                };
            if (comment.Website != null)
            {
                akismetComment.CommentAuthorUrl = comment.Website.OriginalString;
            }

            return akismetComment;
        }

        private void InitSettings()
        {
            var s = Settings;
        }
    }
}
