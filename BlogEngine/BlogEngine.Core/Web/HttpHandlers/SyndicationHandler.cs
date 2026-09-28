namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    using BlogEngine.Core.Web.HttpModules;

    /// <summary>
    /// Implements a custom endpoint to process HTTP Web requests for a syndication feed.
    /// </summary>
    public static class SyndicationEndpoint
    {
        /// <summary>
        /// Maps the syndication feed endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapSyndication(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/syndication.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            if (!Security.IsAuthorizedTo(Rights.ViewPublicPosts))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var title = RetrieveTitle(context);
            var format = RetrieveFormat(context);
            var list = await GenerateItemListAsync(context);
            if (list == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            list = CleanList(list);

            if (string.IsNullOrEmpty(context.Request.Query["post"].ToString()))
            {
                // Shorten the list to the number of posts stated in the settings, except for the comment feed.
                var max = BlogSettings.Instance.PostsPerFeed;

                // usually we want to restrict number of posts for subscribers to latest
                // but it can be overriden in query string to bring any number of items
                var maxItemsStr = context.Request.Query["maxitems"].ToString();
                if (!string.IsNullOrEmpty(maxItemsStr))
                {
                    int maxItems;
                    if (int.TryParse(maxItemsStr, out maxItems))
                        max = maxItems;
                }

                list = list.FindAll(item => item.IsVisible).Take(Math.Min(max, list.Count)).ToList();
            }

            SetHeaderInformation(context, list, format);

            if (BlogSettings.Instance.EnableHttpCompression)
            {
                // Compression handled by ResponseCompression middleware
            }

            var generator = new SyndicationGenerator(BlogSettings.Instance, Category.Categories);
            generator.WriteFeed(format, context.Response.Body, list, title);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleans the list.
        /// </summary>
        private static List<IPublishable> CleanList(List<IPublishable> list)
        {
            return list.FindAll(item => item.IsVisible);
        }

        /// <summary>
        /// A converter delegate used for converting Results to Posts.
        /// </summary>
        private static IPublishable ConvertToIPublishable(IPublishable item)
        {
            return item;
        }

        /// <summary>
        /// Generates the list of feed items based on the URL.
        /// </summary>
        private static async Task<List<IPublishable>> GenerateItemListAsync(HttpContext context)
        {
            var categoryQuery = context.Request.Query["category"].ToString();
            if (!string.IsNullOrEmpty(categoryQuery))
            {
                if (categoryQuery.Length < 36)
                    return null;
                var categoryId = new Guid(categoryQuery.Substring(0, 36));
                return Post.GetPostsByCategory(categoryId).ConvertAll(ConvertToIPublishable);
            }

            var tagQuery = context.Request.Query["tag"].ToString();
            if (!string.IsNullOrEmpty(tagQuery))
            {
                return Post.GetPostsByTag(tagQuery.Trim()).ConvertAll(ConvertToIPublishable);
            }

            var authorQuery = context.Request.Query["author"].ToString();
            if (!string.IsNullOrEmpty(authorQuery))
            {
                return Post.GetPostsByAuthor(authorQuery).ConvertAll(ConvertToIPublishable);
            }

            var postQuery = context.Request.Query["post"].ToString();
            if (!string.IsNullOrEmpty(postQuery))
            {
                var post = Post.GetPost(new Guid(postQuery));
                return post.Comments.ConvertAll(ConvertToIPublishable);
            }

            var commentsQuery = context.Request.Query["comments"].ToString();
            if (!string.IsNullOrEmpty(commentsQuery))
            {
                return RecentComments();
            }

            var searchQuery = context.Request.Query["q"].ToString();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                return Search.Hits(searchQuery, false);
            }

            var apmlQuery = context.Request.Query["apml"].ToString();
            if (!string.IsNullOrEmpty(apmlQuery))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var content = await httpClient.GetStringAsync(apmlQuery);
                        var doc = new XmlDocument() { XmlResolver = null };
                        doc.LoadXml(content);

                        var list = Search.ApmlMatches(doc, 30);
                        list.Sort((i1, i2) => i2.DateCreated.CompareTo(i1.DateCreated));
                        return list;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.Headers.Append("Content-Disposition", "inline; filename=\"error.txt\"");
                    await context.Response.WriteAsync(ex.Message);
                    return null;
                }
            }

            // The latest posts
            return Post.ApplicablePosts.ConvertAll(ConvertToIPublishable);
        }

        /// <summary>
        /// Creates a list of the most recent comments
        /// </summary>
        private static List<IPublishable> RecentComments()
        {
            var temp = Post.ApplicablePosts.SelectMany(post => post.ApprovedComments).ToList();

            temp.Sort();
            temp.Reverse();
            var list = temp.ToList();

            return list.ConvertAll(ConvertToIPublishable);
        }

        /// <summary>
        /// Retrieves the syndication format from the URL parameters.
        /// </summary>
        private static SyndicationFormat RetrieveFormat(HttpContext context)
        {
            var query = context.Request.Query["format"].ToString();
            var format = BlogSettings.Instance.SyndicationFormat;
            if (!string.IsNullOrEmpty(query))
            {
                format = query;
            }

            try
            {
                return (SyndicationFormat)Enum.Parse(typeof(SyndicationFormat), format, true);
            }
            catch (ArgumentException)
            {
                return SyndicationFormat.None;
            }
        }

        /// <summary>
        /// Retrieves the title.
        /// </summary>
        private static string RetrieveTitle(HttpContext context)
        {
            var title = BlogSettings.Instance.Name;
            string subTitle = null;

            var categoryQuery = context.Request.Query["category"].ToString();
            if (!string.IsNullOrEmpty(categoryQuery))
            {
                if (categoryQuery.Length < 36)
                {
                    return title; // StopServing equivalent
                }

                var firstCat = categoryQuery.Substring(0, 36);
                var categoryId = new Guid(firstCat);
                var currentCategory = Category.GetCategory(categoryId, Blog.CurrentInstance.IsSiteAggregation);
                if (currentCategory != null)
                {
                    subTitle = currentCategory.Title;
                }
            }

            var authorQuery = context.Request.Query["author"].ToString();
            if (!string.IsNullOrEmpty(authorQuery))
            {
                subTitle = authorQuery;
            }

            var postQuery = context.Request.Query["post"].ToString();
            if (!string.IsNullOrEmpty(postQuery))
            {
                if (postQuery.Length != 36)
                {
                    return title; // StopServing equivalent
                }

                var post = Post.GetPost(new Guid(postQuery));

                if (post != null)
                {
                    var arg = new ServingEventArgs(post.Content, ServingLocation.Feed);
                    post.OnServing(arg);
                    if (!arg.Cancel)
                    {
                        subTitle = post.Title;
                    }
                }
            }

            var commentsQuery = context.Request.Query["comments"].ToString();
            if (!string.IsNullOrEmpty(commentsQuery))
            {
                subTitle = "Comments";
            }

            if (subTitle != null)
            {
                return $"{title} - {subTitle}";
            }

            return title;
        }

        /// <summary>
        /// Sets the response header information.
        /// </summary>
        private static void SetHeaderInformation(
            HttpContext context, IEnumerable<IPublishable> items, SyndicationFormat format)
        {
            var lastModified = new DateTime(1900, 1, 3);
            foreach (var item in items)
            {
                if (BlogSettings.Instance.FromUtc(item.DateModified) > lastModified)
                {
                    lastModified = BlogSettings.Instance.FromUtc(item.DateModified);
                }
            }

            switch (format)
            {
                case SyndicationFormat.Atom:
                    context.Response.ContentType = "application/atom+xml";
                    context.Response.Headers.Append("Content-Disposition", "inline; filename=atom.xml");
                    break;

                case SyndicationFormat.Rss:
                    context.Response.ContentType = "application/rss+xml";
                    context.Response.Headers.Append("Content-Disposition", "inline; filename=rss.xml");
                    break;
            }

            if (Utils.SetConditionalGetHeaders(lastModified))
            {
                return;
            }
        }
    }
}
