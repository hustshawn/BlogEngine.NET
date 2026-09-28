namespace BlogEngine.Core.Web.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// A site map service for the Pages in BlogEngine.
    /// Replaces the legacy PageSiteMap (SiteMapProvider).
    /// </summary>
    public class PageSiteMap : ISiteMapService
    {
        /// <summary>
        /// Finds a navigation node by raw URL.
        /// </summary>
        public NavigationNode FindNode(string rawUrl)
        {
            var i = rawUrl.IndexOf('?');
            var url = rawUrl;
            if (i > 0)
            {
                url = rawUrl.Substring(0, i);
            }

            var start = url.LastIndexOf('/') + 1;
            var stop = url.LastIndexOf('.');
            url = url.Substring(start, stop - start);

            return Page.Pages
                .Where(page => page.IsVisible && url.Equals(Utils.RemoveIllegalCharacters(page.Title), StringComparison.OrdinalIgnoreCase))
                .Select(page => new NavigationNode(page.Id.ToString(), page.RelativeLink, page.Title, page.Description)).FirstOrDefault();
        }

        /// <summary>
        /// Gets the child nodes of a specific node.
        /// </summary>
        public IList<NavigationNode> GetChildNodes(NavigationNode node)
        {
            var col = new List<NavigationNode>();
            var id = new Guid(node.Key);
            foreach (var page in Page.Pages.Where(page => page.IsVisible && page.Parent == id && page.ShowInList))
            {
                col.Add(new NavigationNode(page.Id.ToString(), page.RelativeLink, page.Title, page.Description));
            }

            return col;
        }

        /// <summary>
        /// Gets the parent node of a specific node.
        /// </summary>
        public NavigationNode GetParentNode(NavigationNode node)
        {
            var id = new Guid(node.Key);
            var parentId = Page.GetPage(id).Parent;
            if (parentId != Guid.Empty && parentId != id)
            {
                var parent = Page.GetPage(parentId);
                if (parent.IsVisible)
                {
                    return new NavigationNode(parent.Id.ToString(), parent.RelativeLink, parent.Title, parent.Description);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the root node.
        /// </summary>
        public NavigationNode GetRootNode()
        {
            var page = Page.GetFrontPage();
            return page != null
                ? new NavigationNode(page.Id.ToString(), page.RelativeLink, page.Title)
                : new NavigationNode(Guid.Empty.ToString(), "~/", "Home");
        }

        /// <summary>
        /// Returns whether the given node is accessible to the current user.
        /// For PageSiteMap, all visible pages are accessible.
        /// </summary>
        public bool IsAccessibleToUser(NavigationNode node)
        {
            return true;
        }
    }
}
