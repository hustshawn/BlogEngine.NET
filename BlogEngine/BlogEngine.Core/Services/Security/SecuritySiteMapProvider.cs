namespace BlogEngine.Core
{
    using System;
    using System.Collections.Generic;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// Implementation of ISiteMapService that is Rights aware.
    /// Replaces the legacy SecuritySiteMapProvider (XmlSiteMapProvider).
    /// </summary>
    public class SecuritySiteMapService : ISiteMapService
    {
        /// <summary>
        /// Finds a navigation node by raw URL — not implemented for security site map.
        /// </summary>
        public NavigationNode FindNode(string rawUrl)
        {
            // Security site map doesn't resolve nodes by URL.
            return null;
        }

        /// <summary>
        /// Gets the child nodes of a specific node — not implemented for security site map.
        /// </summary>
        public IList<NavigationNode> GetChildNodes(NavigationNode node)
        {
            return new List<NavigationNode>();
        }

        /// <summary>
        /// Gets the parent node of a specific node — not implemented for security site map.
        /// </summary>
        public NavigationNode GetParentNode(NavigationNode node)
        {
            return null;
        }

        /// <summary>
        /// Gets the root node — not implemented for security site map.
        /// </summary>
        public NavigationNode GetRootNode()
        {
            return null;
        }

        /// <summary>
        /// Returns whether the given navigation node is accessible to the current user.
        /// Checks Rights and other custom attributes on the node.
        /// </summary>
        public bool IsAccessibleToUser(NavigationNode node)
        {
            if (node == null)
                return false;

            // Check if primary blog instance only
            bool primaryBlogInstanceOnly;
            var primaryBlogAttr = node["primaryBlogInstanceOnly"];
            if (!string.IsNullOrWhiteSpace(primaryBlogAttr) && bool.TryParse(primaryBlogAttr, out primaryBlogInstanceOnly))
            {
                if (primaryBlogInstanceOnly && !Blog.CurrentInstance.IsPrimary)
                {
                    return false;
                }
            }

            var rightsAttr = node["rights"];
            if (!string.IsNullOrWhiteSpace(rightsAttr))
            {
                // By default, all specified Rights must exist.
                // We allow this to be overridden via the "rightsAuthorizationCheck" attribute.
                AuthorizationCheck authCheck = AuthorizationCheck.HasAll;
                var authCheckAttr = node["rightsAuthorizationCheck"];
                if (!string.IsNullOrWhiteSpace(authCheckAttr))
                {
                    authCheck = Utils.ParseEnum<AuthorizationCheck>(authCheckAttr, AuthorizationCheck.HasAll);
                }

                string[] rightsRaw = rightsAttr.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

                List<Rights> rightsToCheck = new List<Rights>();
                foreach (string r in rightsRaw)
                {
                    Rights right = Utils.ParseEnum<Rights>(r.Trim(), Rights.None);
                    if (right != Rights.None)
                        rightsToCheck.Add(right);
                }

                if (rightsToCheck.Count > 0)
                {
                    return Security.IsAuthorizedTo(authCheck, rightsToCheck.ToArray());
                }
            }

            return true;
        }
    }
}
