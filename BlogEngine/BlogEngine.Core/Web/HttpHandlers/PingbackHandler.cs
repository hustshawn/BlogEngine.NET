namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Receives pingbacks from other blogs and websites, and registers them as a comment.
    /// </summary>
    public static class PingbackEndpoint
    {
        #region Constants and Fields

        /// <summary>
        /// The success.
        /// </summary>
        private const string Success =
            "<methodResponse><params><param><value><string>Thanks!</string></value></param></params></methodResponse>";

        /// <summary>
        /// The regex html.
        /// </summary>
        private static readonly Regex RegexHtml =
            new Regex(
                @"</?\\w+((\\s+\\w+(\\s*=\\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\\s*|\\s*)/?>",
                RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// The regex title.
        /// </summary>
        private static readonly Regex RegexTitle = new Regex(
            @"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        #region Events

        /// <summary>
        ///     Occurs when a pingback is accepted as valid and added as a comment.
        /// </summary>
        public static event EventHandler<EventArgs> Accepted;

        /// <summary>
        ///     Occurs when a hit is made to the trackback.axd handler.
        /// </summary>
        public static event EventHandler<CancelEventArgs> Received;

        /// <summary>
        ///     Occurs when a pingback request is rejected because the sending
        ///     website already made a trackback or pingback to the specific page.
        /// </summary>
        public static event EventHandler<EventArgs> Rejected;

        /// <summary>
        ///     Occurs when the request comes from a spammer.
        /// </summary>
        public static event EventHandler<EventArgs> Spammed;

        #endregion

        /// <summary>
        /// Maps the pingback handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapPingbackHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/pingback.axd", HandleAsync);
            return endpoints;
        }

        /// <summary>
        /// Called when [spammed].
        /// </summary>
        public static void OnSpammed(string url)
        {
            Spammed?.Invoke(url, EventArgs.Empty);
        }

        private static async Task HandleAsync(HttpContext context)
        {
            if (!BlogSettings.Instance.IsCommentsEnabled || !BlogSettings.Instance.EnablePingBackReceive)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var e = new CancelEventArgs();
            OnReceived(e);
            if (e.Cancel)
            {
                return;
            }

            var doc = await RetrieveXmlDocumentAsync(context);
            if (doc == null) return;

            var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                       doc.SelectNodes("methodCall/params/param/value");

            if (list == null)
            {
                return;
            }

            var sourceUrl = list[0].InnerText.Trim();
            var targetUrl = list[1].InnerText.Trim();

            var (sourceHasLink, containsHtml, title) = ExamineSourcePage(sourceUrl, targetUrl);
            context.Response.ContentType = "text/xml";

            var post = GetPostByUrl(targetUrl);
            if (post != null)
            {
                if (IsFirstPingBack(post, sourceUrl))
                {
                    if (sourceHasLink && !containsHtml)
                    {
                        AddComment(sourceUrl, post, title);
                        OnAccepted(sourceUrl);
                        await context.Response.WriteAsync(Success);
                    }
                    else if (!sourceHasLink)
                    {
                        await SendErrorAsync(
                            context,
                            17,
                            "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                    }
                    else
                    {
                        OnSpammed(sourceUrl);

                        // Don't let spammers know we exist.
                        context.Response.StatusCode = 404;
                    }
                }
                else
                {
                    OnRejected(sourceUrl);
                    await SendErrorAsync(context, 48, "The pingback has already been registered.");
                }
            }
            else
            {
                await SendErrorAsync(context, 32, "The specified target URI does not exist.");
            }
        }

        private static void OnAccepted(string url)
        {
            Accepted?.Invoke(url, EventArgs.Empty);
        }

        private static void OnReceived(CancelEventArgs e)
        {
            Received?.Invoke(null, e);
        }

        private static void OnRejected(string url)
        {
            Rejected?.Invoke(url, EventArgs.Empty);
        }

        /// <summary>
        /// Parse the source URL to get the domain.
        /// </summary>
        private static string GetDomain(string sourceUrl)
        {
            var start = sourceUrl.IndexOf("://") + 3;
            var stop = sourceUrl.IndexOf("/", start);
            return sourceUrl.Substring(start, stop - start).Replace("www.", string.Empty);
        }

        /// <summary>
        /// Retrieve the post that belongs to the target URL.
        /// </summary>
        private static Post GetPostByUrl(string url)
        {
            var start = url.LastIndexOf("/") + 1;
            var stop = url.ToUpperInvariant().IndexOf(".ASPX");
            if (stop < 0) stop = url.Length;
            var name = url.Substring(start, stop - start).ToLowerInvariant();

            return (from post in Post.Posts
                    let legalTitle = Utils.RemoveIllegalCharacters(post.Title).ToLowerInvariant()
                    where name == legalTitle
                    select post).FirstOrDefault();
        }

        /// <summary>
        /// Checks to see if the source has already pinged the target.
        /// </summary>
        private static bool IsFirstPingBack(Post post, string sourceUrl)
        {
            foreach (var comment in post.Comments)
            {
                if (comment.Website != null &&
                    comment.Website.ToString().Equals(sourceUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (comment.IP != null && comment.IP == Utils.GetClientIP())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Retrieves the content of the input stream and returns it as an XmlDocument.
        /// </summary>
        private static async Task<XmlDocument> RetrieveXmlDocumentAsync(HttpContext context)
        {
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                var xml = await reader.ReadToEndAsync();
                if (!xml.Contains("<methodName>pingback.ping</methodName>"))
                {
                    context.Response.StatusCode = 404;
                    return null;
                }

                var doc = new XmlDocument() { XmlResolver = null };
                doc.LoadXml(xml);
                return doc;
            }
        }

        /// <summary>
        /// The send error.
        /// </summary>
        private static async Task SendErrorAsync(HttpContext context, int code, string message)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\"?>");
            sb.Append("<methodResponse>");
            sb.Append("<fault>");
            sb.Append("<value>");
            sb.Append("<struct>");
            sb.Append("<member>");
            sb.Append("<name>faultCode</name>");
            sb.AppendFormat("<value><int>{0}</int></value>", code);
            sb.Append("</member>");
            sb.Append("<member>");
            sb.Append("<name>faultString</name>");
            sb.AppendFormat("<value><string>{0}</string></value>", message);
            sb.Append("</member>");
            sb.Append("</struct>");
            sb.Append("</value>");
            sb.Append("</fault>");
            sb.Append("</methodResponse>");

            await context.Response.WriteAsync(sb.ToString());
        }

        /// <summary>
        /// Insert the pingback as a comment on the post.
        /// </summary>
        private static void AddComment(string sourceUrl, Post post, string title)
        {
            var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    Author = GetDomain(sourceUrl),
                    Website = new Uri(sourceUrl)
                };
            comment.Content = $"Pingback from {comment.Author}{Environment.NewLine}{Environment.NewLine}{title}";
            comment.DateCreated = DateTime.Now;
            comment.Email = "pingback";
            comment.IP = Utils.GetClientIP();
            comment.Parent = post;
            comment.IsApproved = true;
            post.AddComment(comment);
        }

        /// <summary>
        /// Parse the HTML of the source page.
        /// </summary>
        private static (bool sourceHasLink, bool containsHtml, string title) ExamineSourcePage(string sourceUrl, string targetUrl)
        {
            try
            {
                var remoteFile = new RemoteFile(new Uri(sourceUrl), true);
                var html = remoteFile.GetFileAsString();
                var title = RegexTitle.Match(html).Value.Trim();
                var containsHtml = RegexHtml.IsMatch(title);
                var sourceHasLink = html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());
                return (sourceHasLink, containsHtml, title);
            }
            catch (WebException)
            {
                return (false, false, string.Empty);
            }
        }
    }
}
