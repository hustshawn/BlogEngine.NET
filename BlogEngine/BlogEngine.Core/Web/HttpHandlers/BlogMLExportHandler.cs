namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Exports all posts to the BlogML xml format.
    /// </summary>
    public static class BlogMLExportEndpoint
    {
        /// <summary>
        /// Maps the BlogML export endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapBlogMLExport(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/blogml.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            if (Security.IsAdministrator)
            {
                context.Response.ContentType = "text/xml";
                context.Response.Headers.Append("Content-Disposition", "attachment; filename=BlogML.xml");
                WriteXml(context);
            }
            else
            {
                context.Response.StatusCode = 403;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Add authors.
        /// </summary>
        private static void AddAuthors(XmlWriter writer)
        {
            writer.WriteStartElement("authors");

            // REVIEW: Membership.GetAllUsers() needs IMembershipService — verify after auth migration
            // For now, using the Security/membership abstraction that exists in the project
            var users = Providers.BlogService.GetUserNames();
            if (users != null)
            {
                foreach (var userName in users)
                {
                    writer.WriteStartElement("author");

                    writer.WriteAttributeString("id", userName);
                    writer.WriteAttributeString(
                        "date-created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString(
                        "date-modified", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("approved", "true");
                    writer.WriteAttributeString("email", string.Empty);

                    writer.WriteStartElement("title");
                    writer.WriteAttributeString("type", "text");
                    writer.WriteCData(userName);
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add categories.
        /// </summary>
        private static void AddCategories(XmlWriter writer)
        {
            writer.WriteStartElement("categories");

            foreach (var category in Category.Categories)
            {
                writer.WriteStartElement("category");

                var parentId = "";
                if (category.Parent != null && category.Parent != Guid.Empty)
                    parentId = category.Parent.ToString();

                writer.WriteAttributeString("id", category.Id.ToString());
                writer.WriteAttributeString(
                    "date-created", category.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString(
                    "date-modified", category.DateModified.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("approved", "true");
                writer.WriteAttributeString("parentref", parentId);

                if (!String.IsNullOrEmpty(category.Description))
                {
                    writer.WriteAttributeString("description", category.Description);
                }

                writer.WriteStartElement("title");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(category.Title);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add extended properties.
        /// </summary>
        private static void AddExtendedProperties(XmlWriter writer)
        {
            writer.WriteStartElement("extended-properties");

            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", "CommentModeration");
            writer.WriteAttributeString("value", "Anonymous");
            writer.WriteEndElement();

            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", "SendTrackback");
            writer.WriteAttributeString("value", BlogSettings.Instance.EnableTrackBackSend ? "Yes" : "No");
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add post author.
        /// </summary>
        private static void AddPostAuthor(XmlWriter writer, Post post)
        {
            writer.WriteStartElement("authors");
            writer.WriteStartElement("author");
            writer.WriteAttributeString("ref", post.Author);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Add post categories.
        /// </summary>
        private static void AddPostCategories(XmlWriter writer, Post post)
        {
            if (post.Categories.Count == 0)
            {
                return;
            }

            writer.WriteStartElement("categories");
            foreach (var category in post.Categories)
            {
                writer.WriteStartElement("category");
                writer.WriteAttributeString("ref", category.Id.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add post comments.
        /// </summary>
        private static void AddPostComments(XmlWriter writer, Post post)
        {
            if (post.Comments.Count == 0)
            {
                return;
            }

            writer.WriteStartElement("comments");
            foreach (var comment in
                post.Comments.Where(comment => comment.Email != "trackback" && comment.Email != "pingback"))
            {
                writer.WriteStartElement("comment");
                writer.WriteAttributeString("id", comment.Id.ToString());
                writer.WriteAttributeString("parentid", comment.ParentId.ToString());
                writer.WriteAttributeString(
                    "date-created", comment.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString(
                    "date-modified", comment.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("approved", comment.IsApproved.ToString().ToLowerInvariant());
                writer.WriteAttributeString("user-name", comment.Author);
                writer.WriteAttributeString("user-email", comment.Email);
                writer.WriteAttributeString("user-ip", comment.IP);

                if (comment.Website != null)
                {
                    writer.WriteAttributeString("user-url", comment.Website.ToString());
                }
                else
                {
                    writer.WriteAttributeString("user-url", string.Empty);
                }

                writer.WriteStartElement("title");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData("re: " + post.Title);
                writer.WriteEndElement();

                writer.WriteStartElement("content");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(comment.Content);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Add post content.
        /// </summary>
        private static void AddPostContent(XmlWriter writer, Post post)
        {
            writer.WriteStartElement("content");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(post.Content);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the post excerpt.
        /// </summary>
        private static void AddPostExcerpt(XmlWriter writer, Post post)
        {
            if (String.IsNullOrEmpty(post.Description))
            {
                return;
            }

            writer.WriteStartElement("excerpt");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(post.Description);
            writer.WriteEndElement();
        }

        /// <summary>
        /// The post-name element contains the same as the title.
        /// </summary>
        private static void AddPostName(XmlWriter writer, Post post)
        {
            writer.WriteStartElement("post-name");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(post.Title);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the post tags.
        /// </summary>
        private static void AddPostTags(XmlWriter writer, Post post)
        {
            if (post.Tags.Count == 0)
            {
                return;
            }

            writer.WriteStartElement("tags");
            foreach (var tag in post.Tags)
            {
                writer.WriteStartElement("tag");
                writer.WriteAttributeString("ref", tag);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the post title.
        /// </summary>
        private static void AddPostTitle(XmlWriter writer, Post post)
        {
            writer.WriteStartElement("title");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(post.Title);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the post trackbacks.
        /// </summary>
        private static void AddPostTrackbacks(XmlWriter writer, Post post)
        {
            if (post.Comments.Count == 0)
            {
                return;
            }

            writer.WriteStartElement("trackbacks");
            foreach (var comment in
                post.Comments.Where(comment => comment.Email == "trackback" || comment.Email == "pingback"))
            {
                writer.WriteStartElement("trackback");
                writer.WriteAttributeString("id", comment.Id.ToString());
                writer.WriteAttributeString(
                    "date-created", comment.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString(
                    "date-modified", comment.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("approved", comment.IsApproved.ToString().ToLowerInvariant());

                if (comment.Website != null)
                {
                    writer.WriteAttributeString("url", comment.Website.ToString());
                }

                writer.WriteStartElement("title");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(comment.Content);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the posts.
        /// </summary>
        private static void AddPosts(XmlWriter writer)
        {
            writer.WriteStartElement("posts");

            foreach (var post in Post.Posts)
            {
                writer.WriteStartElement("post");

                writer.WriteAttributeString("id", post.Id.ToString());
                writer.WriteAttributeString(
                    "date-created", post.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString(
                    "date-modified", post.DateModified.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("approved", "true");
                writer.WriteAttributeString("post-url", post.RelativeLink);
                writer.WriteAttributeString("type", "normal");
                writer.WriteAttributeString(
                    "hasexcerpt", (!string.IsNullOrEmpty(post.Description)).ToString().ToLowerInvariant());
                writer.WriteAttributeString("views", "0");
                writer.WriteAttributeString("is-published", post.IsPublished.ToString());

                AddPostTitle(writer, post);
                AddPostContent(writer, post);
                AddPostName(writer, post);
                AddPostExcerpt(writer, post);
                AddPostAuthor(writer, post);
                AddPostCategories(writer, post);
                AddPostTags(writer, post);
                AddPostComments(writer, post);
                AddPostTrackbacks(writer, post);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void AddPages(XmlWriter writer, HttpContext context)
        {
            writer.WriteStartElement("posts");

            foreach (var post in Page.Pages)
            {
                writer.WriteStartElement("post");

                writer.WriteAttributeString("id", post.Id.ToString());
                writer.WriteAttributeString(
                    "date-created", post.DateCreated.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString(
                    "date-modified", post.DateModified.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("approved", "true");
                writer.WriteAttributeString("post-url", post.RelativeLink);
                writer.WriteAttributeString("type", "article");
                writer.WriteAttributeString(
                    "hasexcerpt", (!string.IsNullOrEmpty(post.Description)).ToString().ToLowerInvariant());
                writer.WriteAttributeString("views", "0");
                writer.WriteAttributeString("is-published", post.IsPublished.ToString());

                writer.WriteStartElement("title");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(post.Title);
                writer.WriteEndElement();

                writer.WriteStartElement("content");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(post.Content);
                writer.WriteEndElement();

                writer.WriteStartElement("post-name");
                writer.WriteAttributeString("type", "text");
                writer.WriteCData(post.Title);
                writer.WriteEndElement();

                writer.WriteStartElement("authors");
                writer.WriteStartElement("author");
                writer.WriteAttributeString("ref", context.User.Identity?.Name ?? string.Empty);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the sub title.
        /// </summary>
        private static void AddSubTitle(XmlWriter writer)
        {
            writer.WriteStartElement("sub-title");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(BlogSettings.Instance.Description);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds the title.
        /// </summary>
        private static void AddTitle(XmlWriter writer)
        {
            writer.WriteStartElement("title");
            writer.WriteAttributeString("type", "text");
            writer.WriteCData(BlogSettings.Instance.Name);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the BlogML to the output stream.
        /// </summary>
        private static void WriteXml(HttpContext context)
        {
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

            using (var writer = XmlWriter.Create(context.Response.Body, settings))
            {
                writer.WriteStartElement("blog", "http://www.blogml.com/2006/09/BlogML");
                writer.WriteAttributeString("root-url", Utils.RelativeWebRoot);
                writer.WriteAttributeString(
                    "date-created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("xmlns", "xs", null, "http://www.w3.org/2001/XMLSchema");

                AddTitle(writer);
                AddSubTitle(writer);
                AddAuthors(writer);
                AddExtendedProperties(writer);
                AddCategories(writer);
                AddPosts(writer);
                AddPages(writer, context);

                writer.WriteEndElement();
            }
        }
    }
}
