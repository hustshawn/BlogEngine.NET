using System;
using System.Text;
using System.Globalization;

namespace BlogEngine.Core.Web.Scripting
{
    /// <summary>
    /// Helper methods for script manipulations.
    /// In modern .NET, these return HTML strings instead of manipulating WebForms controls.
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// Returns a stylesheet link tag as HTML string.
        /// </summary>
        public static string AddStyle(string lnk)
        {
            return $"\n<link href=\"{lnk}\" rel=\"stylesheet\" type=\"text/css\" />";
        }

        /// <summary>
        /// Returns a generic link tag as HTML string.
        /// </summary>
        public static string AddGenericLink(string type, string relation, string title, string href)
        {
            var tp = string.IsNullOrEmpty(type) ? "" : $"type=\"{type}\" ";
            return $"\n<link {tp}rel=\"{relation}\" title=\"{title}\" href=\"{href}\" />";
        }

        /// <summary>
        /// Returns a script tag as HTML string.
        /// </summary>
        public static string AddScript(string src, bool defer = false, bool asnc = false)
        {
            var d = defer ? " defer=\"defer\"" : "";
            var a = asnc ? " async=\"async\"" : "";
            return $"\n<script type=\"text/javascript\" src=\"{src}\"{d}{a}></script>";
        }

        /// <summary>
        /// Format inline script.
        /// </summary>
        public static string FormatInlineScript(string script)
        {
            var sb = new StringBuilder();
            sb.Append("\n<script type=\"text/javascript\"> \n");
            sb.Append("//<![CDATA[ \n");
            sb.Append(script).Append(" \n");
            sb.Append("//]]> \n");
            sb.Append("</script> \n");
            return sb.ToString();
        }

        #region BlogBasePage helpers

        /// <summary>
        /// Gets custom code for the HTML head section.
        /// </summary>
        public static string GetCustomCodeForHead()
        {
            if (string.IsNullOrEmpty(BlogSettings.Instance.HtmlHeader))
                return string.Empty;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}<!-- Start custom code -->{0}{1}{0}<!-- End custom code -->{0}",
                Environment.NewLine,
                BlogSettings.Instance.HtmlHeader);
        }

        /// <summary>
        /// Gets tracking script HTML for the bottom of the page.
        /// </summary>
        public static string GetTrackingScript(string scheme = "https")
        {
            var sb = new StringBuilder();

            if (BlogSettings.Instance.CommentProvider == BlogSettings.CommentsBy.Disqus && BlogSettings.Instance.IsCommentsEnabled)
            {
                sb.Append("<script type=\"text/javascript\"> \n");
                sb.Append("//<![CDATA[ \n");
                sb.Append("(function() { ");
                sb.Append("var links = document.getElementsByTagName('a'); ");
                sb.Append("var query = '?'; ");
                sb.Append("for(var i = 0; i < links.length; i++) { ");
                sb.Append("if(links[i].href.indexOf('#comment') >= 0) { ");
                sb.Append("query += 'url' + i + '=' + encodeURIComponent(links[i].href) + '&'; ");
                sb.Append("}}");
                sb.Append("document.write('<script charset=\"utf-8\" type=\"text/javascript\" src=\"" + scheme + "://disqus.com/forums/");
                sb.Append(BlogSettings.Instance.DisqusWebsiteName);
                sb.Append("/get_num_replies.js' + query + '\"></' + 'script>'); ");
                sb.Append("})(); \n");
                sb.Append("//]]> \n");
                sb.Append("</script> \n");
            }

            if (!string.IsNullOrEmpty(BlogSettings.Instance.TrackingScript))
            {
                sb.Append(BlogSettings.Instance.TrackingScript);
            }

            return sb.ToString();
        }

        #endregion
    }
}
