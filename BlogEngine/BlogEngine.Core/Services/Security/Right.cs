using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BlogEngine.Core
{
    /// <summary>
    /// A wrapper class for Rights enum values that allows for providing more information.
    /// </summary>
    /// <remarks>
    /// This class needs to be kept in sync with Role creation/editing/deleting.
    /// </remarks>
    public sealed class Right
    {

        #region "Static"

        #region "Fields"

        private static readonly object staticLockObj = new Object();

        private static readonly ReadOnlyCollection<Rights> rightFlagValues;
        private static readonly ReadOnlyCollection<Right> allRightInstances;

        // This is a static collection so that there's no need to constantly remake a new empty collection
        // when a user has no rights.
        private static readonly ReadOnlyCollection<Right> noRights = new ReadOnlyCollection<Right>(new List<Right>());

        // Once rightsByFlag is set it should not be changed ever.
        private static readonly Dictionary<Rights, Right> rightsByFlag = new Dictionary<Rights, Right>();
        private static readonly Dictionary<string, Right> rightsByName = new Dictionary<string, Right>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Guid, Dictionary<string, HashSet<Right>>> rightsByRole = new Dictionary<Guid, Dictionary<string, HashSet<Right>>>();

        /// <summary>
        /// Static reference to the role service, set during application startup.
        /// </summary>
        private static Services.Security.IRoleService _roleService;

        /// <summary>
        /// Static reference to the membership service, set during application startup.
        /// </summary>
        private static Services.Security.IMembershipService _membershipService;

        #endregion

        /// <summary>
        /// Initializes the Right class with the role and membership services.
        /// Must be called during application startup.
        /// </summary>
        public static void Initialize(Services.Security.IRoleService roleService, Services.Security.IMembershipService membershipService)
        {
            _roleService = roleService;
            _membershipService = membershipService;
            EnsureBlogInstanceDataLoaded();
        }

        /// <summary>
        /// Ensures blog instance data is loaded. Should be called at the beginning of each request.
        /// Replaces the IHttpModule.Init/BeginRequest pattern.
        /// </summary>
        public static void EnsureInitialized()
        {
            EnsureBlogInstanceDataLoaded();
        }

        static Right()
        {
            // Initialize the various dictionaries to their starting state.

            var flagType = typeof(Rights);
            rightFlagValues = Enum.GetValues(flagType).Cast<Rights>().ToList().AsReadOnly();

            var adminRole = BlogEngine.Core.BlogConfig.AdministratorRole;

            var allRights = new List<Right>();

            // Create a Right instance for each value in the Rights enum.
            foreach (var flag in rightFlagValues)
            {
                Rights curFlag = (Rights)flag;
                var flagName = Enum.GetName(flagType, curFlag);
                var curRight = new Right(curFlag, flagName);

                allRights.Add(curRight);

                // Use the Add function so if there are multiple flags with the same
                // value they can be caught quickly at runtime.
                rightsByFlag.Add(curFlag, curRight);

                rightsByName.Add(flagName, curRight);
            }

            allRightInstances = allRights.AsReadOnly();

            Blog.Saved += (s, e) =>
            {
                if (e.Action == SaveAction.Delete)
                {
                    Blog blog = s as Blog;
                    if (blog != null)
                    {
                        // remove deleted blog from static 'rightsByRole'

                        if (rightsByRole != null && rightsByRole.ContainsKey(blog.Id))
                            rightsByRole.Remove(blog.Id);

                        // remove deleted blog from _readOnlyRoles/_rolesWithRight from
                        // each of the Right instances.
                        for (int i = 0; i < allRightInstances.Count; i++)
                        {
                            if (allRightInstances[i]._readOnlyRoles.ContainsKey(blog.Id))
                                allRightInstances[i]._readOnlyRoles.Remove(blog.Id);

                            if (allRightInstances[i]._rolesWithRight.ContainsKey(blog.Id))
                                allRightInstances[i]._rolesWithRight.Remove(blog.Id);
                        }
                    }
                }
            };
        }

        #region "Methods"

        /// <summary>
        /// Method that should be called any time Rights are changed and saved.
        /// </summary>
        public static void RefreshAllRights()
        {
            var flagType = typeof(Rights);

            lock (staticLockObj)
            {
                RightsByRole.Clear();

                string[] allRolesArr;
                if (_roleService != null)
                {
                    allRolesArr = _roleService.GetAllRoles();
                }
                else
                {
                    allRolesArr = Array.Empty<string>();
                }

                var allRoles = new HashSet<string>(allRolesArr, StringComparer.OrdinalIgnoreCase);

                foreach (var role in allRoles)
                {
                    var curRole = PrepareRoleName(role);
                    RightsByRole.Add(curRole, new HashSet<Right>());
                    allRoles.Add(curRole);
                }

                var adminRole = BlogConfig.AdministratorRole;
                var anonymousRole = BlogConfig.AnonymousRole;
                var editorsRole = BlogConfig.EditorsRole;

                foreach (var right in GetAllRights())
                {
                    // Clear the existing roles so any newly-deleted
                    // roles are removed from the list.
                    right.ClearRoles();
                    if (right.Flag != Rights.None)
                    {
                        right.AddRole(adminRole);
                    }
                }

                foreach (var pair in BlogEngine.Core.Providers.BlogService.FillRights())
                {
                    // Ignore any values that are invalid.
                    if (Right.RightExists(pair.Key))
                    {
                        var key = GetRightByName(pair.Key);

                        foreach (var role in pair.Value)
                        {
                            var curRole = PrepareRoleName(role);

                            // Ignore any roles that are added that don't exist.
                            if (allRoles.Contains(curRole))
                            {
                                key.AddRole(curRole);
                                Right.RightsByRole[curRole].Add(key);
                            }
                        }
                    }
                }

                bool defaultsAdded = false;

                // Check that the anonymous role is set up properly.
                if (!GetRights(anonymousRole).Any())
                {
                    List<Rights> defaultRoleRights = GetDefaultRights(anonymousRole);
                    foreach (Rights rights in defaultRoleRights)
                    {
                        Right.rightsByFlag[rights].AddRole(anonymousRole);
                    }

                    defaultsAdded = true;
                }

                // Check that the editor role is set up properly.
                if (!GetRights(editorsRole).Any())
                {
                    List<Rights> defaultRoleRights = GetDefaultRights(editorsRole);
                    foreach (Rights rights in defaultRoleRights)
                    {
                        Right.rightsByFlag[rights].AddRole(editorsRole);
                    }

                    defaultsAdded = true;
                }

                // This check is for autocreating the rights for the Administrator role.
                foreach (KeyValuePair<Rights, Right> kvp in rightsByFlag)
                {
                    if (kvp.Key != Rights.None)
                    {
                        kvp.Value.AddRole(adminRole);
                    }
                }

                if (defaultsAdded)
                {
                    BlogEngine.Core.Providers.BlogService.SaveRights();
                }
            }
        }

        /// <summary>
        /// Gets the list of default rights for the given role name.
        /// </summary>
        public static List<Rights> GetDefaultRights(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) { return new List<Rights>(); }

            if (roleName.Equals(BlogConfig.EditorsRole, StringComparison.OrdinalIgnoreCase))
            {
                return new List<Rights>()
                {
                    Rights.AccessAdminPages,
                    Rights.CreateComments,
                    Rights.ViewPublicComments,
                    Rights.ViewPublicPosts,
                    Rights.ViewPublicPages,
                    Rights.ViewRatingsOnPosts,
                    Rights.SubmitRatingsOnPosts,
                    Rights.ViewUnmoderatedComments,
                    Rights.ModerateComments,
                    Rights.ViewUnpublishedPages,
                    Rights.ViewUnpublishedPosts,
                    Rights.DeleteOwnPosts,
                    Rights.PublishOwnPosts,
                    Rights.CreateNewPages,
                    Rights.CreateNewPosts,
                    Rights.EditOwnPages,
                    Rights.EditOwnPosts,
                    Rights.EditOwnUser,
                    Rights.ViewDashboard,
                    Rights.ManagePackages
                };
            }
            else if (roleName.Equals(BlogConfig.AnonymousRole, StringComparison.OrdinalIgnoreCase))
            {
                return new List<Rights>()
                {
                    Rights.CreateComments,
                    Rights.ViewPublicComments,
                    Rights.ViewPublicPosts,
                    Rights.ViewPublicPages,
                    Rights.ViewRatingsOnPosts,
                    Rights.SubmitRatingsOnPosts
                };
            }

            return new List<Rights>();
        }

        /// <summary>
        /// Handles updating Role name changes.
        /// </summary>
        public static void OnRenamingRole(string oldname, string newname)
        {
            IEnumerable<Right> rightsWithRole = Right.GetRights(oldname);
            if (rightsWithRole.Any())
            {
                foreach (Right right in rightsWithRole)
                {
                    right.RemoveRole(oldname);
                    right.AddRole(newname);
                }

                BlogEngine.Core.Providers.BlogService.SaveRights();
            }
        }

        /// <summary>
        /// Handles removing Roles tied to Rights when a Role will be deleted.
        /// </summary>
        public static void OnRoleDeleting(string roleName)
        {
            IEnumerable<Right> rightsWithRole = Right.GetRights(roleName);
            if (rightsWithRole.Any())
            {
                foreach (Right right in rightsWithRole)
                {
                    right.RemoveRole(roleName);
                }

                BlogEngine.Core.Providers.BlogService.SaveRights();
            }
        }

        /// <summary>
        /// Call this method for verifying role names and then trimming the string.
        /// </summary>
        private static string PrepareRoleName(string roleName)
        {
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }
            else
            {
                return roleName.Trim();
            }
        }

        /// <summary>
        /// Returns an IEnumerable of all of the Rights that exist on BlogEngine.
        /// </summary>
        public static IEnumerable<Right> GetAllRights()
        {
            return Right.allRightInstances;
        }

        /// <summary>
        /// Returns a Right instance based on its name.
        /// </summary>
        public static Right GetRightByName(string rightName)
        {
            if (String.IsNullOrWhiteSpace(rightName))
            {
                throw new ArgumentNullException(nameof(rightName));
            }
            else
            {
                Right right = null;
                if (rightsByName.TryGetValue(rightName.Trim(), out right))
                {
                    return right;
                }
                else
                {
                    throw new KeyNotFoundException("No Right exists by the name '" + rightName + "'");
                }
            }
        }

        /// <summary>
        /// Returns a Right instance based on the flag.
        /// </summary>
        public static Right GetRightByFlag(Rights flag)
        {
            Right right = null;
            if (rightsByFlag.TryGetValue(flag, out right))
            {
                return right;
            }
            else
            {
                throw new KeyNotFoundException("Unable to find a corresponding right for the given flag");
            }
        }

        private static IEnumerable<Right> GetRightsInternal(string roleName)
        {
            roleName = PrepareRoleName(roleName);
            if (RightsByRole.ContainsKey(roleName))
                return RightsByRole[roleName];
            else
                return new HashSet<Right>();
        }

        /// <summary>
        /// Returns an IEnumerable of Rights that are in the given role.
        /// </summary>
        public static IEnumerable<Right> GetRights(string roleName)
        {
            return GetRightsInternal(roleName).ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns an IEnumerable of Rights that are in all of the given roles.
        /// </summary>
        public static IEnumerable<Right> GetRights(IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }
            else if (!roles.Any())
            {
                return noRights;
            }
            else
            {
                var rights = new List<Right>();

                foreach (var role in roles)
                {
                    rights.AddRange(GetRightsInternal(role));
                }

                return rights.Distinct().ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets whether or not a Right exists within any of the given roles.
        /// </summary>
        public static bool HasRight(Rights right, IEnumerable<string> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }
            else if (!roles.Any())
            {
                return false;
            }
            else
            {
                var validRoles = GetRightByFlag(right).Roles;
                if (roles.Count() == 1)
                {
                    return validRoles.Contains(roles.First(), StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    return validRoles.Intersect(roles, StringComparer.OrdinalIgnoreCase).Any();
                }
            }
        }

        /// <summary>
        /// Checks to see if a Right exists by the given name.
        /// </summary>
        public static bool RightExists(string rightName)
        {
            return rightsByName.ContainsKey(rightName);
        }

        #endregion

        #endregion

        #region "Instance"

        #region "Fields and Constants"

        private readonly object instanceLockObj = new Object();

        private readonly Dictionary<Guid, ReadOnlyCollection<string>> _readOnlyRoles;
        private readonly Dictionary<Guid, List<string>> _rolesWithRight;

        #endregion

        #region "Constructor"

        /// <summary>
        /// Private constructor for creating a Right instance.
        /// </summary>
        private Right(Rights Right, string RightEnumName)
        {
            _flag = Right;
            _name = RightEnumName;
            _rolesWithRight = new Dictionary<Guid, List<string>>();
            _readOnlyRoles = new Dictionary<Guid, ReadOnlyCollection<string>>();
        }

        #endregion

        #region "Properties"

        private static void EnsureBlogInstanceDataLoaded()
        {
            Blog blog = Blog.CurrentInstance;

            // either all the right instances will be setup for the current blog instance, or none
            // of them will be. check just the first one to see if it is setup for the current
            // blog instance.

            if (!allRightInstances[0]._readOnlyRoles.ContainsKey(blog.Id))
            {
                for (int i = 0; i < allRightInstances.Count; i++)
                {
                    allRightInstances[i]._rolesWithRight[blog.Id] = new List<string>();
                    allRightInstances[i]._readOnlyRoles[blog.Id] = new ReadOnlyCollection<string>(allRightInstances[i]._rolesWithRight[blog.Id]);
                }
            }

            if (!rightsByRole.ContainsKey(blog.Id))
            {
                // touch RightsByRole to make sure data for current blog instance is loaded
                var rr = RightsByRole;
            }
        }


        private List<string> RolesWithRight
        {
            get
            {
                return _rolesWithRight[Blog.CurrentInstance.Id];
            }
        }

        private ReadOnlyCollection<string> ReadOnlyRoles
        {
            get
            {
                return _readOnlyRoles[Blog.CurrentInstance.Id];
            }
        }

        private static Dictionary<string, HashSet<Right>> RightsByRole
        {
            get
            {
                Blog blog = Blog.CurrentInstance;

                if (!rightsByRole.ContainsKey(blog.Id))
                {
                    lock (staticLockObj)
                    {
                        if (!rightsByRole.ContainsKey(blog.Id))
                        {
                            rightsByRole[blog.Id] = new Dictionary<string, HashSet<Right>>(StringComparer.OrdinalIgnoreCase);
                            InitRightForBlogInstance();
                        }
                    }
                }

                return rightsByRole[blog.Id];
            }
        }

        private static void InitRightForBlogInstance()
        {
            if (_roleService != null)
            {
                // Make sure the Administrator role exists.
                if (!_roleService.RoleExists(BlogConfig.AdministratorRole))
                {
                    _roleService.CreateRole(BlogConfig.AdministratorRole);

                    // if no one is in the admin role, and there is a user named "admin", add that user
                    if (_roleService.GetUsersInRole(BlogConfig.AdministratorRole).Length == 0)
                    {
                        if (_membershipService != null)
                        {
                            var membershipUser = _membershipService.GetUser("Admin");
                            if (membershipUser != null)
                            {
                                _roleService.AddUsersToRoles(new string[] { membershipUser.UserName }, new string[] { BlogConfig.AdministratorRole });
                            }
                        }
                    }
                }

                // Make sure the Anonymous role exists.
                if (!_roleService.RoleExists(BlogConfig.AnonymousRole))
                {
                    _roleService.CreateRole(BlogConfig.AnonymousRole);
                }

                // Make sure the Editors role exists.
                if (!_roleService.RoleExists(BlogConfig.EditorsRole))
                {
                    _roleService.CreateRole(BlogConfig.EditorsRole);
                }
            }

            RefreshAllRights();
        }

        /// <summary>
        /// Returns a display-friendly version of this Right's name.
        /// </summary>
        public string DisplayName
        {
            get { return Utils.FormatIdentifierForDisplay(Name); }
        }

        /// <summary>
        /// Returns the empty string.
        /// </summary>
        public string Description
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the Right value for this Right instance.
        /// </summary>
        public Rights Flag
        {
            get
            {
                return _flag;
            }
        }
        private readonly Rights _flag;

        /// <summary>
        /// Gets the name of this right.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        private readonly string _name;

        /// <summary>
        /// Gets the Roles that currently have this Right.
        /// </summary>
        public IEnumerable<string> Roles
        {
            get { return ReadOnlyRoles; }
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Adds a role to the list of roles that have this Right.
        /// </summary>
        public bool AddRole(string roleName)
        {
            roleName = PrepareRoleName(roleName);

            lock (instanceLockObj)
            {
                if (!Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    RolesWithRight.Add(roleName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes a Role from the collection of roles that allow this Right.
        /// </summary>
        public bool RemoveRole(string roleName)
        {
            roleName = PrepareRoleName(roleName);

            if (roleName.Equals(BlogConfig.AdministratorRole, StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Security.SecurityException("Rights can not be removed from the administrative role");
            }
            else
            {
                lock (instanceLockObj)
                {
                    return RolesWithRight.Remove(roleName);
                }
            }
        }

        /// <summary>
        /// Clears all the roles in the roles list.
        /// </summary>
        private void ClearRoles()
        {
            lock (instanceLockObj)
            {
                RolesWithRight.Clear();
            }
        }

        #endregion

        #endregion
    }
}
