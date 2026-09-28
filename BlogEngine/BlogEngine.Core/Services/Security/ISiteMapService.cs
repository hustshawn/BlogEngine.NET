namespace BlogEngine.Core.Services.Security
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Service interface for site map / navigation operations.
    /// Replaces System.Web.SiteMapProvider.
    /// </summary>
    public interface ISiteMapService
    {
        /// <summary>
        /// Finds a navigation node by raw URL.
        /// </summary>
        NavigationNode FindNode(string rawUrl);

        /// <summary>
        /// Gets the child nodes of a specific node.
        /// </summary>
        IList<NavigationNode> GetChildNodes(NavigationNode node);

        /// <summary>
        /// Gets the parent node of a specific node.
        /// </summary>
        NavigationNode GetParentNode(NavigationNode node);

        /// <summary>
        /// Gets the root node.
        /// </summary>
        NavigationNode GetRootNode();

        /// <summary>
        /// Returns whether the given node is accessible to the current user based on rights.
        /// </summary>
        bool IsAccessibleToUser(NavigationNode node);
    }

    /// <summary>
    /// Represents a navigation/site-map node, replacing System.Web.SiteMapNode.
    /// </summary>
    public class NavigationNode
    {
        /// <summary>
        /// Gets or sets the unique key for this node.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL for this node.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title for this node.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description for this node.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional attributes for the node.
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets an attribute by name, returning null if not found.
        /// </summary>
        public string this[string attributeName]
        {
            get => Attributes.TryGetValue(attributeName, out var val) ? val : null;
            set => Attributes[attributeName] = value;
        }

        /// <summary>
        /// Creates a new navigation node.
        /// </summary>
        public NavigationNode()
        {
        }

        /// <summary>
        /// Creates a new navigation node with specified values.
        /// </summary>
        public NavigationNode(string key, string url, string title, string description = "")
        {
            Key = key;
            Url = url;
            Title = title;
            Description = description;
        }
    }
}
