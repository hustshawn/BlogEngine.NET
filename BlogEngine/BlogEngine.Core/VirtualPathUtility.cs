namespace BlogEngine.Core
{
    /// <summary>
    /// Compatibility helper replacing System.Web.VirtualPathUtility.
    /// Provides virtual path manipulation for ASP.NET Core.
    /// </summary>
    public static class VirtualPathUtility
    {
        /// <summary>
        /// Converts a virtual path to an absolute path (removes ~/).
        /// </summary>
        public static string ToAbsolute(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return "/";

            if (virtualPath.StartsWith("~/"))
                return "/" + virtualPath.Substring(2);

            if (!virtualPath.StartsWith("/"))
                return "/" + virtualPath;

            return virtualPath;
        }

        /// <summary>
        /// Appends a trailing slash to a path if not already present.
        /// </summary>
        public static string AppendTrailingSlash(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return "/";

            return virtualPath.EndsWith("/") ? virtualPath : virtualPath + "/";
        }
    }
}
