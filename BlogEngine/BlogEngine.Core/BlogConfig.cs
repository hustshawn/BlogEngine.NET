using System;
using Microsoft.Extensions.Configuration;

namespace BlogEngine.Core
{
    /// <summary>
    ///     Wrapper class for accessing Blog configuration settings.
    ///     Settings are loaded from IConfiguration (appsettings.json).
    /// </summary>
    public class BlogConfig
    {
        /// <summary>
        /// The default storage location virtual path (App_Data).
        /// </summary>
        public const string DefaultStorageLocation = "~/App_Data/";

        /// <summary>
        /// The application content root path (physical path to the app root).
        /// Set at startup from IWebHostEnvironment.ContentRootPath.
        /// </summary>
        public static string ContentRootPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// The IConfiguration instance for reading app settings.
        /// Set at startup via AddBlogEngineCore().
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Resolves a virtual path (e.g. ~/App_Data/foo) to a physical path.
        /// Replaces BlogConfig.MapPath and BlogConfig.MapPath.
        /// </summary>
        public static string MapPath(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
                return ContentRootPath;

            // already a physical path under the content root
            if (virtualPath.StartsWith(ContentRootPath, StringComparison.Ordinal))
                return virtualPath;

            var relative = virtualPath.Replace("~/", string.Empty).Replace("~", string.Empty);

            // app-absolute paths ("/Custom/Themes", Utils.ApplicationRelativeWebRoot + "...") are relative to the app root too
            var appRoot = Utils.ApplicationRelativeWebRoot;
            if (!string.IsNullOrEmpty(appRoot) && relative.StartsWith(appRoot, StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring(appRoot.Length);
            relative = relative.TrimStart('/', '\\');

            relative = relative.Replace('/', System.IO.Path.DirectorySeparatorChar);
            return System.IO.Path.Combine(ContentRootPath, relative);
        }

        public static string GetAppSetting(string key, string defaultValue = null)
        {
            if (Configuration != null)
            {
                // legacy web.config keys "BlogEngine.X" live in appsettings.json as "BlogEngine": { "X": ... }
                const string legacyPrefix = "BlogEngine.";
                var name = key.StartsWith(legacyPrefix, StringComparison.Ordinal) ? key.Substring(legacyPrefix.Length) : key;
                var value = Configuration[$"BlogEngine:{name}"] ?? Configuration[key];
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return defaultValue;
        }

        #region FileExtension

        private static string _fileExtension;

        /// <summary>
        ///     The  file extension used for aspx pages
        /// </summary>
        public static string FileExtension
        {
            get
            {
                if (BlogSettings.Instance.RemoveExtensionsFromUrls)
                    return "";

                return _fileExtension ?? (_fileExtension = GetAppSetting("BlogEngine.FileExtension") ?? ".aspx");
            }
        }

        #endregion

        #region VirtualPath
        
        private static string _virtualPath;

        /// <summary>
        /// The virtual path of the BE installation.
        /// </summary>
        public static string VirtualPath
        {
            get
            {
                return _virtualPath?? (_virtualPath = GetAppSetting("BlogEngine.VirtualPath") ?? "~/");
            }
        }
        #endregion

        #region MobileServices

        private static string _mobileServices;

        /// <summary>
        /// The regex used to identify mobile devices so a different theme can be shown
        /// </summary>
        public static string MobileServices
        {
            get
            {
                return _mobileServices ?? (_mobileServices = GetAppSetting("BlogEngine.MobileDevices") ??
                    @"(iemobile|iphone|ipod|android|nokia|sonyericsson|blackberry|samsung|sec\-|windows ce|motorola|mot\-|up.b|midp\-)");
            }
        }

        #endregion

        #region ReservedBlogNames

        private static string _reservedBlogNames;

        /// <summary>
        /// Prevent creating new blog that will conflict with existing folder or path
        /// </summary>
        public static string ReservedBlogNames
        {
            get
            {
                return _reservedBlogNames ?? (_reservedBlogNames = GetAppSetting("BlogEngine.ReservedBlogNames") ?? @"(account|admin|api|app_code|app_data|app_globalresources|bin|content|editors|modules|pics|scripts|setup|templates|tests|themes|user controls|widgets|post|page|category|author|tag|calendar)");
            }
        }

        #endregion

        #region StorageLocation

        private static string _storageLocation;

        /// <summary>
        /// Storage location on web server
        /// </summary>
        /// <returns>
        /// string with virtual path to storage
        /// </returns>
        public static string StorageLocation
        {
            get
            {
                return _storageLocation?? (
                _storageLocation = string.IsNullOrEmpty(GetAppSetting("StorageLocation"))
                           ? DefaultStorageLocation
                           : GetAppSetting("StorageLocation"));
            }
        }

        #endregion

        #region BlogInstancesFolderName

        private static string _blogInstancesFolderName;

        /// <summary>
        /// Gets name of the folder blog instances are stored in.
        /// </summary>
        public static string BlogInstancesFolderName
        {
            get
            {
                return _blogInstancesFolderName ?? (_blogInstancesFolderName = GetAppSetting("BlogInstancesFolderName") ?? "blogs");
            }
        }

        #endregion

        #region AdministratorRole

        private static string _administrativeRole;

        /// <summary>
        ///     The role that has administrator persmissions
        /// </summary>
        public static string AdministratorRole
        {
            get
            {
                return _administrativeRole ?? (_administrativeRole = GetAppSetting("BlogEngine.AdminRole") ?? "administrators");
            }
        }
        #endregion

        #region AnonymousRole

        private static string _anonymousRole;

        /// <summary>
        /// The role that represents all non-authenticated users.
        /// </summary>
        public static string AnonymousRole
        {
            get
            {
                return _anonymousRole ?? (_anonymousRole = GetAppSetting("BlogEngine.AnonymousRole") ?? "Anonymous");
            }
        }

        #endregion

        #region EditorsRole

        private static string _editorsRole;

        /// <summary>
        /// The role that represents all non-authenticated users.
        /// </summary>
        public static string EditorsRole
        {
            get
            {
                return _editorsRole ?? (_editorsRole = GetAppSetting("BlogEngine.EditorsRole") ?? "Editors");
            }
        }

        #endregion

        #region IsSystemRole

        /// <summary>
        /// Returns whether a role is a System role.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>true if the roleName is a system role, otherwiser false</returns>
        public static bool IsSystemRole(string roleName)
        {
            if (roleName.Equals(AdministratorRole, StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals(AnonymousRole, StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals(EditorsRole, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region GenericPageSize

        private static int? _genericPageSize;

        /// <summary>
        /// Default number of items per page in admin data grids.
        /// </summary>
        public static int GenericPageSize
        {
            get
            {
                var tmp = GetAppSetting("BlogEngine.GenericPageSize");
                if (!string.IsNullOrEmpty(tmp))
                    _genericPageSize = int.Parse(tmp);

                return _genericPageSize ?? 15;
            }
        }

        #endregion

        #region SingleSignOn

        private static bool? _singleSignOn;
        /// <summary>
        /// Default number of items per page in admin data grids.
        /// </summary>
        public static bool SingleSignOn
        {
            get
            {
                if (_singleSignOn == null)
                {
                    string setting = GetAppSetting("BlogEngine.SingleSignOn") ?? "false";

                    if (!string.IsNullOrEmpty(setting))
                    {
                        bool value;
                        if (bool.TryParse(setting, out value))
                        {
                            _singleSignOn = value;
                        }
                    }
                    _singleSignOn = false;
                }
                return (bool)_singleSignOn;
            }
        }

        #endregion

        #region SiteMapUrlSet

        private static string _siteMapUrlSet;

        /// <summary>
        /// Used to set schema path to Google sitemap declaration.
        /// </summary>
        public static string SiteMapUrlSet
        {
            get
            {
                return _siteMapUrlSet ?? (_siteMapUrlSet = GetAppSetting("BlogEngine.SiteMapUrlSet") 
                    ?? "http://www.sitemaps.org/schemas/sitemap/0.9");
            }
        }

        #endregion

        #region DefaultEditor

        /// <summary>
        /// Default admin editor.
        /// </summary>
        public static string DefaultEditor
        {
            get
            {
                return string.IsNullOrEmpty(GetAppSetting("BlogEngine.DefaultEditor"))
                    ? "~/admin/editors/tinymce/editor.cshtml"
                    : GetAppSetting("BlogEngine.DefaultEditor");
            }
        }
        #endregion

        #region GalleryFeedUrl

        /// <summary>
        /// Online gallery feed endpoint
        /// </summary>
        public static string GalleryFeedUrl
        {
            get
            {
                return string.IsNullOrEmpty(GetAppSetting("BlogEngine.GalleryFeedUrl"))
                    ? "http://dnbe.net/v01/nuget"
                    : GetAppSetting("BlogEngine.GalleryFeedUrl");
            }
        }
        #endregion

        #region UsageScenario

        /// <summary>
        /// Configure usage scenario
        /// </summary>
        public static string UsageScenario
        {
            get
            {
                return string.IsNullOrEmpty(GetAppSetting("BlogEngine.UsageScenario"))
                    ? "singleblog"
                    : GetAppSetting("BlogEngine.UsageScenario");
            }
        }
        #endregion
    }
}
