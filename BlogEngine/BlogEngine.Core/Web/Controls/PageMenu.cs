using System;
using System.Linq;
using System.Net;
using System.Text;

namespace BlogEngine.Core.Web.Controls
{
    /// <summary>
    /// POCO/service class for page menu generation.
    /// Converted from WebForms-dependent class to plain class.
    /// Replaces HtmlGenericControl/HtmlAnchor/HtmlTextWriter with StringBuilder-based HTML generation.
    /// Replaces Blog._httpContextAccessor?.HttpContext with a requestPath parameter.
    /// </summary>
    public class PageMenu
    {
        #region Properties

        private bool _ulIdSet = false;
        private string _curPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageMenu"/> class.
        /// </summary>
        /// <param name="requestPath">The current request path (replaces Blog._httpContextAccessor?.HttpContext.Request.RawUrl).</param>
        public PageMenu(string requestPath = "")
        {
            _curPage = WebUtility.UrlEncode(GetPageName((requestPath ?? string.Empty).ToLower()));
        }

        /// <summary>
        /// Home label.
        /// </summary>
        public string Home { get; set; }

        /// <summary>
        /// Contact label.
        /// </summary>
        public string Contact { get; set; }

        /// <summary>
        /// Archive label.
        /// </summary>
        public string Archive { get; set; }

        /// <summary>
        /// Logon label.
        /// </summary>
        public string Logon { get; set; }

        /// <summary>
        /// Logoff label.
        /// </summary>
        public string Logoff { get; set; }

        /// <summary>
        /// The HTML for the control to display on the page.
        /// </summary>
        public string Html
        {
            get
            {
                return BindPages();
            }
        }

        #endregion

        private string BindPages()
        {
            var sb = new StringBuilder();
            _ulIdSet = false;

            // recursively get all children of the root page
            BuildChildren(sb, Guid.Empty);

            // items that will be appended to the end of menu list
            AppendMenuItem(sb, Contact, Utils.RelativeWebRoot + "contact.aspx");

            if (Security.IsAuthenticated)
            {
                AppendMenuItem(sb, Logoff, Utils.RelativeWebRoot + "Account/login.aspx?logoff");
            }
            else
            {
                AppendMenuItem(sb, Logon, Utils.RelativeWebRoot + "Account/login.aspx");
            }

            sb.Append("</ul>");
            return sb.ToString();
        }

        bool HasChildren(Guid pageId)
        {
            foreach (BlogEngine.Core.Page page in BlogEngine.Core.Page.Pages.OrderBy(page => page.SortOrder))
            {
                if (page.ShowInList && page.IsPublished)
                {
                    if (page.Parent == pageId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void BuildChildren(StringBuilder sb, Guid parentId)
        {
            if (!_ulIdSet)
            {
                sb.Append("<ul id=\"menu-topmenu\" class=\"menu\">");
                _ulIdSet = true;

                AppendMenuItem(sb, Home, Utils.RelativeWebRoot + "default.aspx");
                AppendMenuItem(sb, Archive, Utils.RelativeWebRoot + "archive.aspx");
            }
            else
            {
                sb.Append("<ul>");
            }

            foreach (BlogEngine.Core.Page page in BlogEngine.Core.Page.Pages)
            {
                if (page.ShowInList && page.IsPublished)
                {
                    if (page.Parent == parentId)
                    {
                        sb.Append("<li>");
                        string pageName = WebUtility.UrlEncode(GetPageName(page.RelativeLink.ToString().ToLower()));

                        string cssClass = (pageName == _curPage) ? " class=\"current\"" : "";
                        sb.AppendFormat("<a href=\"{0}\" title=\"{1}\"{2}>{3}</a>",
                            WebUtility.HtmlEncode(page.RelativeLink.ToString()),
                            WebUtility.HtmlEncode(page.Description ?? ""),
                            cssClass,
                            WebUtility.HtmlEncode(page.Title));

                        if (HasChildren(page.Id))
                        {
                            BuildChildren(sb, page.Id);
                            sb.Append("</ul>");
                        }
                        sb.Append("</li>");
                    }
                }
            }
        }

        private void AppendMenuItem(StringBuilder sb, string pageName, string pageUrl)
        {
            sb.Append("<li>");
            string cssClass = (GetPageName(pageUrl).ToLower() == (_curPage ?? "").ToLower()) ? " class=\"current\"" : "";
            sb.AppendFormat("<a href=\"{0}\" title=\"{1}\"{2}>{1}</a>",
                WebUtility.HtmlEncode(pageUrl),
                WebUtility.HtmlEncode(pageName ?? ""),
                cssClass);
            sb.Append("</li>");
        }

        static string GetPageName(string requestPath)
        {
            if (requestPath.IndexOf('?') != -1)
                requestPath = requestPath.Substring(0, requestPath.IndexOf('?'));
            return requestPath.Remove(0, requestPath.LastIndexOf("/") + 1).ToLower();
        }
    }
}
