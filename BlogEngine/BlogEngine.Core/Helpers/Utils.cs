namespace BlogEngine.Core
{
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Reflection;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;
    using BlogEngine.Core.Web.Controls;
    using BlogEngine.Core.Web.Extensions;
    using System.Net.Sockets;

    /// <summary>
    /// Utilities for the entire solution to use.
    /// </summary>
    public static class Utils
    {
        #region Constants and Fields

        /// <summary>
        /// Static IHttpContextAccessor for resolving current HttpContext.
        /// Must be set at startup via DI.
        /// </summary>
        private static IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Sets the IHttpContextAccessor for the Utils class. Call from startup.
        /// </summary>
        public static void SetHttpContextAccessor(IHttpContextAccessor accessor)
        {
            _httpContextAccessor = accessor;
        }

        /// <summary>
        /// The cookie name for forcing the main theme on a mobile device.
        /// </summary>
        public const String ForceMainThemeCookieName = "forceMainTheme";

        /// <summary>
        /// The folder name where files are stored in the XmlFileSystemProvider.
        /// </summary>
        /// <value>"files"</value>
        public const string FilesFolder = "files";

        /// <summary>
        /// The pattern.
        /// </summary>
        private const string Pattern = "<head.*<link( [^>]*title=\"{0}\"[^>]*)>.*</head>";

        /// <summary>
        /// The application's relative web root.
        /// </summary>
        private static string applicationRelativeWebRoot;

        /// <summary>
        /// The href regex.
        /// </summary>
        private static readonly Regex HrefRegex = new Regex(
            "href=\"(.*)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// The regex between tags.
        /// </summary>
        private static readonly Regex RegexBetweenTags = new Regex(@">\s+", RegexOptions.Compiled);

        /// <summary>
        /// The regex line breaks.
        /// </summary>
        private static readonly Regex RegexLineBreaks = new Regex(@"\n\s+", RegexOptions.Compiled);

        /// <summary>
        /// The regex mobile.
        /// </summary>
        private static readonly Regex RegexMobile =
            new Regex(BlogConfig.MobileServices, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// The regex strip html.
        /// </summary>
        private static readonly Regex RegexStripHtml = new Regex("<[^>]*>", RegexOptions.Compiled);

        /// <summary>
        ///     Boolean for returning whether or not BlogEngine is currently running on Mono.
        /// </summary>
        private static readonly bool isMono = (Type.GetType("Mono.Runtime") != null);

        #endregion

        #region Events

        /// <summary>
        ///     Occurs after an e-mail has been sent. The sender is the MailMessage object.
        /// </summary>
        public static event EventHandler<EventArgs> EmailFailed;

        /// <summary>
        ///     Occurs after an e-mail has been sent. The sender is the MailMessage object.
        /// </summary>
        public static event EventHandler<EventArgs> EmailSent;

        /// <summary>
        ///     Occurs when a message will be logged. The sender is a string containing the log message.
        /// </summary>
        public static event EventHandler<EventArgs> OnLog;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the absolute root of the website.
        /// </summary>
        /// <value>A string that ends with a '/'.</value>
        public static Uri AbsoluteWebRoot
        {
            get { return Blog.CurrentInstance.AbsoluteWebRoot; }
        }

        /// <summary>
        ///     Gets the relative URL of the blog feed. If a Feedburner username
        ///     is entered in the admin settings page, it will return the 
        ///     absolute Feedburner URL to the feed.
        /// </summary>
        public static string FeedUrl
        {
            get
            {
                return !string.IsNullOrEmpty(BlogSettings.Instance.AlternateFeedUrl)
                           ? BlogSettings.Instance.AlternateFeedUrl
                           : $"{AbsoluteWebRoot}syndication.axd";
            }
        }

        /// <summary>
        ///     Gets a value indicating whether we're running under Linux or a Unix variant.
        /// </summary>
        /// <value><c>true</c> if Linux/Unix; otherwise, <c>false</c>.</value>
        public static bool IsLinux
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 128);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the client is a mobile device.
        /// </summary>
        /// <value><c>true</c> if this instance is mobile; otherwise, <c>false</c>.</value>
        public static bool IsMobile
        {
            get
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context != null)
                {
                    var request = context.Request;
                    var userAgent = request.Headers["User-Agent"].ToString();

                    if (!string.IsNullOrEmpty(userAgent) && RegexMobile.IsMatch(userAgent))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether we're running under Mono.
        /// </summary>
        /// <value><c>true</c> if Mono; otherwise, <c>false</c>.</value>
        public static bool IsMono
        {
            get
            {
                return isMono;
            }
        }

        /// <summary>
        ///     Gets the relative root of the current blog instance.
        /// </summary>
        /// <value>A string that ends with a '/'.</value>
        public static string RelativeWebRoot
        {
            get
            {
                return Blog.CurrentInstance.RelativeWebRoot;
            }
        }

        /// <summary>
        ///     Gets the application's relative root.
        /// </summary>
        /// <value>A string that ends with a '/'.</value>
        public static string ApplicationRelativeWebRoot
        {
            get
            {
                return applicationRelativeWebRoot ??
                       (applicationRelativeWebRoot =
                        VirtualPathUtility.ToAbsolute(BlogConfig.VirtualPath));
            }
        }

        /// <summary>
        ///     Returns the relative webroot if possible if the hostname of this blog instance matches the
        ///     hostname of the site aggregation blog.  If the hostname is different, then the
        ///     absolute webroot is returned.
        /// </summary>
        public static string RelativeOrAbsoluteWebRoot
        {
            get
            {
                if (Blog.CurrentInstance.DoesHostnameDifferFromSiteAggregationBlog)
                    return AbsoluteWebRoot.ToString();
                else
                    return RelativeWebRoot.ToString();
            }
        }

        /// <summary>
        ///     The request path including PathBase (the application virtual directory and, for
        ///     child blogs, the blog's virtual path), i.e. the path System.Web's Request.RawUrl had.
        /// </summary>
        public static string GetRequestPath(HttpContext context)
        {
            return (context.Request.PathBase + context.Request.Path).Value ?? "/";
        }

        /// <summary>
        ///     Equivalent of System.Web's Request.RawUrl: path including PathBase, plus query string.
        /// </summary>
        public static string GetRawUrl(HttpContext context)
        {
            return GetRequestPath(context) + context.Request.QueryString.Value;
        }

        /// <summary>
        ///     Gets if the current HTTP request is a homepage request, taking the blog
        ///     instance into consideration.
        /// </summary>
        public static bool IsCurrentRequestForHomepage
        {
            get
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context == null) return false;

                string[] knownQueryParameters =
                {
                    "id", "name", "tag", "year", "month", "date", "calendar", "apml", "page"
                };
                if (context.Request.Query.Keys
                    .Where(x => x != null)
                    .Any(x => knownQueryParameters.Any(y => x.Equals(y, StringComparison.OrdinalIgnoreCase))))
                    return false;

                string path = GetRequestPath(context);

                if (RelativeWebRoot.Equals(VirtualPathUtility.AppendTrailingSlash(path), StringComparison.OrdinalIgnoreCase))
                    return true;
                else if (path.Equals($"{RelativeWebRoot}default.aspx", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns whether the main theme should be forced for the current mobile device.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// A <see cref="Boolean"/> instance.
        /// </returns>
        public static Boolean ShouldForceMainTheme(HttpRequest request)
        {
            if (request == null) return false;

            if (request.Cookies.TryGetValue(ForceMainThemeCookieName, out var cookieValue))
            {
                if (!string.IsNullOrEmpty(cookieValue) && Boolean.TryParse(cookieValue, out var forceMainTheme))
                {
                    return forceMainTheme;
                }
            }

            return false;
        }

        /// <summary>
        /// Parse the string representation of an enum field to a strongly typed enum value.
        /// </summary>
        public static T ParseEnum<T>(string value, T defaultValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            foreach (T item in Enum.GetValues(typeof(T)))
            {
                if (item.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return defaultValue;
        }

        private static Regex _identifierForDisplayRgx = new Regex(
            @"  (?<=[A-Z])(?=[A-Z][a-z])    # UC before me, UC lc after me
             |  (?<=[^A-Z])(?=[A-Z])        # Not UC before me, UC after me
             |  (?<=[A-Za-z])(?=[^A-Za-z])  # Letter before me, non letter after me
            ", RegexOptions.IgnorePatternWhitespace
        );
        /// <summary>
        /// Format's an identifier (e.g. variable, enum field value) for display purposes,
        /// by adding a space before each capital letter.  A value like EditOwnUser becomes
        /// "Edit Own User".  If multiple capital letters are in identifier, these letters
        /// will be treated as one word (e.g. XMLEditor becomes "XML Editor", not
        /// "X M L Editor").
        /// </summary>
        /// <param name="fieldName">An identifier ready to be formatted for display.</param>
        /// <returns>The identifier for display purposes.</returns>
        /// <remarks>Credit: http://stackoverflow.com/questions/3103730</remarks>
        public static string FormatIdentifierForDisplay(string fieldName)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string part in _identifierForDisplayRgx.Split(fieldName))
            {
                if (sb.Length > 0) { sb.Append(" "); }
                sb.Append(part);
            }

            return sb.ToString();
        }

        // REVIEW: SelectListItemByValue was a WebForms helper for ListControl.
        // In modern .NET, use HTML select binding directly in Razor/Blazor views.

        /// <summary>
        /// Converts an object to its JSON representation.
        /// </summary>
        public static string ConvertToJson(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }

        /// <summary>
        /// Adds a JavaScript reference URL. In modern .NET, this returns the script tag as a string
        /// for inclusion in views.
        /// </summary>
        public static string AddJavaScriptInclude(string url, bool placeInBottom = false, bool addDeferAttribute = false)
        {
            var deferAttr = addDeferAttribute ? " defer=\"defer\"" : string.Empty;
            return $"<script type=\"text/javascript\"{deferAttr} src=\"{url}\"></script>";
        }

        /// <summary>
        /// Returns code assemblies loaded from the bin directory.
        /// </summary>
        public static IEnumerable<Assembly> CodeAssemblies()
        {
            // Extensions under Custom/Extensions are compiled into the web application itself
            // (the replacement for the runtime-compiled App_Code assembly)
            var codeAssemblies = new List<Assembly>();
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
                codeAssemblies.Add(entryAssembly);

            try
            {
                codeAssemblies.AddRange(GetCompiledExtensions());
            }
            catch (Exception ex)
            {
                Log("Error loading compiled assemblies: " + ex.Message);
            }

            return codeAssemblies;
        }

        /// <summary>
        /// Converts a relative URL to an absolute one.
        /// </summary>
        /// <param name="relativeUri">
        /// The relative Uri.
        /// </param>
        /// <returns>
        /// The absolute Uri.
        /// </returns>
        public static Uri ConvertToAbsolute(Uri relativeUri)
        {
            return ConvertToAbsolute(relativeUri.ToString());
        }

        /// <summary>
        /// Converts a relative URL to an absolute one.
        /// </summary>
        /// <param name="relativeUri">
        /// The relative Uri.
        /// </param>
        /// <returns>
        /// The absolute Uri.
        /// </returns>
        public static Uri ConvertToAbsolute(string relativeUri)
        {
            if (String.IsNullOrEmpty(relativeUri))
            {
                throw new ArgumentNullException(nameof(relativeUri));
            }

            var absolute = AbsoluteWebRoot.ToString();
            var index = absolute.LastIndexOf(RelativeWebRoot);

            return new Uri(absolute.Substring(0, index) + relativeUri);
        }

        /// <summary>
        /// Downloads a web page from the Internet and returns the HTML as a string. .
        /// </summary>
        /// <param name="url">
        /// The URL to download from.
        /// </param>
        /// <returns>
        /// The HTML or null if the URL isn't valid.
        /// </returns>
        [Obsolete("Use the RemoteFile class instead.")]
        public static string DownloadWebPage(Uri url)
        {
            try
            {
                var remoteFile = new RemoteFile(url, true);
                return remoteFile.GetFileAsString();
            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// Extract file name from given physical server path
        /// </summary>
        /// <param name="path">
        /// The Server path.
        /// </param>
        /// <returns>
        /// The File Name.
        /// </returns>
        public static string ExtractFileNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// Finds semantic links in a given HTML document.
        /// </summary>
        /// <param name="type">
        /// The type of link. Could be foaf, apml or sioc.
        /// </param>
        /// <param name="html">
        /// The HTML to look through.
        /// </param>
        /// <returns>
        /// A list of Uri.
        /// </returns>
        public static List<Uri> FindLinks(string type, string html)
        {
            var matches = Regex.Matches(
                html, string.Format(Pattern, type), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var urls = new List<Uri>();

            foreach (var hrefMatch in
                matches.Cast<Match>().Where(match => match.Groups.Count == 2).Select(match => match.Groups[1].Value).
                    Select(link => HrefRegex.Match(link)).Where(hrefMatch => hrefMatch.Groups.Count == 2))
            {
                Uri url;
                var value = hrefMatch.Groups[1].Value;
                if (Uri.TryCreate(value, UriKind.Absolute, out url))
                {
                    urls.Add(url);
                }
            }

            return urls;
        }

        /// <summary>
        /// Converts all the relative paths in the specified content to absolute.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <param name="publishable">
        /// The publishable.
        /// </param>
        public static string ConvertPublishablePathsToAbsolute(string content, IPublishable publishable)
        {
            content = content.Replace(
                string.Format("\"{0}image.axd", publishable.Blog.AbsoluteWebRoot.AbsolutePath), "\"" + publishable.Blog.AbsoluteWebRoot + "image.axd");
            content = content.Replace(
                string.Format("\"{0}file.axd", publishable.Blog.AbsoluteWebRoot.AbsolutePath), "\"" + publishable.Blog.AbsoluteWebRoot + "file.axd");
            content = content.Replace($"href=\"{publishable.Blog.RelativeWebRoot}", $"href=\"{publishable.Blog.AbsoluteWebRoot}");

            content = content.Replace("href=\"/", $"href=\"{publishable.Blog.AbsoluteWebRoot}");

            return content;
        }

        /// <summary>
        /// Finds the semantic documents from a URL based on the type.
        /// </summary>
        /// <param name="url">
        /// The URL of the semantic document or a document containing semantic links.
        /// </param>
        /// <param name="type">
        /// The type. Could be "foaf", "apml" or "sioc".
        /// </param>
        /// <returns>
        /// A dictionary of the semantic documents. The dictionary is empty if no documents were found.
        /// </returns>
        public static Dictionary<Uri, XmlDocument> FindSemanticDocuments(Uri url, string type)
        {
            var list = new Dictionary<Uri, XmlDocument>();

            // ignoreRemoteDownloadSettings is set to true for the
            // RemoteFile instance for backwards compatibility.
            var remoteFile = new RemoteFile(url, true);
            var content = remoteFile.GetFileAsString();
            if (!string.IsNullOrEmpty(content))
            {
                var upper = content.ToUpperInvariant();

                if (upper.Contains("</HEAD") && upper.Contains("</HTML"))
                {
                    var urls = FindLinks(type, content);
                    foreach (var xmlUrl in urls)
                    {
                        var doc = LoadDocument(url, xmlUrl);
                        if (doc != null)
                        {
                            list.Add(xmlUrl, doc);
                        }
                    }
                }
                else
                {
                    var doc = LoadDocument(url, url);
                    if (doc != null)
                    {
                        list.Add(url, doc);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Returns the default culture.  This is either the culture specified in the blog settings,
        ///     or the default culture installed with the operating system.
        /// </summary>
        /// <returns>
        /// The default culture.
        /// </returns>
        public static CultureInfo GetDefaultCulture()
        {
            var settingsCulture = BlogSettings.Instance.Culture;
            if (String.IsNullOrWhiteSpace(settingsCulture) ||
                settingsCulture.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.InstalledUICulture;
            }

            return CultureInfo.CreateSpecificCulture(settingsCulture);
        }

        /// <summary>
        /// Gets the sub domain.
        /// </summary>
        /// <param name="url">
        /// The URL to get the sub domain from.
        /// </param>
        /// <returns>
        /// The sub domain.
        /// </returns>
        public static string GetSubDomain(Uri url)
        {
           
            if (url.HostNameType == UriHostNameType.Dns)
            {
                var host = url.Host;
                if (host.Split('.').Length > 2)
                {
                    var lastIndex = host.LastIndexOf(".");
                    var index = host.LastIndexOf(".", lastIndex - 1);
                    return host.Substring(0, index);
                }
            }

            return null;
        }

        /// <summary>
        /// Encrypts a string using the SHA256 algorithm.
        /// </summary>
        /// <param name="plainMessage">
        /// The plain Message.
        /// </param>
        /// <returns>
        /// The hash password.
        /// </returns>
        public static string HashPassword(string plainMessage)
        {
            var data = Encoding.UTF8.GetBytes(plainMessage);
            using (HashAlgorithm sha = new SHA256Managed())
            {
                sha.TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(sha.Hash);
            }
        }

        /// <summary>
        /// The body regex.
        /// </summary>
        private static readonly Regex BodyRegex = new Regex(@"\[UserControl:(.*?)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses user control references in content and returns the content with
        /// the [UserControl:...] tags replaced with placeholder text.
        /// In modern .NET, user controls are replaced by Blazor components or partial views.
        /// </summary>
        /// <param name="content">The string to parse for user controls.</param>
        /// <returns>The processed content string.</returns>
        public static string ProcessUserControlTags(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var matches = BodyRegex.Matches(content);
            foreach (Match match in matches)
            {
                var controlPath = match.Groups[1].Value.Trim();
                content = content.Replace(match.Value, $"<!-- UserControl: {WebUtility.HtmlEncode(controlPath)} -->");
            }
            return content;
        }

        private static readonly Regex emailRegex = new Regex(
            @"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.IgnoreCase);

        private static readonly Regex validIpV4AddressRegex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$", RegexOptions.IgnoreCase);
        private static readonly Regex validHostnameRegex = new Regex(@"^(([a-z]|[a-z][a-z0-9\-]*[a-z0-9])\.)*([a-z]|[a-z][a-z0-9\-]*[a-z0-9])$", RegexOptions.IgnoreCase);
        

        /// <summary>
        /// Email address by user name.
        /// In modern .NET, this delegates to the IMembershipService via BlogService.
        /// </summary>
        /// <param name="userName">User name</param>
        /// <returns>Email Address</returns>
        public static string GetUserEmail(string userName)
        {
            // REVIEW: In the migrated app, this should use IMembershipService.
            // For now, return the username as a fallback.
            return userName;
        }

        /// <summary>
        /// Validates an email address.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsEmailValid(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                return emailRegex.IsMatch(email.Trim());
            }

            return false;
        }

        /// <summary>
        /// Validates a host name.
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static bool IsHostnameValid(string hostname)
        {
            if (!string.IsNullOrWhiteSpace(hostname))
            {
                return validHostnameRegex.IsMatch(hostname.Trim());
            }

            return false;
        }

        /// <summary>
        /// Validates an IPv4 address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsIpV4AddressValid(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                return validIpV4AddressRegex.IsMatch(address.Trim());
            }

            return false;
        }

        /// <summary>
        /// Validates an IPv6 address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsIpV6AddressValid(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                IPAddress ip;
                if (IPAddress.TryParse(address, out ip))
                {
                    return ip.AddressFamily == AddressFamily.InterNetworkV6;
                }
            }
            return false;
        }

        /// <summary>
        /// Run through all code assemblies and creates object
        ///     instance for types marked with extension attribute
        /// </summary>
        public static void LoadExtensions()
        {
            var codeAssemblies = CodeAssemblies();

            var sortedExtensions = new List<SortedExtension>();

            foreach (Assembly a in codeAssemblies)
            {
                var types = a.GetTypes();
                
                Type extensionsAttribute = typeof(ExtensionAttribute);

                sortedExtensions.AddRange(
                    from type in types
                    let attributes = type.GetCustomAttributes(extensionsAttribute, false)
                    from attribute in attributes
                    where (attribute.GetType() == extensionsAttribute)
                    let ext = (ExtensionAttribute)attribute
                    select new SortedExtension(ext.Priority, type.Name, type.FullName));

                sortedExtensions.Sort(
                    (e1, e2) =>
                    e1.Priority == e2.Priority
                        ? string.CompareOrdinal(e1.Name, e2.Name)
                        : e1.Priority.CompareTo(e2.Priority));

                foreach (var x in sortedExtensions.Where(x => ExtensionManager.ExtensionEnabled(x.Name)))
                {
                    a.CreateInstance(x.Type);
                }
            }

            // initialize comment rules and filters
            CommentHandlers.Listen();
        }

        /// <summary>
        /// Sends a message to any subscribed log listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        public static void Log(object message)
        {
            if (OnLog != null)
            {
                OnLog(message, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sends a message to any subscribed log listeners.
        /// </summary>
        /// <param name="format">A format string</param>
        /// <param name="args">Arguments to replace in the format string</param>
		public static void Log(string format, params object[] args)
		{
			if (OnLog != null)
			{
				OnLog(string.Format(format, args), EventArgs.Empty);
			}
		}

        /// <summary>
        /// Sends a message to any subscribed log listeners.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="ex"></param>
        public static void Log(string methodName, Exception ex)
        {
            Log($"{methodName}: {ex.Message}");
        }

        /// <summary>
        /// Generates random password for password reset
        /// </summary>
        /// <returns>
        /// Random password
        /// </returns>
        public static string RandomPassword()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            var password = string.Empty;
            var random = new Random();

            for (var i = 0; i < 8; i++)
            {
                var x = random.Next(1, chars.Length);
                if (!password.Contains(chars.GetValue(x).ToString()))
                {
                    password += chars.GetValue(x);
                }
                else
                {
                    i--;
                }
            }

            return password;
        }

        /// <summary>
        /// Removes the HTML whitespace.
        /// </summary>
        /// <param name="html">
        /// The HTML to remove the whitespace from.
        /// </param>
        /// <returns>
        /// The html with the whitespace removed.
        /// </returns>
        public static string RemoveHtmlWhitespace(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            html = RegexBetweenTags.Replace(html, "> ");
            html = RegexLineBreaks.Replace(html, string.Empty);

            return html.Trim();
        }

        // RenderControl was a WebForms helper. In modern .NET, use Razor rendering.

        /// <summary>
        /// Strips all illegal characters from the specified title.
        /// </summary>
        /// <param name="text">
        /// The text to strip.
        /// </param>
        /// <returns>
        /// The remove illegal characters.
        /// </returns>
        public static string RemoveIllegalCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            text = text.Replace(":", string.Empty);
            text = text.Replace("/", string.Empty);
            text = text.Replace("?", string.Empty);
            text = text.Replace("#", string.Empty);
            text = text.Replace("[", string.Empty);
            text = text.Replace("]", string.Empty);
            text = text.Replace("@", string.Empty);
            text = text.Replace("*", string.Empty);
            text = text.Replace(".", string.Empty);
            text = text.Replace(",", string.Empty);
            text = text.Replace("\"", string.Empty);
            text = text.Replace("&", string.Empty);
            text = text.Replace("'", string.Empty);

            text = text.Replace("–", "-"); // live writer passes char 8211  this inplace of a char 45 for hyphen
            text = RemoveUnicodePunctuation(text); // moves any unicode versions of punctuation

            text = text.Replace(" ", "-");
            text = RemoveDiacritics(text);
            text = RemoveExtraHyphen(text);

            return WebUtility.HtmlEncode(text).Replace("%", string.Empty);
        }

        /// <summary>
        /// Sends a MailMessage object using the SMTP settings.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="smtpServer">SMTP server</param>
        /// <param name="smtpServerPort">SMTP server port</param>
        /// <param name="smtpUserName">User name</param>
        /// <param name="smtpPassword">Password</param>
        /// <param name="enableSsl">Enable SSL</param>
        /// <returns>
        /// Error message, if any.
        /// </returns>
        public static string SendMailMessage(MailMessage message, string smtpServer = "", string smtpServerPort = "",
            string smtpUserName = "", string smtpPassword = "", string enableSsl = "")
        {
            StringBuilder errorMsg = new StringBuilder();
            bool boolSssl = BlogSettings.Instance.EnableSsl;
            int intPort = BlogSettings.Instance.SmtpServerPort;

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            try
            {
                if (string.IsNullOrEmpty(smtpServer))
                    smtpServer = BlogSettings.Instance.SmtpServer;

                if (string.IsNullOrEmpty(smtpUserName))
                    smtpUserName = BlogSettings.Instance.SmtpUserName;

                if (string.IsNullOrEmpty(smtpPassword))
                    smtpPassword = BlogSettings.Instance.SmtpPassword;

                if (!string.IsNullOrEmpty(smtpServerPort))
                    intPort = int.Parse(smtpServerPort);

                if (!string.IsNullOrEmpty(enableSsl))
                    bool.TryParse(enableSsl, out boolSssl);

                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;
                var smtp = new SmtpClient(smtpServer);

                // don't send credentials if a server doesn't require it,
                // linux smtp servers don't like that 
                if (!string.IsNullOrEmpty(smtpUserName))
                {
                    smtp.Credentials = new NetworkCredential(smtpUserName, smtpPassword);
                }

                smtp.Port = intPort;
                smtp.EnableSsl = boolSssl;
                smtp.Send(message);
                OnEmailSent(message);
            }
            catch (Exception ex)
            {
                OnEmailFailed(message);

                errorMsg.Append("Error sending email in SendMailMessage: ");
                Exception current = ex;

                while (current != null)
                {
                    if (errorMsg.Length > 0) { errorMsg.Append(" "); }
                    errorMsg.Append(current.Message);
                    current = current.InnerException;
                }

                Utils.Log(errorMsg.ToString());
            }
            finally
            {
                // Remove the pointer to the message object so the GC can close the thread.
                message.Dispose();
            }

            return errorMsg.ToString();
        }

        /// <summary>
        /// Sends the mail message asynchronously in another thread.
        /// </summary>
        /// <param name="message">
        /// The message to send.
        /// </param>
        public static void SendMailMessageAsync(MailMessage message)
        {
            // Before entering a BG thread, retrieve the current instance blog settings.
            Guid blogId = Blog.CurrentInstance.Id;
            ThreadPool.QueueUserWorkItem(delegate
            {
                // because HttpContext is not available within this BG thread
                // needed to determine the current blog instance,
                // set override value here.
                Blog.InstanceIdOverride = blogId;

                SendMailMessage(message);
            });
        }

        /// <summary>
        /// Writes ETag and Last-Modified headers and checks conditional get headers.
        /// Returns true if a 304 Not Modified response should be sent.
        /// </summary>
        public static bool SetConditionalGetHeaders(DateTime date, HttpContext context)
        {
            if (context == null) return false;

            var now = date.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
            if (date > now || date.Year < 1900)
            {
                date = now;
            }

            var response = context.Response;
            var request = context.Request;

            var etag = $"\"{date.Ticks}\"";
            var incomingEtag = request.Headers["If-None-Match"].ToString();

            DateTime incomingLastModifiedDate;
            DateTime.TryParse(request.Headers["If-Modified-Since"].ToString(), out incomingLastModifiedDate);

            response.Headers["Last-Modified"] = date.ToString("R");
            response.Headers["Cache-Control"] = "public";
            response.Headers["ETag"] = etag;

            if (string.Equals(incomingEtag, etag) || incomingLastModifiedDate == date)
            {
                response.StatusCode = (int)HttpStatusCode.NotModified;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Overload for backward compatibility — uses static accessor.
        /// </summary>
        public static bool SetConditionalGetHeaders(DateTime date)
        {
            var context = _httpContextAccessor?.HttpContext;
            return context != null && SetConditionalGetHeaders(date, context);
        }

        /// <summary>
        /// Strips all HTML tags from the specified string.
        /// </summary>
        /// <param name="html">
        /// The string containing HTML
        /// </param>
        /// <returns>
        /// A string without HTML tags
        /// </returns>
        public static string StripHtml(string html)
        {
            return String.IsNullOrWhiteSpace(html) ? string.Empty : RegexStripHtml.Replace(html, string.Empty).Trim();
        }

        /// <summary>
        /// Translates the specified string using the resource files.
        /// </summary>
        /// <param name="text">
        /// The text to translate.
        /// </param>
        /// <returns>
        /// The translate.
        /// </returns>
        public static string Translate(string text)
        {
            return Translate(text, null, null);
        }

        /// <summary>
        /// Translates the specified string using the resource files.  If a translation
        ///     is not found, defaultValue will be returned.
        /// </summary>
        /// <param name="text">
        /// The text to translate.
        /// </param>
        /// <param name="defaultValue">
        /// The default Value.
        /// </param>
        /// <returns>
        /// The translate.
        /// </returns>
        public static string Translate(string text, string defaultValue)
        {
            return Translate(text, defaultValue, null);
        }

        /// <summary>
        /// Translates the specified string using the resource files and specified culture.
        ///     If a translation is not found, defaultValue will be returned.
        /// </summary>
        /// <param name="text">
        /// The text to translate.
        /// </param>
        /// <param name="defaultValue">
        /// The default Value.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <returns>
        /// The translate.
        /// </returns>
        public static string Translate(string text, string defaultValue, CultureInfo culture)
        {
            string resource = null;
            var manager = LabelsResourceManager;
            if (manager != null && !string.IsNullOrEmpty(text))
            {
                try
                {
                    resource = manager.GetString(text, culture ?? CultureInfo.CurrentUICulture);
                }
                catch (System.Resources.MissingManifestResourceException)
                {
                    resource = null;
                }
            }

            return resource ?? (string.IsNullOrEmpty(defaultValue) ? $"Missing Resource [{text}]" : defaultValue);
        }

        private static readonly Lazy<System.Resources.ResourceManager> labelsResourceManager =
            new Lazy<System.Resources.ResourceManager>(FindLabelsResourceManager);

        /// <summary>
        /// Resource manager for the global "labels" resources (replaces App_GlobalResources/labels.resx).
        /// The resources are embedded in the web application assembly as "{Assembly}.Resources.labels".
        /// </summary>
        public static System.Resources.ResourceManager LabelsResourceManager => labelsResourceManager.Value;

        private static System.Resources.ResourceManager FindLabelsResourceManager()
        {
            const string suffix = ".labels.resources";
            var candidates = new[] { Assembly.GetEntryAssembly() }
                .Concat(AppDomain.CurrentDomain.GetAssemblies())
                .Where(a => a != null && !a.IsDynamic);

            foreach (var assembly in candidates)
            {
                string name;
                try
                {
                    name = assembly.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                if (name != null)
                {
                    return new System.Resources.ResourceManager(name.Substring(0, name.Length - ".resources".Length), assembly);
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to quickly check if directory or file is writable
        /// </summary>
        /// <param name="url">Physical path</param>
        /// <param name="file">Optional File name</param>
        /// <returns>True if application can write file/directory</returns>
        public static bool CanWrite(string url, string file = "")
        {
            var dir = BlogConfig.MapPath(url);

            if (dir != null && Directory.Exists(dir))
            {
                if (string.IsNullOrEmpty(file)) 
                    file = $"test{DateTime.Now.ToString("ddmmhhssss")}.txt";

                try
                {
                    var p = Path.Combine(dir, file);

                    using (var fs = new FileStream(p, FileMode.Create))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(@"test");
                    }

                    File.Delete(p);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Recursively copies the contents of the source directory to the target directory, 
        /// without exclusions.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="target">The target directory.</param>
        public static void CopyDirectoryContents(DirectoryInfo source, DirectoryInfo target)
        {
            CopyDirectoryContents(source, target, null);
        }

        /// <summary>
        /// Recursively copies the contents of the source directory to the target directory, with 
        /// an optional list of directories to exclude.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="target">The target directory.</param>
        /// <param name="directoryNamesToExclude">An optional list of directory names to exclude from the copy.</param>
        public static void CopyDirectoryContents(DirectoryInfo source, DirectoryInfo target, List<string> directoryNamesToExclude)
        {
            foreach (DirectoryInfo dir in source.GetDirectories().Where(d => (directoryNamesToExclude ?? new List<string>()).FirstOrDefault(dn => dn.Equals(d.Name, StringComparison.OrdinalIgnoreCase)) == null))
                CopyDirectoryContents(dir, target.CreateSubdirectory(dir.Name), directoryNamesToExclude);

            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        /// <summary>
        /// If the directory does not exist, it is created.
        /// </summary>
        /// <param name="directoryName">The name of the directory to create.</param>
        /// <returns></returns>
        public static bool CreateDirectoryIfNotExists(string directoryName)
        {
            try
            {
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
            }
            catch (Exception ex)
            {
                Utils.Log("Utils.CreateDirectoryIfNotExists", ex);
                throw;  // re-throw error so error message bubbles up.
            }

            return true;
        }

        /// <summary>
        /// Recycle application (touch appsettings.json to trigger reload).
        /// </summary>
        public static bool RecycleIIS()
        {
            try
            {
                string configPath = Path.Combine(BlogConfig.ContentRootPath, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    System.IO.File.SetLastWriteTimeUtc(configPath, DateTime.UtcNow);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log("Utils.Recycle() : " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Gets the client's IP address.
        /// This method takes into account the X-Forwarded-For header,
        /// in case the blog is hosted behind a load balancer or proxy.
        /// </summary>
        /// <returns>The client's IP address.</returns>
        public static string GetClientIP()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                var request = context.Request;
                string xff = request.Headers["X-Forwarded-For"];
                string clientIP = string.Empty;
                if (!string.IsNullOrWhiteSpace(xff))
                {
                    int idx = xff.IndexOf(',');
                    if (idx > 0)
                    {
                        clientIP = xff.Substring(0, idx);
                    }
                    else
                    {
                        clientIP = xff;
                    }
                }

                if (string.IsNullOrWhiteSpace(clientIP))
                {
                    clientIP = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                }
                return clientIP;
            }

            return string.Empty;
        }

        #endregion

        #region Methods

        /// <summary>
        /// To support compiled extensions
        ///     This methed looks for DLLs in the "/bin" folder
        ///     and if assembly compiled with configuration
        ///     attributed set to "BlogEngineExtension" it will
        ///     be added to the list of code assemlies
        /// </summary>
        private static IEnumerable<Assembly> GetCompiledExtensions()
        {
            // Drop-in extension assemblies deployed next to the application (formerly ~/bin)
            var assemblies = new List<Assembly>();
            var entryAssembly = Assembly.GetEntryAssembly();
            var coreName = typeof(Utils).Assembly.GetName().Name;

            foreach (var fileName in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                try
                {
                    var asm = Assembly.LoadFrom(fileName);
                    if (asm == entryAssembly || !asm.GetReferencedAssemblies().Any(r => r.Name == coreName))
                        continue;

                    // check if assembly can be loaded before adding to collection
                    asm.GetTypes();
                    assemblies.Add(asm);
                }
                catch (Exception)
                {
                    // any third party DLL can throw here, not a critical error
                }
            }
            return assemblies;
        }

        /// <summary>
        /// Loads the document.
        /// </summary>
        /// <param name="url">
        /// The URL of the document to load.
        /// </param>
        /// <param name="xmlUrl">
        /// The XML URL.
        /// </param>
        /// <returns>
        /// The XmlDocument.
        /// </returns>
        private static XmlDocument LoadDocument(Uri url, Uri xmlUrl)
        {
            string absoluteUrl;

            if (url.IsAbsoluteUri)
            {
                absoluteUrl = url.ToString();
            }
            else if (!url.ToString().StartsWith("/"))
            {
                absoluteUrl = url + xmlUrl.ToString();
            }
            else
            {
                absoluteUrl = $"{url.Scheme}://{url.Authority}{xmlUrl}";
            }

            var readerSettings = new XmlReaderSettings
                {
                    MaxCharactersFromEntities = 1024, XmlResolver = new XmlSafeResolver() 
                };

            XmlDocument doc;
            try
            {
                using (var reader = XmlReader.Create(absoluteUrl, readerSettings))
                {
                    doc = new XmlDocument();
                    doc.Load(reader);
                }
            }
            catch (Exception)
            {
                return null;
            }
            
            return doc;
        }

        /// <summary>
        /// The on email failed.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private static void OnEmailFailed(MailMessage message)
        {
            if (EmailFailed != null)
            {
                EmailFailed(message, EventArgs.Empty);
            }
        }

        /// <summary>
        /// The on email sent.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private static void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">
        /// The text to remove diacritics from.
        /// </param>
        /// <returns>
        /// The string with the diacritics removed.
        /// </returns>
        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in
                normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes any unicode punctuation
        /// </summary>
        /// <param name="text">
        /// The text to remove punctuation from.
        /// </param>
        /// <returns>
        /// The string with the punctuation removed.
        /// </returns>
        private static string RemoveUnicodePunctuation(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in
                normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.InitialQuotePunctuation &&
                                      CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.FinalQuotePunctuation))
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes the extra hyphen.
        /// </summary>
        /// <param name="text">
        /// The text to remove the extra hyphen from.
        /// </param>
        /// <returns>
        /// The text with the extra hyphen removed.
        /// </returns>
        private static string RemoveExtraHyphen(string text)
        {
            if (text.Contains("--"))
            {
                text = text.Replace("--", "-");
                return RemoveExtraHyphen(text);
            }

            return text;
        }

        #endregion
    }
}
