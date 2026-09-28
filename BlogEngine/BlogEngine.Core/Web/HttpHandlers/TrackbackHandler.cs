namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Trackback Handler Endpoint
    /// </summary>
    public static class TrackbackEndpoint
    {
        #region Constants and Fields

        /// <summary>
        /// The HTML Regex.
        /// </summary>
        private static readonly Regex RegexHtml =
            new Regex(
                @"</?\\w+((\\s+\\w+(\\s*=\\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\\s*|\\s*)/?>",
                RegexOptions.Singleline | RegexOptions.Compiled);

        #endregion

        #region Events

        /// <summary>
        ///     Occurs when a trackback is accepted as valid and added as a comment.
        /// </summary>
        public static event EventHandler<EventArgs> Accepted;

        /// <summary>
        ///     Occurs when a hit is made to the trackback.axd handler.
        /// </summary>
        public static event EventHandler<CancelEventArgs> Received;

        /// <summary>
        ///     Occurs when a trackback request is rejected because the sending
        ///     website already made a trackback or pingback to the specific page.
        /// </summary>
        public static event EventHandler<EventArgs> Rejected;

        /// <summary>
        ///     Occurs when the request comes from a spammer.
        /// </summary>
        public static event EventHandler<EventArgs> Spammed;

        #endregion

        /// <summary>
        /// Maps the trackback handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapTrackbackHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/trackback.axd", HandleAsync);
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
            if (!BlogSettings.Instance.IsCommentsEnabled || !BlogSettings.Instance.EnableTrackBackReceive)
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

            var form = await context.Request.ReadFormAsync();
            var postId = form["id"].ToString();
            if (string.IsNullOrEmpty(postId))
                postId = context.Request.Query["id"].ToString();

            var title = form["title"].ToString();
            if (string.IsNullOrEmpty(title))
                title = context.Request.Query["title"].ToString();

            var excerpt = form["excerpt"].ToString();
            if (string.IsNullOrEmpty(excerpt))
                excerpt = context.Request.Query["excerpt"].ToString();

            var blogName = form["blog_name"].ToString();
            if (string.IsNullOrEmpty(blogName))
                blogName = context.Request.Query["blog_name"].ToString();

            var url = string.Empty;
            var urlParam = form["url"].ToString();
            if (string.IsNullOrEmpty(urlParam))
                urlParam = context.Request.Query["url"].ToString();

            if (!string.IsNullOrEmpty(urlParam))
            {
                url = urlParam.Split(',')[0];
            }

            Post post;

            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(postId) && !string.IsNullOrEmpty(blogName) &&
                postId.Length == 36)
            {
                post = Post.GetPost(new Guid(postId));
                var sourceHasLink = ExamineSourcePage(url, post.AbsoluteLink.AbsoluteUri);
                var containsHtml = !string.IsNullOrEmpty(excerpt) &&
                                   (RegexHtml.IsMatch(excerpt) || RegexHtml.IsMatch(title) ||
                                    RegexHtml.IsMatch(blogName));

                if (IsFirstPingBack(post, url) && sourceHasLink && !containsHtml)
                {
                    AddComment(url, post, blogName, title, excerpt);
                    OnAccepted(url);
                    await context.Response.WriteAsync(
                        "<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><response><error>0</error></response>");
                    return;
                }
                else if (!IsFirstPingBack(post, url))
                {
                    OnRejected(url);
                    await context.Response.WriteAsync(
                        "<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><response><error>Trackback already registered</error></response>");
                    return;
                }
                else if (!sourceHasLink || containsHtml)
                {
                    OnSpammed(url);
                    await context.Response.WriteAsync(
                        "<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><response><error>The source page does not link</error></response>");
                    return;
                }
            }
            else
            {
                context.Response.Redirect(Utils.RelativeWebRoot);
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
        /// Insert the trackback as a comment on the post.
        /// </summary>
        private static void AddComment(string sourceUrl, Post post, string blogName, string title, string excerpt)
        {
            var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    Author = blogName,
                    Website = new Uri(sourceUrl),
                    Content = title + Environment.NewLine + Environment.NewLine + excerpt,
                    Email = "trackback",
                    Parent = post,
                    DateCreated = DateTime.Now,
                    IP = Utils.GetClientIP(),
                    IsApproved = true
                };
            post.AddComment(comment);
        }

        /// <summary>
        /// Checks to see if the source has already pinged the target.
        /// </summary>
        private static bool IsFirstPingBack(Post post, string sourceUrl)
        {
            return
                !post.Comments.Any(
                    comment =>
                        comment.Website != null &&
                        comment.Website.ToString().Equals(sourceUrl, StringComparison.OrdinalIgnoreCase)) &&
                !post.Comments.Any(
                    comment =>
                        comment.IP != null &&
                        comment.IP == Utils.GetClientIP());
        }

        /// <summary>
        /// Parse the HTML of the source page.
        /// </summary>
        private static bool ExamineSourcePage(string sourceUrl, string targetUrl)
        {
            try
            {
                var remoteFile = new RemoteFile(new Uri(sourceUrl), true);
                var html = remoteFile.GetFileAsString();
                return html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}
