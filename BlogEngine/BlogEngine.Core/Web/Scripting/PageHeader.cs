using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BlogEngine.Core.Web.Scripting
{
    /// <summary>
    /// Class to process links dynamically injected into page header.
    /// In modern .NET, this generates HTML strings instead of WebForms controls.
    /// </summary>
    public class PageHeader
    {
        private readonly List<string> _headerLinks = new List<string>();

        /// <summary>
        /// Add link to collection.
        /// </summary>
        public void AddLink(string type, string rel, string title, string href)
        {
            string link;
            if (!string.IsNullOrEmpty(type))
            {
                link = $"\n\t<link type=\"{type}\" rel=\"{rel}\" title=\"{title}\" href=\"{href}\" />";
            }
            else
            {
                link = $"\n\t<link rel=\"{rel}\" title=\"{title}\" href=\"{href}\" />";
            }
            if (!_headerLinks.Contains(link))
            {
                _headerLinks.Add(link);
            }
        }

        /// <summary>
        /// Renders all header links, styles, and scripts as an HTML string.
        /// </summary>
        public string RenderToString()
        {
            var sb = new StringBuilder();
            foreach (var link in _headerLinks)
            {
                sb.Append(link);
            }
            foreach (var style in GetStyles())
            {
                sb.Append(style);
            }
            foreach (var script in GetScripts())
            {
                sb.Append(script);
            }
            return sb.ToString();
        }

        #region Private Methods

        private List<string> GetStyles()
        {
            var headerStyles = new List<string>();
            var tmpl = "\n\t<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />";

            foreach (var f in GetFiles($"{Utils.ApplicationRelativeWebRoot}Content/Auto"))
            {
                var href = $"{Utils.ApplicationRelativeWebRoot}Content/Auto/{f}";
                headerStyles.Add(string.Format(tmpl, href));
            }
            return headerStyles;
        }

        private List<string> GetScripts()
        {
            var headerScripts = new List<string>();
            var tmpl = "\n\t<script type=\"text/javascript\" src=\"{0}\"></script>";
            var lang = BlogSettings.Instance.Language;

            if (BlogSettings.Instance.Culture.ToLower() != "auto")
                lang = BlogSettings.Instance.Culture;

            var rsrc = HttpHandlers.ResourceEndpoint.GetScriptPath(new CultureInfo(lang));
            headerScripts.Add(string.Format(tmpl, rsrc));

            foreach (var f in GetFiles($"{Utils.ApplicationRelativeWebRoot}Scripts/Auto"))
            {
                var href = $"{Utils.ApplicationRelativeWebRoot}Scripts/Auto/{f}";
                headerScripts.Add(string.Format(tmpl, href));
            }
            return headerScripts;
        }

        private static List<string> GetFiles(string url)
        {
            var files = new List<string>();
            string path = BlogConfig.MapPath(url);

            var folder = new DirectoryInfo(path);
            if (folder.Exists)
            {
                foreach (var file in folder.GetFiles())
                {
                    files.Add(file.Name);
                }
            }
            return files;
        }

        #endregion
    }
}
