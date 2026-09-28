namespace BlogEngine.Core.Web.Controls
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Net;

    /// <summary>
    /// POCO/service class for post rendering logic.
    /// Converted from UserControl inheritance to a plain class.
    /// Preserves all the post rendering methods and properties.
    /// </summary>
    public class PostViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostViewBase"/> class.
        /// </summary>
        public PostViewBase()
        {
            Location = ServingLocation.None;
            ContentBy = ServingContentBy.Unspecified;
        }

        #region Properties

        /// <summary>
        ///     Gets or sets whether the control is visible.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        ///     Gets the body of the post. Important: use this instead of Post.Content.
        /// </summary>
        public string Body
        {
            get
            {
                var post = Post;
                var body = post.Content;

                if (Blog.CurrentInstance.IsSiteAggregation)
                {
                    body = Utils.ConvertPublishablePathsToAbsolute(body, post);
                }

                if (ShowExcerpt)
                {
                    var link = $" <a href=\"{post.RelativeLink}\">[{Utils.Translate("more")}]</a>";

                    if (!string.IsNullOrEmpty(post.Description))
                    {
                        body = post.Description.Replace(Environment.NewLine, "<br />") + link;
                    }
                    else
                    {
                        body = Utils.StripHtml(body);
                        if (body.Length > DescriptionCharacters && DescriptionCharacters > 0)
                        {
                            body = $"{body.Substring(0, DescriptionCharacters)}...{link}";
                        }
                    }
                }

                var arg = new ServingEventArgs(body, Location, ContentBy);
                Post.OnServing(post, arg);

                if (arg.Cancel)
                {
                    if (arg.Location == ServingLocation.SinglePost)
                    {
                        // REVIEW: In the original, this redirected to ~/error404.aspx.
                        // Callers should check Body/Visible and handle the redirect.
                        Visible = false;
                    }
                    else
                    {
                        Visible = false;
                    }
                }

                return arg.Body ?? string.Empty;
            }
        }

        /// <summary>
        ///     Gets the comment feed link.
        /// </summary>
        /// <value>The comment feed.</value>
        public string CommentFeed
        {
            get
            {
                return Post.RelativeLink.Replace("/post/", "/post/feed/");
            }
        }

        /// <summary>
        ///     Gets or sets the criteria by which the content is being served (by tag, category, author, etc).
        /// </summary>
        public ServingContentBy ContentBy { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating how many characters should be shown of the description.
        /// </summary>
        public int DescriptionCharacters
        {
            get
            {
                int chars = 0;
                // REVIEW: behavior inferred from usage — the original used Blog._httpContextAccessor?.HttpContext.Request.RawUrl.
                // Callers should set the RequestUrl property to enable URL-based logic.
                string url = (RequestUrl ?? string.Empty).ToUpperInvariant();

                if (url.Contains("/CATEGORY/") || url.Contains("?TAG=/"))
                {
                    if (BlogSettings.Instance.ShowDescriptionInPostListForPostsByTagOrCategory)
                    {
                        return BlogSettings.Instance.DescriptionCharactersForPostsByTagOrCategory;
                    }
                }
                else
                {
                    if (BlogSettings.Instance.ShowDescriptionInPostList)
                    {
                        return BlogSettings.Instance.DescriptionCharacters;
                    }
                }
                return chars;
            }
        }

        /// <summary>
        /// Gets or sets the current request URL for URL-based logic.
        /// Set this from the caller to replace Blog._httpContextAccessor?.HttpContext.Request.RawUrl.
        /// </summary>
        public string RequestUrl { get; set; }

        /// <summary>
        ///     Gets or sets the index of the post in a list of posts displayed
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     Gets or sets the location where the serving takes place.
        /// </summary>
        public ServingLocation Location { get; set; }

        /// <summary>
        ///     Gets or sets the Post object that is displayed through the PostView.ascx control.
        /// </summary>
        /// <value>The Post object that has to be displayed.</value>
        public Post Post { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether or not to show the entire post or just the excerpt/description.
        /// </summary>
        public bool ShowExcerpt { get; set; }

        /// <summary>
        ///     Gets an Edit and Delete link to any authenticated user.
        /// </summary>
        public virtual string AdminLinks
        {
            get
            {
                if (!Security.IsAuthenticated)
                {
                    return string.Empty;
                }

                if (Blog.CurrentInstance.IsSiteAggregation)
                {
                    // only can edit own posts, not from aggregated blogs
                    if (Post.BlogId != Blog.CurrentInstance.BlogId)
                    {
                        return string.Empty;
                    }
                }

                var postRelativeLink = Post.RelativeLink;
                var sb = new StringBuilder();

                if (Security.IsAuthorizedTo(Rights.ModerateComments))
                {
                    if (Post.NotApprovedComments.Count > 0 &&
                        BlogSettings.Instance.CommentProvider == BlogSettings.CommentsBy.BlogEngine)
                    {
                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "<a href=\"{0}\">{1} ({2})</a> | ",
                            postRelativeLink,
                            Utils.Translate("unapprovedcomments"),
                            Post.NotApprovedComments.Count);
                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "<a href=\"{0}\">{1}</a> | ",
                            postRelativeLink + "?approveallcomments=true",
                            Utils.Translate("approveallcomments"));
                    }
                }

                if (Post.CanUserEdit)
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "<a href=\"{0}\">{1}</a> | ",
                    Post.Blog.AbsoluteWebRoot + "admin/app/editor/editpost.cshtml?id=" + Post.Id,
                        Utils.Translate("edit"));
                }

                if (Post.CanUserDelete)
                {
                    var confirmDelete = string.Format(
                            CultureInfo.InvariantCulture,
                            Utils.Translate("areYouSure"),
                            Utils.Translate("delete").ToLowerInvariant(),
                            Utils.Translate("thePost"));

                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "<a href=\"#\" onclick=\"if (confirm('{2}')) location.href='{0}?deletepost={1}'\">{3}</a> | ",
                        postRelativeLink,
                        Post.Id,
                        WebUtility.HtmlEncode(confirmDelete),
                        Utils.Translate("delete"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        ///     Gets the rating.
        /// Enable visitors to rate the post.
        /// </summary>
        public virtual string Rating
        {
            get
            {
                if (!BlogSettings.Instance.EnableRating || !Security.IsAuthorizedTo(AuthorizationCheck.HasAll, Rights.ViewRatingsOnPosts, Rights.SubmitRatingsOnPosts))
                {
                    return string.Empty;
                }

                const string Script = "<div class=\"ratingcontainer\" style=\"visibility:hidden\">{0}|{1}|{2}|{3}</div>";
                return string.Format(
                    Script,
                    Post.Id,
                    Post.Raters,
                    Post.Rating.ToString("#.0", CultureInfo.InvariantCulture),
                    Post.BlogId);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays the Post's categories separated by the specified string.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <returns>The category links.</returns>
        public virtual string CategoryLinks(string separator)
        {
            var keywords = new string[Post.Categories.Count];
            const string Link = "<a href=\"{0}\">{1}</a>";
            for (var i = 0; i < Post.Categories.Count; i++)
            {
                Category c = Post.Categories[i];
                keywords[i] = string.Format(CultureInfo.InvariantCulture, Link, c.RelativeOrAbsoluteLink, c.Title);
            }

            return string.Join(separator, keywords);
        }

        /// <summary>
        /// Displays the Post's tags separated by the specified string.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <returns>The tag links.</returns>
        public virtual string TagLinks(string separator)
        {
            var tags = Post.Tags;
            if (tags.Count == 0)
            {
                return null;
            }

            var tagStrings = new string[tags.Count];
            const string Link = "<a href=\"{0}?tag={1}\" rel=\"tag\">{2}</a>";
            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                tagStrings[i] = string.Format(
                    CultureInfo.InvariantCulture, Link, Post.Blog.AbsoluteWebRoot,
                    WebUtility.UrlEncode(tag), WebUtility.HtmlEncode(tag));
            }

            return string.Join(separator, tagStrings);
        }

        #endregion
    }
}
