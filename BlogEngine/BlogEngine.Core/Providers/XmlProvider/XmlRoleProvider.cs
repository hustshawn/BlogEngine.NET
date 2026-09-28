namespace BlogEngine.Core.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// XML-based role service implementation.
    /// Replaces the legacy XmlRoleProvider (RoleProvider).
    /// </summary>
    public class XmlRoleService : IRoleService
    {
        #region Constants and Fields

        /// <summary>
        /// The default roles to add.
        /// </summary>
        private readonly string[] defaultRolesToAdd = new[] { BlogConfig.AdministratorRole, BlogConfig.AnonymousRole };

        /// <summary>
        /// The roles.
        /// </summary>
        private readonly Dictionary<Guid, List<Role>> roles = new Dictionary<Guid, List<Role>>();

        /// <summary>
        /// The user names.
        /// </summary>
        private List<string> userNames;

        /// <summary>
        /// The xml file name.
        /// </summary>
        private string xmlFileName;

        /// <summary>
        /// Reference to membership service for initial user loading.
        /// </summary>
        private readonly IMembershipService _membershipService;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new XmlRoleService.
        /// </summary>
        /// <param name="membershipService">The membership service for loading initial users.</param>
        /// <param name="xmlFileName">The XML file name (e.g. "roles.xml").</param>
        public XmlRoleService(IMembershipService membershipService, string xmlFileName = "roles.xml")
        {
            this._membershipService = membershipService;
            this.xmlFileName = xmlFileName;

            // Load initial user names from membership service
            this.ReadMembershipDataStore();

            // Initialize roles data store
            if (!File.Exists(this.XmlFullyQualifiedPath))
            {
                this.AddUsersToRoles(this.userNames.ToArray(), this.defaultRolesToAdd);
            }

            this.ReadRoleDataStore();
        }

        #endregion

        #region Properties

        private string XmlFullyQualifiedPath
        {
            get
            {
                string location = Blog.CurrentInstance.StorageLocation;
                string fullyQualifiedPath = BlogConfig.MapPath(location + xmlFileName);
                return fullyQualifiedPath;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified user names to the specified roles.
        /// </summary>
        public void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            ReadRoleDataStore();

            var currentRoles = new List<string>(this.GetAllRoles());
            if (usernames.Length != 0 && roleNames.Length != 0)
            {
                foreach (var rolename in roleNames.Where(rolename => !currentRoles.Contains(rolename, StringComparer.OrdinalIgnoreCase) && !rolename.Equals(BlogConfig.AnonymousRole, StringComparison.OrdinalIgnoreCase)))
                {
                    this.roles[Blog.CurrentInstance.Id].Add(new Role(rolename, new List<string>(usernames)));
                }

                foreach (var role in this.roles[Blog.CurrentInstance.Id])
                {
                    var role1 = role;
                    foreach (var s in from name in roleNames
                                      where role1.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                      from s in usernames
                                      where !role1.Users.Contains(s, StringComparer.OrdinalIgnoreCase)
                                      select s)
                    {
                        role.Users.Add(s);
                    }
                }
            }

            this.Save();
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public void CreateRole(string roleName)
        {
            ReadRoleDataStore();

            if (this.roles[Blog.CurrentInstance.Id].Contains(new Role(roleName)))
            {
                return;
            }

            this.roles[Blog.CurrentInstance.Id].Add(new Role(roleName));
            this.Save();
        }

        /// <summary>
        /// Removes a role.
        /// </summary>
        public bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            if (Security.IsSystemRole(roleName))
                return false;

            ReadRoleDataStore();

            var blogRoles = roles[Blog.CurrentInstance.Id];

            if (blogRoles == null || blogRoles.Count == 0)
                return false;

            for (var i = 0; i < blogRoles.Count; i++)
            {
                if (blogRoles[i].Name == roleName)
                {
                    roles[Blog.CurrentInstance.Id].RemoveAt(i);
                    Save();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets an array of user names in a role where the user name contains the specified user name to match.
        /// </summary>
        public string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            var usersInRole = new List<string>();
            if (this.IsUserInRole(usernameToMatch, roleName))
            {
                usersInRole.AddRange(this.userNames);
            }

            return usersInRole.ToArray();
        }

        /// <summary>
        /// Gets a list of all the roles.
        /// </summary>
        public string[] GetAllRoles()
        {
            ReadRoleDataStore();
            return this.roles[Blog.CurrentInstance.Id].Select(role => role.Name).ToArray();
        }

        /// <summary>
        /// Gets a list of the roles that a specified user is in.
        /// </summary>
        public string[] GetRolesForUser(string username)
        {
            ReadRoleDataStore();
            return (from role in this.roles[Blog.CurrentInstance.Id]
                    from user in role.Users
                    where user.Equals(username, StringComparison.OrdinalIgnoreCase)
                    select role.Name).ToArray();
        }

        /// <summary>
        /// Gets a list of users in the specified role.
        /// </summary>
        public string[] GetUsersInRole(string roleName)
        {
            ReadRoleDataStore();
            return (from role in this.roles[Blog.CurrentInstance.Id]
                    where role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)
                    from user in role.Users
                    select user.ToLowerInvariant()).ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role.
        /// </summary>
        public bool IsUserInRole(string username, string roleName)
        {
            ReadRoleDataStore();
            return
                this.roles[Blog.CurrentInstance.Id].Where(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).SelectMany(
                    role => role.Users).Any(user => user.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes the specified user names from the specified roles.
        /// </summary>
        public void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            ReadRoleDataStore();

            if (usernames.Length != 0 && roleNames.Length != 0)
            {
                foreach (var role in this.roles[Blog.CurrentInstance.Id])
                {
                    var role1 = role;
                    if (!roleNames.Any(name => role1.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    foreach (var user in usernames)
                    {
                        if (role.Name.Equals(
                            BlogConfig.AdministratorRole, StringComparison.OrdinalIgnoreCase))
                        {
                            if (role.Users.Count != 1)
                            {
                                RemoveItemFromList(role.Users, user);
                            }
                        }
                        else
                        {
                            RemoveItemFromList(role.Users, user);
                        }
                    }
                }
            }

            this.Save();
        }

        /// <summary>
        /// Gets a value indicating whether the specified role name already exists.
        /// </summary>
        public bool RoleExists(string roleName)
        {
            if (String.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentNullException(nameof(roleName));
            }
            else
            {
                return (this.GetAllRoles().Contains(roleName, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Saves roles to XML file.
        /// </summary>
        public void Save()
        {
            ReadRoleDataStore();
            var settings = new XmlWriterSettings { Indent = true };

            using (var writer = XmlWriter.Create(this.XmlFullyQualifiedPath, settings))
            {
                writer.WriteStartDocument(true);
                writer.WriteStartElement("roles");

                foreach (var role in this.roles[Blog.CurrentInstance.Id])
                {
                    writer.WriteStartElement("role");
                    writer.WriteElementString("name", role.Name);
                    writer.WriteStartElement("users");
                    foreach (var username in role.Users)
                    {
                        writer.WriteElementString("user", username);
                    }

                    writer.WriteEndElement(); // closes users
                    writer.WriteEndElement(); // closes role
                }
            }

            // This needs to be called in order to keep the Right class in sync.
            Right.RefreshAllRights();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The remove item from list.
        /// </summary>
        private static void RemoveItemFromList(ICollection<string> list, string item)
        {
            if (list == null || string.IsNullOrEmpty(item) || list.Count == 0)
            {
                return;
            }

            var usersToRemove = list.Where(u => u.Equals(item, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var u in usersToRemove)
            {
                list.Remove(u);
            }
        }

        /// <summary>
        /// Only so we can add users to the administrators role.
        /// </summary>
        private void ReadMembershipDataStore()
        {
            this.userNames = new List<string>();

            int totalRecords;
            var allUsers = _membershipService.GetAllUsers(out totalRecords);
            foreach (var user in allUsers)
            {
                this.userNames.Add(user.UserName);
            }
        }

        /// <summary>
        /// Builds the internal cache of roles.
        /// </summary>
        private void ReadRoleDataStore()
        {
            lock (this)
            {
                if (!roles.ContainsKey(Blog.CurrentInstance.Id))
                {
                    roles[Blog.CurrentInstance.Id] = new List<Role>();

                    var doc = new XmlDocument();

                    try
                    {
                        doc.Load(this.XmlFullyQualifiedPath);
                        var nodes = doc.GetElementsByTagName("role");
                        foreach (XmlNode roleNode in nodes)
                        {
                            var name = roleNode.SelectSingleNode("name");
                            if (name == null)
                            {
                                continue;
                            }

                            var tempRole = new Role(name.InnerText);
                            var user = roleNode.SelectNodes("users/user");
                            if (user != null)
                            {
                                foreach (XmlNode userNode in user)
                                {
                                    tempRole.Users.Add(userNode.InnerText);
                                }
                            }

                            this.roles[Blog.CurrentInstance.Id].Add(tempRole);
                        }
                    }
                    catch (XmlException)
                    {
                        this.AddUsersToRoles(this.userNames.ToArray(), this.defaultRolesToAdd);
                    }
                }
            }
        }

        #endregion
    }
}
