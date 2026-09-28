namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Based on John Dyer's (http://johndyer.name/) extension.
    /// Serves APML XML.
    /// </summary>
    public static class ApmlEndpoint
    {
        /// <summary>
        /// Maps the APML handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapApmlHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/apml.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var filePath = context.Request.Path.Value?.ToLower() ?? string.Empty;
            if (filePath != Utils.RelativeWebRoot + "apml.axd")
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.ContentType = "text/xml";
            WriteApmlDocument(context.Response.Body);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Finds the max.
        /// </summary>
        private static IEnumerable<Concept> FindMax(Dictionary<string, Concept> dic)
        {
            float max = 0;
            float max1 = max;
            foreach (var concept in dic.Values.Where(concept => concept.Score > max1))
            {
                max = concept.Score;
            }

            // reset values as a percentage of the max
            var list = dic.Keys.Select(key => new Concept(dic[key].LastUpdated, dic[key].Score / max, key)).ToList();

            list.Sort((c1, c2) => c2.Score.CompareTo(c1.Score));

            return list;
        }

        /// <summary>
        /// Closes the writer.
        /// </summary>
        private static void CloseWriter(XmlWriter xmlWriter)
        {
            xmlWriter.WriteEndElement(); // APML
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        /// <summary>
        /// Creates the link list.
        /// </summary>
        private static Dictionary<string, string> CreateLinkList()
        {
            var dic = new Dictionary<string, string>();
            var blogroll = new Data.ViewModels.BlogRollVM();

            foreach (var br in blogroll.BlogRolls)
            {
                var title = br.Title;
                var website = br.BlogUrl.ToString();
                dic.Add(title, website);
            }

            return dic;
        }

        /// <summary>
        /// Creates the tag list.
        /// </summary>
        private static IEnumerable<Concept> CreateTagList()
        {
            var dic = new Dictionary<string, Concept>();
            foreach (var post in Post.Posts)
            {
                if (!post.IsVisible)
                {
                    continue;
                }

                foreach (var tag in post.Tags)
                {
                    if (dic.ContainsKey(tag))
                    {
                        var concept = dic[tag];
                        concept.Score++;
                        if (post.DateModified > concept.LastUpdated)
                        {
                            concept.LastUpdated = post.DateModified;
                        }

                        dic[tag] = concept;
                    }
                    else
                    {
                        dic[tag] = new Concept(post.DateModified, 1, tag);
                    }
                }
            }

            return FindMax(dic);
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        private static XmlWriter GetWriter(Stream stream)
        {
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
            var xmlWriter = XmlWriter.Create(stream, settings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("APML");

            return xmlWriter;
        }

        /// <summary>
        /// The write APML document.
        /// </summary>
        private static void WriteApmlDocument(Stream stream)
        {
            var writer = GetWriter(stream);

            // HEAD
            writer.WriteStartElement("Head");
            writer.WriteElementString(
                "Title", "APML data for " + BlogSettings.Instance.Name + " - " + BlogSettings.Instance.Description);
            writer.WriteElementString("Generator", "BlogEngine.NET " + BlogSettings.Instance.Version());
            writer.WriteElementString("UserEmail", string.Empty);
            writer.WriteElementString("DateCreated", DateTime.Now.ToString());
            writer.WriteEndElement(); // Head

            // BODY
            writer.WriteStartElement("Body");
            writer.WriteAttributeString("defaultProfile", "tags");

            // tags
            writer.WriteStartElement("Profile");
            writer.WriteAttributeString("name", "tags");
            writer.WriteStartElement("ImplicitData");
            writer.WriteStartElement("Concepts");

            var tagList = CreateTagList();
            foreach (var key in tagList)
            {
                writer.WriteStartElement("Concept");
                writer.WriteAttributeString("key", key.Title);
                writer.WriteAttributeString("value", key.Score.ToString());
                writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
                writer.WriteAttributeString("updated", key.LastUpdated.ToString());
                writer.WriteEndElement(); // Concept
            }

            writer.WriteEndElement(); // Concepts
            writer.WriteEndElement(); // ImplicitData
            writer.WriteEndElement(); // Profile

            // links
            writer.WriteStartElement("Profile");
            writer.WriteAttributeString("name", "links");
            writer.WriteStartElement("ExplicitData");
            writer.WriteStartElement("Concepts");

            var links = CreateLinkList();
            if (links != null)
            {
                foreach (var title in links.Keys)
                {
                    var website = links[title];
                    writer.WriteStartElement("Source");
                    writer.WriteAttributeString("key", website);
                    writer.WriteAttributeString("name", title);
                    writer.WriteAttributeString("value", "1.0");
                    writer.WriteAttributeString("type", "text/html");
                    writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
                    writer.WriteAttributeString("updated", DateTime.Now.ToString());

                    writer.WriteStartElement("Author");
                    writer.WriteAttributeString("key", title);
                    writer.WriteAttributeString("value", "1.0");
                    writer.WriteAttributeString("from", Utils.AbsoluteWebRoot.ToString());
                    writer.WriteAttributeString("updated", DateTime.Now.ToString());
                    writer.WriteEndElement(); // Author

                    writer.WriteEndElement(); // Source
                }
            }

            writer.WriteEndElement(); // Concepts
            writer.WriteEndElement(); // ImplicitData
            writer.WriteEndElement(); // Profile
            writer.WriteEndElement(); // Body

            CloseWriter(writer);
        }

        /// <summary>
        /// The concept.
        /// </summary>
        private class Concept
        {
            public Concept(DateTime lastUpdated, float score, string title)
            {
                this.LastUpdated = lastUpdated;
                this.Score = score;
                this.Title = title;
            }

            public DateTime LastUpdated { get; set; }
            public float Score { get; set; }
            public string Title { get; private set; }
        }
    }
}
