namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Based on John Dyer's (http://johndyer.name/) extension.
    /// Serves FOAF RDF XML.
    /// </summary>
    public static class FoafEndpoint
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
                                { "admin", "http://webns.net/mvcb/" },
                                { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" }
                            });
            }
        }

        /// <summary>
        /// Maps the FOAF handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapFoafHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/foaf.axd", HandleAsync);
            endpoints.MapGet("/{name}.axd", HandleFoafNameAsync);
            return endpoints;
        }

        private static async Task HandleFoafNameAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.Contains("foaf"))
            {
                context.Response.StatusCode = 404;
                return;
            }

            await HandleAsync(context);
        }

        private static async Task HandleAsync(HttpContext context)
        {
            // attempt to grab the username from the URL
            // where URL = www.mysite.com/foaf_admin.axd
            // username = 'admin'
            var requestUrl = context.Request.GetDisplayUrl();
            var filename = requestUrl;
            var name =
                filename.Substring(filename.LastIndexOf("/") + 1).Replace(".axd", string.Empty).Replace(
                    "foaf_", string.Empty).Replace("foaf", string.Empty);

            // if no name is specified, then grab the first user from available profiles
            if (name == string.Empty)
            {
                var userNames = Providers.BlogService.GetUserNames();
                if (userNames != null && userNames.Length > 0)
                {
                    name = userNames[0];
                }
            }

            context.Response.ContentType = "application/rdf+xml";
            WriteFoaf(context, name);
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
        /// Closes up the FOAF document
        /// </summary>
        private static void CloseWriter(XmlWriter xmlWriter)
        {
            xmlWriter.WriteEndElement(); // rdf:RDF
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        /// <summary>
        /// Creates the necessary startup information for the FOAF document
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
        /// Writes the FOAF.
        /// </summary>
        private static void WriteFoaf(HttpContext context, string name)
        {
            // begin FOAF
            var writer = GetWriter(context.Response.Body);

            // write DOCUMENT
            writer.WriteStartElement("foaf", "PersonalProfileDocument", null);
            writer.WriteAttributeString("rdf", "about", null, string.Empty);
            writer.WriteStartElement("foaf", "maker", null);
            writer.WriteAttributeString("rdf", "resource", null, "#me");
            writer.WriteEndElement(); // foaf:maker
            writer.WriteStartElement("foaf", "primaryTopic", null);
            writer.WriteAttributeString("rdf", "resource", null, "#me");
            writer.WriteEndElement(); // foaf:primaryTopic
            writer.WriteEndElement(); // foaf:PersonalProfileDocument

            // get main person's data
            var ap = AuthorProfile.GetProfile(name);

            if (ap != null && !ap.Private)
            {
                // main author object
                var me = new FoafPerson("#me", ap) { Friends = new List<FoafPerson>() };

                // assume all other authors are friends
                var userNames = Providers.BlogService.GetUserNames();
                if (userNames != null)
                {
                    foreach (var userName in userNames)
                    {
                        if (userName.Equals(name, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var friend = AuthorProfile.GetProfile(userName);
                        if (friend == null)
                        {
                            continue;
                        }

                        me.Friends.Add(new FoafPerson("#" + userName, friend));
                    }
                }

                // assume blog roll = friends
                var blogroll = new Data.ViewModels.BlogRollVM();
                foreach (var br in blogroll.BlogRolls)
                {
                    var title = br.Title;
                    var url = br.BlogUrl.ToString();

                    var foaf = new FoafPerson(title) { Name = title, Blog = url };

                    if (Blog.CurrentInstance.Cache[$"foaf:{title}"] == null)
                    {
                        var docs = Utils.FindSemanticDocuments(new Uri(url), "foaf");
                        if (docs.Count > 0)
                        {
                            foreach (var key in docs.Keys)
                            {
                                Blog.CurrentInstance.Cache.Insert($"foaf:{title}", key.ToString());
                                break;
                            }
                        }
                        else
                        {
                            Blog.CurrentInstance.Cache.Insert($"foaf:{title}", "0");
                        }
                    }

                    var seeAlso = (string)Blog.CurrentInstance.Cache[$"foaf:{title}"];
                    if (seeAlso != null && seeAlso.Contains("://"))
                    {
                        foaf.Rdf = seeAlso;
                    }

                    me.Friends.Add(foaf);
                }

                // begin writing FOAF Persons
                WriteFoafPerson(writer, me, context);
            }

            CloseWriter(writer);
        }

        /// <summary>
        /// Write a FOAF:Person and any friends to the output stream
        /// </summary>
        private static void WriteFoafPerson(XmlWriter writer, FoafPerson person, HttpContext context)
        {
            writer.WriteStartElement("foaf", "Person", null);

            writer.WriteElementString("foaf", "name", null, person.Name);
            if (person.Title != string.Empty)
            {
                writer.WriteElementString("foaf", "title", null, person.Title);
            }

            if (person.Firstname != string.Empty)
            {
                writer.WriteElementString("foaf", "givenname", null, person.Firstname);
            }

            if (person.Lastname != string.Empty)
            {
                writer.WriteElementString("foaf", "family_name", null, person.Lastname);
            }

            if (!string.IsNullOrEmpty(person.Email))
            {
                writer.WriteElementString("foaf", "mbox_sha1sum", null, CalculateSha1(person.Email, Encoding.UTF8));
            }

            if (!string.IsNullOrEmpty(person.Homepage))
            {
                writer.WriteStartElement("foaf", "homepage", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Homepage);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Blog))
            {
                writer.WriteStartElement("foaf", "weblog", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Blog);
                writer.WriteEndElement();
            }

            var requestUrl = context.Request.GetDisplayUrl();
            if (person.Rdf != string.Empty && person.Rdf != requestUrl)
            {
                writer.WriteStartElement("rdfs", "seeAlso", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Rdf);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Birthday))
            {
                writer.WriteElementString("foaf", "birthday", null, person.Birthday);
            }

            if (!string.IsNullOrEmpty(person.PhotoUrl))
            {
                writer.WriteStartElement("foaf", "depiction", null);
                writer.WriteAttributeString("rdf", "resource", null, person.PhotoUrl);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Phone))
            {
                writer.WriteElementString("foaf", "phone", null, person.Phone);
            }

            if (person.Friends != null && person.Friends.Count > 0)
            {
                foreach (var friend in person.Friends)
                {
                    writer.WriteStartElement("foaf", "knows", null);

                    WriteFoafPerson(writer, friend, context);

                    writer.WriteEndElement(); // foaf:knows
                }
            }

            writer.WriteEndElement(); // foaf:Person
        }
    }

    /// <summary>
    /// Temporary class for transmitting FOAF attributes
    /// </summary>
    public class FoafPerson
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FoafPerson"/> class.
        /// </summary>
        public FoafPerson(string id)
        {
            this.Birthday = string.Empty;
            this.Blog = string.Empty;
            this.Email = string.Empty;
            this.Firstname = string.Empty;
            this.Homepage = string.Empty;
            this.Image = string.Empty;
            this.Lastname = string.Empty;
            this.Name = string.Empty;
            this.Phone = string.Empty;
            this.PhotoUrl = string.Empty;
            this.Rdf = string.Empty;
            this.Title = string.Empty;
            this.Id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FoafPerson"/> class.
        /// </summary>
        public FoafPerson(string id, AuthorProfile ap)
        {
            this.Homepage = string.Empty;
            this.Title = string.Empty;
            this.Id = string.IsNullOrEmpty(id) ? "#me" : id;
            this.Name = ap.FullName;
            this.Email = ap.EmailAddress;

            this.Blog = Utils.AbsoluteWebRoot.ToString();
            this.Rdf = $"{Utils.AbsoluteWebRoot}foaf_{ap.UserName}.axd";
            this.Firstname = ap.FirstName;
            this.Lastname = ap.LastName;
            this.Image = ap.PhotoUrl;
            this.Birthday = ap.Birthday.ToString("yyyy-MM-dd");
            this.Phone = ap.PhoneMain;
            this.PhotoUrl = ap.PhotoUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FoafPerson"/> class.
        /// </summary>
        public FoafPerson(
            string id,
            string name,
            string title,
            string email,
            string homepage,
            string blog,
            string rdf,
            string firstname,
            string lastname,
            string image,
            string birthday,
            string phone)
        {
            this.PhotoUrl = string.Empty;
            this.Id = id;
            this.Name = name;
            this.Title = title;
            this.Email = email;
            this.Homepage = homepage;
            this.Blog = blog;
            this.Rdf = rdf;
            this.Firstname = firstname;
            this.Lastname = lastname;
            this.Image = image;
            this.Birthday = birthday;
            this.Phone = phone;
        }

        public string Birthday { get; set; }
        public string Blog { get; set; }
        public string Email { get; set; }
        public string Firstname { get; set; }
        public List<FoafPerson> Friends { get; set; }
        public string Homepage { get; set; }
        public string Id { get; set; }
        public string Image { get; set; }
        public string Lastname { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string PhotoUrl { get; set; }
        public string Rdf { get; set; }
        public string Title { get; set; }
    }
}
