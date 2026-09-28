namespace BlogEngine.Core.API.MetaWeblog
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// ASP.NET Core endpoint for MetaWeblog API
    /// </summary>
    public static class MetaWeblogEndpoint
    {
        /// <summary>
        /// Maps the MetaWeblog API endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapMetaWeblogHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/metaweblog.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            try
            {
                var rootUrl = Utils.AbsoluteWebRoot.ToString();

                // the request body is a forward-only stream in ASP.NET Core; read it asynchronously
                string requestXml;
                using (var reader = new System.IO.StreamReader(context.Request.Body, System.Text.Encoding.UTF8))
                    requestXml = await reader.ReadToEndAsync();

                var input = new XMLRPCRequest(requestXml);
                var output = new XMLRPCResponse(input.MethodName);

                Security.ImpersonateUser(input.UserName, input.Password);

                // After user credentials have been set, we can use the normal Security
                // class to authorize individual requests.
                if (!Security.IsAuthenticated)
                {
                    throw new MetaWeblogException("11", "User authentication failed");
                }

                switch (input.MethodName)
                {
                    case "metaWeblog.newPost":
                        output.PostID = NewPost(
                            input.BlogID, input.UserName, input.Password, input.Post, input.Publish);
                        break;
                    case "metaWeblog.editPost":
                        output.Completed = EditPost(
                            input.PostID, input.UserName, input.Password, input.Post, input.Publish);
                        break;
                    case "metaWeblog.getPost":
                        output.Post = GetPost(input.PostID, input.UserName, input.Password);
                        break;
                    case "metaWeblog.newMediaObject":
                        output.MediaInfo = NewMediaObject(
                            input.BlogID, input.UserName, input.Password, input.MediaObject, context);
                        break;
                    case "metaWeblog.getCategories":
                        output.Categories = GetCategories(input.BlogID, input.UserName, input.Password, rootUrl);
                        break;
                    case "metaWeblog.getRecentPosts":
                        output.Posts = GetRecentPosts(
                            input.BlogID, input.UserName, input.Password, input.NumberOfPosts);
                        break;
                    case "blogger.getUsersBlogs":
                    case "metaWeblog.getUsersBlogs":
                        output.Blogs = GetUserBlogs(input.AppKey, input.UserName, input.Password, rootUrl);
                        break;
                    case "blogger.deletePost":
                        output.Completed = DeletePost(
                            input.AppKey, input.PostID, input.UserName, input.Password, input.Publish);
                        break;
                    case "blogger.getUserInfo":
                        // Not implemented.  Not planned.
                        throw new MetaWeblogException("10", "The method GetUserInfo is not implemented.");
                    case "wp.newPage":
                        output.PageID = NewPage(
                            input.BlogID, input.UserName, input.Password, input.Page, input.Publish);
                        break;
                    case "wp.getPageList":
                    case "wp.getPages":
                        output.Pages = GetPages(input.BlogID, input.UserName, input.Password);
                        break;
                    case "wp.getPage":
                        output.Page = GetPage(input.BlogID, input.PageID, input.UserName, input.Password);
                        break;
                    case "wp.editPage":
                        output.Completed = EditPage(
                            input.BlogID, input.PageID, input.UserName, input.Password, input.Page, input.Publish);
                        break;
                    case "wp.deletePage":
                        output.Completed = DeletePage(input.BlogID, input.PageID, input.UserName, input.Password);
                        break;
                    case "wp.getAuthors":
                        output.Authors = GetAuthors(input.BlogID, input.UserName, input.Password);
                        break;
                    case "wp.getTags":
                        output.Keywords = GetKeywords(input.BlogID, input.UserName, input.Password);
                        break;
                }

                output.Response(context);
            }
            catch (MetaWeblogException mex)
            {
                var output = new XMLRPCResponse("fault");
                var fault = new MWAFault { faultCode = mex.Code, faultString = mex.Message };
                output.Fault = fault;
                output.Response(context);
            }
            catch (Exception ex)
            {
                var output = new XMLRPCResponse("fault");
                var fault = new MWAFault { faultCode = "0", faultString = ex.Message };
                output.Fault = fault;
                output.Response(context);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Deletes the page.
        /// </summary>
        internal static bool DeletePage(string blogId, string pageId, string userName, string password)
        {
            try
            {
                var page = Page.GetPage(new Guid(pageId));
                if (!page.CanUserDelete)
                {
                    throw new MetaWeblogException("11", "User authentication failed");
                }
                page.Delete();
                page.Save();
            }
            catch (Exception ex)
            {
                throw new MetaWeblogException("15", $"DeletePage failed.  Error: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// blogger.deletePost method
        /// </summary>
        internal static bool DeletePost(string appKey, string postId, string userName, string password, bool publish)
        {
            try
            {
                var post = Post.GetPost(new Guid(postId));

                if (!post.CanUserDelete)
                {
                    throw new MetaWeblogException("11", "User authentication failed");
                }

                post.Delete();
                post.Save();
            }
            catch (Exception ex)
            {
                throw new MetaWeblogException("12", $"DeletePost failed.  Error: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Edits the page.
        /// </summary>
        internal static bool EditPage(
            string blogId, string pageId, string userName, string password, MWAPage mwaPage, bool publish)
        {
            var page = Page.GetPage(new Guid(pageId));

            if (!page.CanUserEdit)
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            if (!page.IsPublished && publish)
            {
                if (!page.CanPublish())
                {
                    throw new MetaWeblogException("11", "Not authorized to publish this Page.");
                }
            }

            page.Title = mwaPage.title;
            page.Content = mwaPage.description;
            page.Keywords = mwaPage.mt_keywords;
            page.ShowInList = publish;
            page.IsPublished = publish;
            if (mwaPage.pageParentID != "0")
            {
                page.Parent = new Guid(mwaPage.pageParentID);
            }

            page.Save();

            return true;
        }

        /// <summary>
        /// metaWeblog.editPost method
        /// </summary>
        internal static bool EditPost(string postId, string userName, string password, MWAPost sentPost, bool publish)
        {
            var post = Post.GetPost(new Guid(postId));

            if (!post.CanUserEdit)
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            string author = String.IsNullOrEmpty(sentPost.author) ? userName : sentPost.author;

            if (!post.IsPublished && publish)
            {
                if (!post.CanPublish(author))
                {
                    throw new MetaWeblogException("11", "Not authorized to publish this Post.");
                }
            }

            post.Author = author;
            post.Title = sentPost.title;
            post.Content = sentPost.description;
            post.IsPublished = publish;
            post.Slug = sentPost.slug;
            post.Description = sentPost.excerpt;
            post.DateCreated = sentPost.postDate;

            if (sentPost.commentPolicy != string.Empty)
            {
                post.HasCommentsEnabled = sentPost.commentPolicy == "1";
            }

            post.Categories.Clear();
            foreach (var item in sentPost.categories.Where(c => c != null && c.Trim() != string.Empty))
            {
                Category cat;
                if (LookupCategoryGuidByName(item, out cat))
                {
                    post.Categories.Add(cat);
                }
                else
                {
                    // Allowing new categories to be added.  (This breaks spec, but is supported via WLW)
                    using (var newcat = new Category(item, string.Empty))
                    {
                        newcat.Save();
                        post.Categories.Add(newcat);
                    }
                }
            }

            post.Tags.Clear();
            foreach (var item in sentPost.tags.Where(item => item != null && item.Trim() != string.Empty))
            {
                post.Tags.Add(item);
            }

            post.Save();

            return true;
        }

        /// <summary>
        /// Gets authors.
        /// </summary>
        internal static List<MWAAuthor> GetAuthors(string blogId, string userName, string password)
        {
            var authors = new List<MWAAuthor>();

            if (Security.IsAuthorizedTo(Rights.EditOtherUsers))
            {
                var userNames = Providers.BlogService.GetUserNames();
                if (userNames != null)
                {
                    authors.AddRange(
                        userNames.Select(
                            user =>
                            new MWAAuthor
                                {
                                    user_id = user,
                                    user_login = user,
                                    display_name = user,
                                    user_email = string.Empty,
                                    meta_value = string.Empty
                                }));
                }
            }
            else
            {
                // If not able to administer others, just add that user to the options.
                var temp = new MWAAuthor
                    {
                        user_id = userName,
                        user_login = userName,
                        display_name = userName,
                        user_email = string.Empty,
                        meta_value = string.Empty
                    };
                authors.Add(temp);
            }

            return authors;
        }

        /// <summary>
        /// metaWeblog.getCategories method
        /// </summary>
        internal static List<MWACategory> GetCategories(string blogId, string userName, string password, string rootUrl)
        {
            if (!Security.IsAuthorizedTo(Rights.CreateNewPosts))
                return new List<MWACategory>();

            return Category.Categories.Select(cat => new MWACategory
                {
                    id = cat.Id.ToString(), title = cat.Title, description = cat.Title, htmlUrl = cat.AbsoluteLink.ToString(), rssUrl = cat.FeedAbsoluteLink.ToString()
                }).ToList();
        }

        /// <summary>
        /// wp.getTags method
        /// </summary>
        internal static List<string> GetKeywords(string blogId, string userName, string password)
        {
            var keywords = new List<string>();

            if (!Security.IsAuthorizedTo(Rights.CreateNewPosts))
                return keywords;

            foreach (var tag in
                Post.Posts.Where(post => post.IsVisible).SelectMany(post => post.Tags.Where(tag => !keywords.Contains(tag))))
            {
                keywords.Add(tag);
            }

            keywords.Sort();

            return keywords;
        }

        /// <summary>
        /// wp.getPage method
        /// </summary>
        internal static MWAPage GetPage(string blogId, string pageId, string userName, string password)
        {
            var sendPage = new MWAPage();
            var page = Page.GetPage(new Guid(pageId));

            if (!page.IsVisible)
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            sendPage.pageID = page.Id.ToString();
            sendPage.title = page.Title;
            sendPage.description = page.Content;
            sendPage.mt_keywords = page.Keywords;
            sendPage.pageDate = page.DateCreated;
            sendPage.link = page.AbsoluteLink.AbsoluteUri;
            sendPage.mt_convert_breaks = "__default__";
            sendPage.pageParentID = page.Parent.ToString();

            return sendPage;
        }

        /// <summary>
        /// wp.getPages method
        /// </summary>
        internal static List<MWAPage> GetPages(string blogId, string userName, string password)
        {
            return Page.Pages.Where(p => p.IsVisible).Select(page => new MWAPage
                {
                    pageID = page.Id.ToString(), title = page.Title, description = page.Content, mt_keywords = page.Keywords, pageDate = page.DateCreated, link = page.AbsoluteLink.AbsoluteUri, mt_convert_breaks = "__default__", pageParentID = page.Parent.ToString()
                }).ToList();
        }

        /// <summary>
        /// metaWeblog.getPost method
        /// </summary>
        internal static MWAPost GetPost(string postId, string userName, string password)
        {
            var sendPost = new MWAPost();
            var post = Post.GetPost(new Guid(postId));

            if (!post.IsVisible)
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            sendPost.postID = post.Id.ToString();
            sendPost.postDate = post.DateCreated;
            sendPost.title = post.Title;
            sendPost.description = post.Content;
            sendPost.link = post.AbsoluteLink.AbsoluteUri;
            sendPost.slug = post.Slug;
            sendPost.excerpt = post.Description;
            sendPost.commentPolicy = post.HasCommentsEnabled ? "1" : "0";
            sendPost.publish = post.IsPublished;

            var cats = post.Categories.Select(t => Category.GetCategory(t.Id).ToString()).ToList();

            sendPost.categories = cats;

            var tags = post.Tags.ToList();

            sendPost.tags = tags;

            return sendPost;
        }

        /// <summary>
        /// metaWeblog.getRecentPosts method
        /// </summary>
        internal static List<MWAPost> GetRecentPosts(string blogId, string userName, string password, int numberOfPosts)
        {
            var sendPosts = new List<MWAPost>();
            var posts = Post.Posts.Where(p => p.IsVisible).ToList();

            // Set End Point
            var stop = numberOfPosts;
            if (stop > posts.Count)
            {
                stop = posts.Count;
            }

            foreach (var post in posts.GetRange(0, stop))
            {
                var tempPost = new MWAPost
                    {
                        postID = post.Id.ToString(),
                        postDate = post.DateCreated,
                        title = post.Title,
                        description = post.Content,
                        link = post.AbsoluteLink.AbsoluteUri,
                        slug = post.Slug,
                        excerpt = post.Description,
                        commentPolicy = post.HasCommentsEnabled ? string.Empty : "0",
                        publish = post.IsPublished
                    };

                var tempCats = post.Categories.Select(t => Category.GetCategory(t.Id).ToString()).ToList();

                tempPost.categories = tempCats;

                var tempTags = post.Tags.ToList();

                tempPost.tags = tempTags;

                sendPosts.Add(tempPost);
            }

            return sendPosts;
        }

        /// <summary>
        /// blogger.getUsersBlogs method
        /// </summary>
        internal static List<MWABlogInfo> GetUserBlogs(string appKey, string userName, string password, string rootUrl)
        {
            var blogs = new List<MWABlogInfo>();

            var temp = new MWABlogInfo { url = rootUrl, blogID = "1000", blogName = BlogSettings.Instance.Name };
            blogs.Add(temp);

            return blogs;
        }

        /// <summary>
        /// metaWeblog.newMediaObject method
        /// </summary>
        internal static MWAMediaInfo NewMediaObject(
            string blogId, string userName, string password, MWAMediaObject mediaObject, HttpContext context)
        {
            if (!Security.IsAuthorizedTo(AuthorizationCheck.HasAny, new Rights[]
                {
                    Rights.CreateNewPosts,
                    Rights.CreateNewPages,
                    Rights.EditOwnPosts,
                    Rights.EditOwnPages,
                    Rights.EditOtherUsersPosts
                }))
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            var mediaInfo = new MWAMediaInfo();

            var rootPath = $"{Blog.CurrentInstance.StorageLocation}files/";
            var serverPath = BlogConfig.MapPath(rootPath);
            var saveFolder = serverPath;
            string mediaObjectName = mediaObject.name.Replace(" ", "_");
            mediaObjectName = mediaObjectName.Replace(":", "-");
            var fileName = mediaObjectName;
            var mediaFolder = string.Empty;

            // Check/Create Folders & Fix fileName
            if (mediaObjectName.LastIndexOf('/') > -1)
            {
                mediaFolder = mediaObjectName.Substring(0, mediaObjectName.LastIndexOf('/'));
                saveFolder += mediaFolder;
                mediaFolder += "/";
                saveFolder = saveFolder.Replace('/', Path.DirectorySeparatorChar);
                fileName = mediaObjectName.Substring(mediaObjectName.LastIndexOf('/') + 1);
            }
            else
            {
                if (saveFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    saveFolder = saveFolder.Substring(0, saveFolder.Length - 1);
                }
            }

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            saveFolder += Path.DirectorySeparatorChar;

            if (File.Exists(saveFolder + fileName))
            {
                // Find unique fileName
                for (var count = 1; count < 30000; count++)
                {
                    var tempFileName = fileName.Insert(fileName.LastIndexOf('.'), $"_{count}");
                    if (File.Exists(saveFolder + tempFileName))
                    {
                        continue;
                    }

                    fileName = tempFileName;
                    break;
                }
            }

            // Save File
            using (var fs = new FileStream(saveFolder + fileName, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(mediaObject.bits);
                bw.Close();
            }

            // Set Url
            var rootUrl = Utils.AbsoluteWebRoot.ToString();
            if (BlogSettings.Instance.RequireSslMetaWeblogApi)
            {
                rootUrl = rootUrl.Replace("https://", "http://");
            }

            var mediaType = mediaObject.type;
            if (mediaType.IndexOf('/') > -1)
            {
                mediaType = mediaType.Substring(0, mediaType.IndexOf('/'));
            }

            switch (mediaType)
            {
                case "image":
                case "notsent":
                    rootUrl += "image.axd?picture=";
                    break;
                default:
                    rootUrl += "file.axd?file=";
                    break;
            }

            mediaInfo.url = rootUrl + mediaFolder + fileName;
            return mediaInfo;
        }

        /// <summary>
        /// wp.newPage method
        /// </summary>
        internal static string NewPage(string blogId, string userName, string password, MWAPage mwaPage, bool publish)
        {
            if (!Security.IsAuthorizedTo(Rights.CreateNewPages))
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            var page = new Page
                {
                    Title = mwaPage.title,
                    Content = mwaPage.description,
                    Description = string.Empty,
                    Keywords = mwaPage.mt_keywords
                };

            if (publish)
            {
                if (!page.CanPublish())
                {
                    throw new MetaWeblogException("11", "Not authorized to publish this Page.");
                }
            }

            if (mwaPage.pageDate != new DateTime())
            {
                page.DateCreated = mwaPage.pageDate;
            }

            page.ShowInList = publish;
            page.IsPublished = publish;
            if (mwaPage.pageParentID != "0")
            {
                page.Parent = new Guid(mwaPage.pageParentID);
            }

            page.Save();

            return page.Id.ToString();
        }

        /// <summary>
        /// metaWeblog.newPost method
        /// </summary>
        internal static string NewPost(string blogId, string userName, string password, MWAPost sentPost, bool publish)
        {
            if (!Security.IsAuthorizedTo(Rights.CreateNewPosts))
            {
                throw new MetaWeblogException("11", "User authentication failed");
            }

            string author = String.IsNullOrEmpty(sentPost.author) ? userName : sentPost.author;

            var post = new Post
                {
                    Author = author,
                    Title = sentPost.title,
                    Content = sentPost.description,
                    IsPublished = publish,
                    Slug = sentPost.slug,
                    Description = sentPost.excerpt
                };

            if (publish)
            {
                if (!post.CanPublish(author))
                {
                    throw new MetaWeblogException("11", "Not authorized to publish this Post.");
                }
            }

            if (sentPost.commentPolicy != string.Empty)
            {
                post.HasCommentsEnabled = sentPost.commentPolicy == "1";
            }

            post.Categories.Clear();
            foreach (var item in sentPost.categories.Where(c => c != null && c.Trim() != string.Empty))
            {
                Category cat;
                if (LookupCategoryGuidByName(item, out cat))
                {
                    post.Categories.Add(cat);
                }
                else
                {
                    // Allowing new categories to be added.  (This breaks spec, but is supported via WLW)
                    var newcat = new Category(item, string.Empty);
                    newcat.Save();
                    post.Categories.Add(newcat);
                }
            }

            post.Tags.Clear();
            foreach (var item in sentPost.tags.Where(item => item != null && item.Trim() != string.Empty))
            {
                post.Tags.Add(item);
            }

            post.DateCreated = sentPost.postDate == new DateTime() ?
                BlogSettings.Instance.FromUtc() : sentPost.postDate;

            post.Save();

            return post.Id.ToString();
        }

        /// <summary>
        /// Returns Category Guid from Category name.
        /// </summary>
        private static bool LookupCategoryGuidByName(string name, out Category cat)
        {
            cat = new Category();
            foreach (var item in Category.Categories.Where(item => item.Title == name))
            {
                cat = item;
                return true;
            }

            return false;
        }
    }
}
