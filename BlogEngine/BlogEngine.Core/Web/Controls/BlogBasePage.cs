using System.IO;

namespace BlogEngine.Core.Web.Controls
{
    using System;

    /// <summary>
    /// Abstract base class for blog pages. In the original WebForms architecture,
    /// all pages inherited from this class (which extended System.Web.UI.Page).
    /// Now converted to a plain abstract class that exposes the same logic as methods
    /// callable by the web project (Blazor/MVC/Razor Pages).
    /// </summary>
    public abstract class BlogBasePage
    {
        #region Public Methods

        /// <summary>
        /// Generates the generic link HTML tag.
        /// </summary>
        /// <param name="relation">The relation string.</param>
        /// <param name="title">The title string.</param>
        /// <param name="href">The href string.</param>
        /// <returns>HTML string for the link tag.</returns>
        public virtual string GenerateGenericLink(string relation, string title, string href)
        {
            return GenerateGenericLink(null, relation, title, href);
        }

        /// <summary>
        /// Generates the generic link HTML tag.
        /// </summary>
        /// <param name="type">The type string.</param>
        /// <param name="relation">The relation string.</param>
        /// <param name="title">The title string.</param>
        /// <param name="href">The href string.</param>
        /// <returns>HTML string for the link tag.</returns>
        public virtual string GenerateGenericLink(string type, string relation, string title, string href)
        {
            if (!string.IsNullOrEmpty(type))
            {
                return $"<link type=\"{type}\" rel=\"{relation}\" title=\"{title}\" href=\"{href}\" />";
            }
            return $"<link rel=\"{relation}\" title=\"{title}\" href=\"{href}\" />";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the default language headers for the page.
        /// </summary>
        /// <returns>Dictionary of header name to value.</returns>
        public virtual System.Collections.Generic.Dictionary<string, string> GetDefaultLanguageHeaders()
        {
            return new System.Collections.Generic.Dictionary<string, string>
            {
                { "Content-Style-Type", "text/css" },
                { "Content-Script-Type", "text/javascript" }
            };
        }

        /// <summary>
        /// Generates the content-type meta tag HTML string.
        /// </summary>
        /// <param name="contentType">The content type (e.g., "text/html").</param>
        /// <param name="charsetName">The charset name (e.g., "utf-8").</param>
        /// <returns>HTML string for the meta tag.</returns>
        public virtual string GenerateMetaContentType(string contentType = "text/html", string charsetName = "utf-8")
        {
            return $"<meta http-equiv=\"content-type\" content=\"{contentType}; charset={charsetName}\" />";
        }

        /// <summary>
        /// Generates a meta tag HTML string.
        /// </summary>
        /// <param name="name">The tag name.</param>
        /// <param name="value">The tag value.</param>
        /// <returns>HTML string for the meta tag, or empty string if name/value is empty.</returns>
        public virtual string GenerateMetaTag(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                return string.Empty;

            return $"\n\t<meta name=\"{name}\" content=\"{value}\" />";
        }

        /// <summary>
        /// Gets the standard header links for a blog page (RSS, RSD, SIOC, APML, FOAF, OpenSearch).
        /// </summary>
        /// <returns>Array of link HTML strings to include in the page header.</returns>
        public virtual string[] GetHeaderLinks()
        {
            string relativeWebRoot = Utils.RelativeWebRoot;
            Uri absoluteWebRoot = Utils.AbsoluteWebRoot;
            string instanceName = BlogSettings.Instance.Name;

            var links = new System.Collections.Generic.List<string>();

            links.Add(GenerateGenericLink("", "contents", "Archive", $"{relativeWebRoot}archive{BlogConfig.FileExtension}"));
            links.Add(GenerateGenericLink("", "start", instanceName, relativeWebRoot));
            links.Add(GenerateGenericLink("application/rsd+xml", "edituri", "RSD", $"{absoluteWebRoot}rsd.axd"));
            links.Add(GenerateGenericLink("application/rdf+xml", "meta", "SIOC", $"{absoluteWebRoot}sioc.axd"));
            links.Add(GenerateGenericLink("application/apml+xml", "meta", "APML", $"{absoluteWebRoot}apml.axd"));
            links.Add(GenerateGenericLink("application/rdf+xml", "meta", "FOAF", $"{absoluteWebRoot}foaf.axd"));

            if (string.IsNullOrEmpty(BlogSettings.Instance.AlternateFeedUrl))
            {
                links.Add(GenerateGenericLink("application/rss+xml", "alternate", $"{instanceName} (RSS)", $"{Utils.FeedUrl}"));
                links.Add(GenerateGenericLink("application/atom+xml", "alternate", $"{instanceName} (ATOM)", $"{Utils.FeedUrl}?format=atom"));
            }
            else
            {
                links.Add(GenerateGenericLink("application/rss+xml", "alternate", instanceName, Utils.FeedUrl));
            }

            if (BlogSettings.Instance.EnableOpenSearch)
            {
                links.Add(GenerateGenericLink("application/opensearchdescription+xml", "search", instanceName, $"{absoluteWebRoot}opensearch.axd"));
            }

            return links.ToArray();
        }

        /// <summary>
        /// Checks if the current user is allowed to view the page.
        /// </summary>
        /// <returns>True if viewing is allowed.</returns>
        public virtual bool IsViewingAllowed()
        {
            if (Security.IsAuthorizedTo(Rights.ViewPublicPosts))
                return true;

            if (Security.IsAuthenticated && Security.GetCurrentUserRoles().Length == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the page title with blog name prefix if configured.
        /// </summary>
        /// <param name="pageTitle">The original page title.</param>
        /// <returns>The formatted page title.</returns>
        public virtual string GetFormattedTitle(string pageTitle)
        {
            if (BlogSettings.Instance.UseBlogNameInPageTitles)
            {
                return $"{BlogSettings.Instance.Name} | {pageTitle}";
            }
            return pageTitle;
        }

        /// <summary>
        /// Gets the appropriate master/layout based on the request path.
        /// Returns the theme-relative path to the layout file.
        /// </summary>
        /// <param name="requestPath">The current request path.</param>
        /// <param name="contentRootPath">The content root path for file existence checks.</param>
        /// <returns>Path to the master/layout file.</returns>
        public string GetSiteMaster(string requestPath, string contentRootPath)
        {
            if (requestPath != null && requestPath.Contains("post.aspx", StringComparison.InvariantCultureIgnoreCase))
            {
                string path = $"{Utils.ApplicationRelativeWebRoot}Custom/Themes/{BlogSettings.Instance.Theme}/post.master";
                string physicalPath = System.IO.Path.Combine(contentRootPath, path.TrimStart('~', '/'));
                if (File.Exists(physicalPath))
                    return path;
            }

            // if page.master exists, use it for all common pages
            // and site.master only for post list
            if (IsPageMasterPath(requestPath))
            {
                string path = $"{Utils.ApplicationRelativeWebRoot}Custom/Themes/{BlogSettings.Instance.Theme}/page.master";
                string physicalPath = System.IO.Path.Combine(contentRootPath, path.TrimStart('~', '/'));
                if (File.Exists(physicalPath))
                    return path;
            }

            var siteMaster = $"{Utils.ApplicationRelativeWebRoot}Custom/Themes/{BlogSettings.Instance.GetThemeWithAdjustments(null)}/site.master";
            string siteMasterPhysical = System.IO.Path.Combine(contentRootPath, siteMaster.TrimStart('~', '/'));

            return File.Exists(siteMasterPhysical)
                ? siteMaster
                : $"{Utils.ApplicationRelativeWebRoot}Custom/Themes/Standard/site.master";
        }

        private bool IsPageMasterPath(string requestPath)
        {
            if (requestPath == null) return false;
            return requestPath.Contains("page.aspx", StringComparison.InvariantCultureIgnoreCase) ||
                   requestPath.Contains("archive.aspx", StringComparison.InvariantCultureIgnoreCase) ||
                   requestPath.Contains("contact.aspx", StringComparison.InvariantCultureIgnoreCase) ||
                   requestPath.Contains("error.aspx", StringComparison.InvariantCultureIgnoreCase) ||
                   requestPath.Contains("error404.aspx", StringComparison.InvariantCultureIgnoreCase) ||
                   requestPath.Contains("search.aspx", StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
    }
}
