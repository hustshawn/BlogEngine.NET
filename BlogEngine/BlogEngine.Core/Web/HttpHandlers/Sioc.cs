namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Based on John Dyer's (http://johndyer.name/) extension.
    /// Serves SIOC RDF XML.
    /// </summary>
    public static class SiocEndpoint
    {
        #region Constants and Fields

        /// <summary>
        ///     The xml namespaces.
        /// </summary>
        private static Dictionary<string, string> xmlNamespaces;

        #endregion

        /// <summary>
        ///     Gets SupportedNamespaces.
        /// </summary>
        private static Dictionary<string, string> SupportedNamespaces
        {
            get
            {
                return xmlNamespaces ??
                       (xmlNamespaces =
                        new Dictionary<string, string>
                            {
                                { "foaf", "http://xmlns.com/foaf/0.1/" },
                                { "rss", "http://purl.org/rss/1.0/" },
                                { "admin", "http://webns.net/mvcb/" },
                                { "dc", "http://purl.org/dc/elements/1.1/" },
                                { "dcterms", "http://purl.org/dc/terms/" },
                                { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#" },
                                { "content", "http://purl.org/rss/1.0/modules/content" },
                                { "sioc", "http://rdfs.org/sioc/ns#" }
                            });
            }
        }

        /// <summary>
        /// Maps the SIOC handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapSiocHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/sioc.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            context.Response.ContentType = "application/rdf+xml";
            var siocType = context.Request.Query["sioc_type"].ToString();
            var siocId = context.Request.Query["sioc_id"].ToString();

            switch (siocType)
            {
                default:
                    var list = Post.Posts.ConvertAll(ConvertToIPublishable);
                    var max = Math.Min(BlogSettings.Instance.PostsPerFeed, list.Count);
                    list = list.GetRange(0, max);
                    WriteSite(context.Response.Body, list);
                    break;

                case "post":
                    Guid postId;
                    if (siocId.TryParse(out postId))
                    {
                        var post = Post.GetPost(postId);
                        if (post != null)
                        {
                            WritePub(context.Response.Body, post);
                        }
                    }

                    break;

                case "comment":
                    Guid commentId;
                    if (siocId.TryParse(out commentId))
                    {
                        var comment = GetComment(commentId);

                        if (comment != null)
                        {
                            WritePub(context.Response.Body, comment);
                        }
                    }

                    break;

                case "user":
                    WriteAuthor(context.Response.Body, siocId);
                    break;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Calculates the SHA1.
        /// </summary>
        private static string CalculateSha1(string text, Encoding enc)
        {
            var buffer = enc.GetBytes(text);
            using (var sha1 = SHA1.Create())
            {
                var hash = BitConverter.ToString(sha1.ComputeHash(buffer)).Replace("-", string.Empty);
                return hash.ToLower();
            }
        }

        /// <summary>
        /// Closes the writer.
        /// </summary>
        private static void CloseWriter(XmlWriter xmlWriter)
        {
            xmlWriter.WriteEndElement(); // rdf:RDF
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        /// <summary>
        /// Converts to publishable interface.
        /// </summary>
        private static IPublishable ConvertToIPublishable(IPublishable item)
        {
            return item;
        }

        /// <summary>
        /// Gets the blog author URL.
        /// </summary>
        private static string GetBlogAuthorUrl(string username)
        {
            return $"{Utils.AbsoluteWebRoot}author/{WebUtility.UrlEncode(username)}{BlogConfig.FileExtension}";
        }

        /// <summary>
        /// Gets the comment.
        /// </summary>
        private static Comment GetComment(Guid commentId)
        {
            var posts = Post.Posts;
            return posts.SelectMany(post => post.Comments).FirstOrDefault(comm => comm.Id == commentId);
        }

        /// <summary>
        /// Gets the sioc author URL.
        /// </summary>
        private static string GetSiocAuthorUrl(string username)
        {
            return $"{Utils.AbsoluteWebRoot}sioc.axd?sioc_type=user&sioc_id={WebUtility.UrlEncode(username)}";
        }

        /// <summary>
        /// Gets the sioc authors URL.
        /// </summary>
        private static string GetSiocAuthorsUrl()
        {
            return $"{Utils.AbsoluteWebRoot}sioc.axd?sioc_type=site#authors";
        }

        /// <summary>
        /// Gets the sioc blog URL.
        /// </summary>
        private static string GetSiocBlogUrl()
        {
            return $"{Utils.AbsoluteWebRoot}sioc.axd?sioc_type=site#webblog";
        }

        /// <summary>
        /// Gets the sioc comment URL.
        /// </summary>
        private static string GetSiocCommentUrl(string id)
        {
            return $"{Utils.AbsoluteWebRoot}sioc.axd?sioc_type=comment&sioc_id={id}";
        }

        /// <summary>
        /// Gets the sioc post URL.
        /// </summary>
        private static string GetSiocPostUrl(string id)
        {
            return $"{Utils.AbsoluteWebRoot}sioc.axd?sioc_type=post&sioc_id={id}";
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        private static XmlWriter GetWriter(Stream stream)
        {
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
            var xmlWriter = XmlWriter.Create(stream, settings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            foreach (var prefix in SupportedNamespaces.Keys)
            {
                xmlWriter.WriteAttributeString("xmlns", prefix, null, SupportedNamespaces[prefix]);
            }

            return xmlWriter;
        }

        /// <summary>
        /// Writes the author.
        /// </summary>
        private static void WriteAuthor(Stream stream, string authorName)
        {
            var xmlWriter = GetWriter(stream);

            WriteFoafDocument(xmlWriter, "user", GetSiocAuthorUrl(authorName));

            var ap = AuthorProfile.GetProfile(authorName);

            // FOAF:Person
            xmlWriter.WriteStartElement("foaf", "Person", null);
            xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocAuthorUrl(authorName));

            xmlWriter.WriteElementString("foaf", "Name", null, authorName);
            if (ap != null && !ap.Private && ap.FirstName != String.Empty)
            {
                xmlWriter.WriteElementString("foaf", "firstName", null, ap.FirstName);
            }

            if (ap != null && !ap.Private && ap.LastName != String.Empty)
            {
                xmlWriter.WriteElementString("foaf", "surname", null, ap.LastName);
            }

            // REVIEW: Membership.GetUser replaced with profile-based email lookup — verify after auth migration
            var email = ap?.EmailAddress ?? string.Empty;
            xmlWriter.WriteElementString(
                "foaf", "mbox_sha1sum", null, !string.IsNullOrEmpty(email) ? CalculateSha1(email, Encoding.UTF8) : string.Empty);
            xmlWriter.WriteStartElement("foaf", "homepage", null);
            xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot.ToString());
            xmlWriter.WriteEndElement(); // foaf:homepage

            xmlWriter.WriteStartElement("foaf", "holdsAccount", null);
            xmlWriter.WriteAttributeString("rdf", "resource", null, GetBlogAuthorUrl(authorName));
            xmlWriter.WriteEndElement(); // foaf:holdsAccount

            xmlWriter.WriteEndElement(); // foaf:Person

            // SIOC:User
            xmlWriter.WriteStartElement("sioc", "User", null);
            xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(authorName));
            xmlWriter.WriteElementString("foaf", "accountName", null, authorName);
            xmlWriter.WriteElementString("sioc", "name", null, authorName);

            xmlWriter.WriteEndElement(); // sioc:User

            CloseWriter(xmlWriter);
        }

        /// <summary>
        /// Writes the foaf document.
        /// </summary>
        private static void WriteFoafDocument(XmlWriter xmlWriter, string siocType, string url)
        {
            var title = $"SIOC {siocType} profile for \"{BlogSettings.Instance.Name}\"";
            const string Description =
                "A SIOC profile describes the structure and contents of a weblog in a machine readable form. For more information please refer to http://sioc-project.org/.";

            xmlWriter.WriteStartElement("foaf", "Document", null);
            xmlWriter.WriteAttributeString("rdf", "about", null, Utils.AbsoluteWebRoot.ToString());

            xmlWriter.WriteElementString("dc", "title", null, title);
            xmlWriter.WriteElementString("dc", "description", null, Description);
            xmlWriter.WriteElementString("foaf", "primaryTopic", null, url);
            xmlWriter.WriteElementString(
                "admin", "generatorAgent", null, $"BlogEngine.NET{BlogSettings.Instance.Version()}");

            xmlWriter.WriteEndElement(); // foaf:Document
        }

        /// <summary>
        /// Writes the forum.
        /// </summary>
        private static void WriteForum(XmlWriter xmlWriter, IEnumerable<IPublishable> list)
        {
            xmlWriter.WriteStartElement("sioc", "Forum", null);

            xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocBlogUrl());
            xmlWriter.WriteElementString("sioc", "name", null, BlogSettings.Instance.Name);
            xmlWriter.WriteStartElement("sioc", "link", null);
            xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocBlogUrl());
            xmlWriter.WriteEndElement();

            foreach (var pub in list)
            {
                xmlWriter.WriteStartElement("sioc", "container_of", null);
                xmlWriter.WriteStartElement("sioc", "Post", null);
                xmlWriter.WriteAttributeString("rdf", "about", null, pub.AbsoluteLink.ToString());
                xmlWriter.WriteAttributeString("dc", "title", null, pub.Title);

                xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
                xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocPostUrl(pub.Id.ToString()));
                xmlWriter.WriteEndElement(); // sioc:Post

                xmlWriter.WriteEndElement(); // sioc:Post
                xmlWriter.WriteEndElement(); // sioc:Forum
            }

            xmlWriter.WriteEndElement(); // sioc:Forum
        }

        /// <summary>
        /// Writes an IPublishable to a stream.
        /// </summary>
        private static void WritePub(Stream stream, IPublishable pub)
        {
            var xmlWriter = GetWriter(stream);

            if (pub is Post)
            {
                WriteFoafDocument(xmlWriter, "post", pub.AbsoluteLink.ToString());
            }
            else
            {
                WriteFoafDocument(xmlWriter, "comment", pub.AbsoluteLink.ToString());
            }

            WriteSiocPost(xmlWriter, pub);
            CloseWriter(xmlWriter);
        }

        /// <summary>
        /// Writes SIOC post.
        /// </summary>
        private static void WriteSiocPost(XmlWriter xmlWriter, IPublishable pub)
        {
            xmlWriter.WriteStartElement("sioc", "Post", null);
            xmlWriter.WriteAttributeString("rdf", "about", null, pub.AbsoluteLink.ToString());

            xmlWriter.WriteStartElement("sioc", "link", null);
            xmlWriter.WriteAttributeString("rdf", "resource", null, pub.AbsoluteLink.ToString());
            xmlWriter.WriteEndElement(); // sioc:link

            xmlWriter.WriteStartElement("sioc", "has_container", null);
            xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocBlogUrl());
            xmlWriter.WriteEndElement(); // sioc:has_container

            xmlWriter.WriteElementString("dc", "title", null, pub.Title);

            // SIOC:USER
            if (pub is Post)
            {
                xmlWriter.WriteStartElement("sioc", "has_creator", null);
                xmlWriter.WriteStartElement("sioc", "User", null);
                xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(pub.Author));
                xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
                xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(pub.Author));
                xmlWriter.WriteEndElement(); // rdf:about
                xmlWriter.WriteEndElement(); // sioc:User
                xmlWriter.WriteEndElement(); // sioc:has_creator
            }

            // FOAF:maker
            xmlWriter.WriteStartElement("foaf", "maker", null);
            xmlWriter.WriteStartElement("foaf", "Person", null);
            if (pub is Post)
            {
                xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(pub.Author));
            }

            xmlWriter.WriteAttributeString("foaf", "name", null, pub.Author);

            if (pub is Post)
            {
                // REVIEW: Membership.GetUser replaced with profile-based email lookup — verify after auth migration
                var ap = AuthorProfile.GetProfile(pub.Author);
                var email = ap?.EmailAddress ?? string.Empty;
                xmlWriter.WriteElementString(
                    "foaf",
                    "mbox_sha1sum",
                    null,
                    !string.IsNullOrEmpty(email) ? CalculateSha1(email, Encoding.UTF8) : string.Empty);
            }
            else
            {
                xmlWriter.WriteElementString(
                    "foaf", "mbox_sha1sum", null, CalculateSha1(((Comment)pub).Email, Encoding.UTF8));
            }

            xmlWriter.WriteStartElement("foaf", "homepage", null);
            if (pub is Post)
            {
                xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot.ToString());
            }
            else
            {
                xmlWriter.WriteAttributeString("rdf", "resource", null, "TODO:");
            }

            xmlWriter.WriteEndElement(); // foaf:homepage

            if (pub is Post)
            {
                xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
                xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(pub.Author));
                xmlWriter.WriteEndElement(); // rdfs:seeAlso
            }

            xmlWriter.WriteEndElement(); // foaf:Person
            xmlWriter.WriteEndElement(); // foaf:maker

            // CONTENT
            xmlWriter.WriteElementString("sioc", "content", null, Utils.StripHtml(pub.Content));
            xmlWriter.WriteElementString("content", "encoded", null, pub.Content);

            // TOPICS
            if (pub is Post)
            {
                // categories
                foreach (var category in ((Post)pub).Categories)
                {
                    xmlWriter.WriteStartElement("sioc", "topic", null);
                    xmlWriter.WriteAttributeString("rdfs", "label", null, category.Title);
                    xmlWriter.WriteAttributeString("rdf", "resource", null, category.AbsoluteLink.ToString());
                    xmlWriter.WriteEndElement(); // sioc:topic
                }

                // tags are also supposed to be treated as sioc:topic
                foreach (var tag in ((Post)pub).Tags)
                {
                    xmlWriter.WriteStartElement("sioc", "topic", null);
                    xmlWriter.WriteAttributeString("rdfs", "label", null, tag);
                    xmlWriter.WriteAttributeString("rdf", "resource", null, Utils.AbsoluteWebRoot + "?tag=/" + tag);
                    xmlWriter.WriteEndElement(); // sioc:topic
                }

                // COMMENTS
                foreach (var comment in ((Post)pub).ApprovedComments)
                {
                    xmlWriter.WriteStartElement("sioc", "has_reply", null);
                    xmlWriter.WriteStartElement("sioc", "Post", null);
                    xmlWriter.WriteAttributeString("rdf", "about", null, comment.AbsoluteLink.ToString());

                    xmlWriter.WriteStartElement("rdfs", "seeAlso", null);
                    xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocCommentUrl(comment.Id.ToString()));
                    xmlWriter.WriteEndElement(); // rdfs:seeAlso

                    xmlWriter.WriteEndElement(); // sioc:Post
                    xmlWriter.WriteEndElement(); // sioc:has_reply
                }

                // LINKS
                var linkMatches = Regex.Matches(pub.Content, @"<a[^(href)]?href=""([^""]+)""[^>]?>([^<]+)</a>");
                var linkPairs = new List<string>();

                foreach (Match linkMatch in linkMatches)
                {
                    var url = linkMatch.Groups[1].Value;
                    var text = linkMatch.Groups[2].Value;

                    if (url.IndexOf(Utils.AbsoluteWebRoot.ToString()) == 0)
                    {
                        continue;
                    }

                    var pair = url + "|" + text;
                    if (linkPairs.Contains(pair))
                    {
                        continue;
                    }

                    xmlWriter.WriteStartElement("sioc", "links_to", null);
                    xmlWriter.WriteAttributeString("rdf", "resource", null, url);
                    xmlWriter.WriteAttributeString("rdfs", "label", null, text);
                    xmlWriter.WriteEndElement(); // sioc:links_to

                    linkPairs.Add(pair);
                }
            }

            xmlWriter.WriteEndElement(); // sioc:Post
        }

        /// <summary>
        /// Writes sioc site.
        /// </summary>
        private static void WriteSiocSite(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("sioc", "Site", null);
            xmlWriter.WriteAttributeString("rdf", "about", null, Utils.AbsoluteWebRoot.ToString());

            xmlWriter.WriteElementString("dc", "title", null, BlogSettings.Instance.Name);
            xmlWriter.WriteElementString("dc", "description", null, BlogSettings.Instance.Description);
            xmlWriter.WriteElementString("sioc", "link", null, Utils.AbsoluteWebRoot.ToString());
            xmlWriter.WriteElementString("sioc", "host_of", null, GetSiocBlogUrl());
            xmlWriter.WriteElementString("sioc", "has_group", null, GetSiocAuthorsUrl());

            xmlWriter.WriteEndElement(); // sioc:Site
        }

        /// <summary>
        /// Writes site.
        /// </summary>
        private static void WriteSite(Stream stream, IEnumerable<IPublishable> list)
        {
            var xmlWriter = GetWriter(stream);

            WriteUserGroup(xmlWriter);
            WriteFoafDocument(xmlWriter, "site", Utils.AbsoluteWebRoot.ToString());
            WriteSiocSite(xmlWriter);
            WriteForum(xmlWriter, list);

            CloseWriter(xmlWriter);
        }

        /// <summary>
        /// Writes the user group.
        /// </summary>
        private static void WriteUserGroup(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("sioc", "Usergroup", null);

            xmlWriter.WriteAttributeString("rdf", "about", null, GetSiocAuthorsUrl());
            xmlWriter.WriteElementString("dc", "title", null, "Authors at \"" + BlogSettings.Instance.Name + "\"");

            var userNames = Providers.BlogService.GetUserNames();
            if (userNames != null)
            {
                foreach (var userName in userNames)
                {
                    xmlWriter.WriteStartElement("sioc", "has_member", null);
                    xmlWriter.WriteStartElement("sioc", "User", null);
                    xmlWriter.WriteAttributeString("rdf", "about", null, GetBlogAuthorUrl(userName));
                    xmlWriter.WriteAttributeString("rdfs", "label", null, userName);

                    xmlWriter.WriteStartElement("sioc", "see_also", null);
                    xmlWriter.WriteAttributeString("rdf", "resource", null, GetSiocAuthorUrl(userName));
                    xmlWriter.WriteEndElement(); // sioc:see_also

                    xmlWriter.WriteEndElement(); // sioc:User
                    xmlWriter.WriteEndElement(); // sioc:has_member
                }
            }

            xmlWriter.WriteEndElement(); // sioc:Usergroup
        }
    }
}
