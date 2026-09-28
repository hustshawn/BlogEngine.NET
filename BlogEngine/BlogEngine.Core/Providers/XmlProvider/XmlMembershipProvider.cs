namespace BlogEngine.Core.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.IO;
    using System.Xml;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// XML-based membership service implementation.
    /// Replaces the legacy XmlMembershipProvider (MembershipProvider).
    /// </summary>
    public class XmlMembershipService : IMembershipService
    {
        #region Constants and Fields

        /// <summary>
        /// The password format.
        /// </summary>
        private PasswordFormat passwordFormat;

        /// <summary>
        /// The users cache per blog instance.
        /// </summary>
        private Dictionary<Guid, Dictionary<string, BlogMembershipUser>> users;

        /// <summary>
        /// The xml file name.
        /// </summary>
        private string xmlFileName;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new XmlMembershipService with default settings.
        /// </summary>
        /// <param name="xmlFileName">The XML file name (e.g. "users.xml").</param>
        /// <param name="passwordFormat">Password format (Hashed or Clear).</param>
        public XmlMembershipService(string xmlFileName = "users.xml", PasswordFormat passwordFormat = PasswordFormat.Hashed)
        {
            this.xmlFileName = xmlFileName;
            this.passwordFormat = passwordFormat;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the password format.
        /// </summary>
        public PasswordFormat PasswordFormatSetting
        {
            get { return this.passwordFormat; }
        }

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
        /// Processes a request to update the password for a membership user.
        /// </summary>
        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);
            var nodes = doc.GetElementsByTagName("User");
            foreach (XmlNode node in nodes)
            {
                if (!node["UserName"].InnerText.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!this.CheckPassword(node["Password"].InnerText, oldPassword))
                {
                    continue;
                }

                string passwordPrep = this.passwordFormat == PasswordFormat.Hashed ? Utils.HashPassword(newPassword) : newPassword;

                node["Password"].InnerText = passwordPrep;
                doc.Save(XmlFullyQualifiedPath);

                this.users = null;
                this.ReadMembershipDataStore();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        public BlogMembershipUser CreateUser(string username, string password, string email)
        {
            this.ReadMembershipDataStore();

            if (this.users[Blog.CurrentInstance.Id].ContainsKey(username))
            {
                throw new InvalidOperationException("The username is already in use. Please choose another username.");
            }

            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);

            XmlNode xmlUserRoot = doc.CreateElement("User");
            XmlNode xmlUserName = doc.CreateElement("UserName");
            XmlNode xmlPassword = doc.CreateElement("Password");
            XmlNode xmlEmail = doc.CreateElement("Email");
            XmlNode xmlLastLoginTime = doc.CreateElement("LastLoginTime");

            xmlUserName.InnerText = username;

            string passwordPrep = this.passwordFormat == PasswordFormat.Hashed ? Utils.HashPassword(password) : password;

            xmlPassword.InnerText = passwordPrep;

            xmlEmail.InnerText = email;
            xmlLastLoginTime.InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            xmlUserRoot.AppendChild(xmlUserName);
            xmlUserRoot.AppendChild(xmlPassword);
            xmlUserRoot.AppendChild(xmlEmail);
            xmlUserRoot.AppendChild(xmlLastLoginTime);

            doc.SelectSingleNode("Users").AppendChild(xmlUserRoot);
            doc.Save(XmlFullyQualifiedPath);

            var user = new BlogMembershipUser(
                "XmlMembershipService",
                username,
                username,
                email,
                string.Empty,
                passwordPrep,
                true,
                false,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                DateTime.MaxValue);
            this.users[Blog.CurrentInstance.Id].Add(username, user);
            return user;
        }

        /// <summary>
        /// Removes a user from the membership data source.
        /// </summary>
        public bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            this.ReadMembershipDataStore();

            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);

            foreach (XmlNode node in
                doc.GetElementsByTagName("User").Cast<XmlNode>().Where(node => node.ChildNodes[0].InnerText.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                doc.SelectSingleNode("Users").RemoveChild(node);
                doc.Save(XmlFullyQualifiedPath);
                this.users[Blog.CurrentInstance.Id].Remove(username);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a collection of all the users.
        /// </summary>
        public ICollection<BlogMembershipUser> GetAllUsers(out int totalRecords)
        {
            this.ReadMembershipDataStore();
            var membershipUsers = new List<BlogMembershipUser>();

            if (Blog.CurrentInstance.IsSiteAggregation)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Blog b in Blog.Blogs)
                {
                    if (!b.IsDeleted && b.IsActive)
                    {
                        foreach (var pair in this.users[b.Id])
                        {
                            if (!seen.Contains(pair.Key))
                            {
                                seen.Add(pair.Key);
                                membershipUsers.Add(pair.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var pair in this.users[Blog.CurrentInstance.Id])
                {
                    membershipUsers.Add(pair.Value);
                }
            }

            totalRecords = membershipUsers.Count;
            return membershipUsers;
        }

        /// <summary>
        /// The get number of users online — not supported for XML provider.
        /// </summary>
        public int GetNumberOfUsersOnline()
        {
            return 0;
        }

        /// <summary>
        /// Retrieves a user based on username.
        /// </summary>
        public BlogMembershipUser GetUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            this.ReadMembershipDataStore();

            BlogMembershipUser user;
            return this.users[Blog.CurrentInstance.Id].TryGetValue(username, out user) ? user : null;
        }

        /// <summary>
        /// Get a user based on the provider user key.
        /// </summary>
        public BlogMembershipUser GetUser(object providerUserKey)
        {
            if (providerUserKey == null)
            {
                throw new ArgumentNullException(nameof(providerUserKey));
            }

            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);

            return (from XmlNode node in doc.SelectNodes("//User")
                    where node.ChildNodes[0].InnerText.Equals(providerUserKey.ToString(), StringComparison.OrdinalIgnoreCase)
                    let userName = node.ChildNodes[0].InnerText
                    let password = node.ChildNodes[1].InnerText
                    let email = node.ChildNodes[2].InnerText
                    let lastLoginTime = DateTime.Parse(node.ChildNodes[3].InnerText, CultureInfo.InvariantCulture)
                    select new BlogMembershipUser(
                        "XmlMembershipService",
                        providerUserKey.ToString(),
                        providerUserKey,
                        email,
                        string.Empty,
                        password,
                        true,
                        false,
                        DateTime.Now,
                        lastLoginTime,
                        DateTime.Now,
                        DateTime.Now,
                        DateTime.MaxValue)).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a username based on a matching email.
        /// </summary>
        public string GetUserNameByEmail(string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);

            return doc.GetElementsByTagName("User").Cast<XmlNode>().Where(
                node => node.ChildNodes[2].InnerText.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)).Select(
                    node => node.ChildNodes[0].InnerText).FirstOrDefault();
        }

        /// <summary>
        /// Resets a user's password to a new, automatically generated password.
        /// </summary>
        public string ResetPassword(string username)
        {
            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);
            var nodes = doc.GetElementsByTagName("User");
            var newPassword = Utils.RandomPassword();

            foreach (var node in
                nodes.Cast<XmlNode>().Where(
                    node =>
                    node["UserName"].InnerText.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(node["Password"].InnerText)))
            {
                string passwordPrep;
                if (this.passwordFormat == PasswordFormat.Hashed)
                {
                    passwordPrep = Utils.HashPassword(newPassword);
                }
                else
                {
                    passwordPrep = newPassword;
                }

                node["Password"].InnerText = passwordPrep;
                doc.Save(XmlFullyQualifiedPath);

                this.users = null;
                this.ReadMembershipDataStore();
                return newPassword;
            }

            return string.Empty;
        }

        /// <summary>
        /// Clears a lock — not supported for XML provider.
        /// </summary>
        public bool UnlockUser(string userName)
        {
            // XML provider does not support account locking
            return false;
        }

        /// <summary>
        /// Updates a user. The username will not be changed.
        /// </summary>
        public void UpdateUser(BlogMembershipUser user)
        {
            this.ReadMembershipDataStore();
            var doc = new XmlDocument();
            doc.Load(XmlFullyQualifiedPath);

            foreach (var node in
                doc.GetElementsByTagName("User").Cast<XmlNode>().Where(node => node.ChildNodes[0].InnerText.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                if (user.Comment.Length > 30)
                {
                    node.ChildNodes[1].InnerText = user.Comment;
                }

                node.ChildNodes[2].InnerText = user.Email;
                node.ChildNodes[3].InnerText = user.LastLoginDate.ToString(
                    "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                doc.Save(XmlFullyQualifiedPath);
                this.users[Blog.CurrentInstance.Id][user.UserName] = user;
            }
        }

        /// <summary>
        /// Returns true if the username and password match an existing user.
        /// </summary>
        public bool ValidateUser(string username, string password)
        {
            var validated = false;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                this.ReadMembershipDataStore();

                BlogMembershipUser user;
                if (this.users[Blog.CurrentInstance.Id].TryGetValue(username, out user))
                {
                    validated = this.CheckPassword(user.Comment, password);
                }

                return validated;
            }
            catch (Exception)
            {
                return validated;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The check password.
        /// </summary>
        private bool CheckPassword(string storedPassword, string inputPassword)
        {
            var validated = false;
            if (storedPassword == string.Empty)
            {
                // This is a special case used for resetting.
                if (string.Compare(inputPassword, "admin", true) == 0)
                {
                    validated = true;
                }
            }
            else
            {
                if (this.passwordFormat == PasswordFormat.Hashed)
                {
                    if (storedPassword == Utils.HashPassword(inputPassword))
                    {
                        validated = true;
                    }
                }
                else if (inputPassword == storedPassword)
                {
                    validated = true;
                }
            }

            return validated;
        }

        /// <summary>
        /// Builds the internal cache of users.
        /// </summary>
        private void ReadMembershipDataStore()
        {
            lock (this)
            {
                if (this.users == null)
                {
                    this.users = new Dictionary<Guid, Dictionary<string, BlogMembershipUser>>();
                }

                ReadFromFile(XmlFullyQualifiedPath, Blog.CurrentInstance.Id);

                if (Blog.CurrentInstance.IsSiteAggregation)
                {
                    foreach (Blog b in Blog.Blogs)
                    {
                        if (b.IsPrimary || b.IsDeleted || !b.IsActive)
                            continue;

                        string path = BlogConfig.MapPath(Path.Combine(Blog.CurrentInstance.StorageLocation, "blogs", b.Name, "users.xml"));

                        if (!File.Exists(path))
                        {
                            Utils.Log($"XmlMembershipService: can not read users from file \"{path}\"");
                        }

                        ReadFromFile(path, b.Id);
                    }
                }
            }
        }

        private void ReadFromFile(string fileName, Guid blogId)
        {
            if (!this.users.ContainsKey(blogId))
            {
                this.users[blogId] = new Dictionary<string, BlogMembershipUser>(16, StringComparer.OrdinalIgnoreCase);
                var doc = new XmlDocument();
                doc.Load(fileName);
                var nodes = doc.GetElementsByTagName("User");

                foreach (var user in
                    nodes.Cast<XmlNode>().Select(
                        node => new BlogMembershipUser(
                                    "XmlMembershipService",
                            // Provider name
                                    node["UserName"].InnerText,
                            // Username
                                    node["UserName"].InnerText,
                            // providerUserKey
                                    node["Email"].InnerText,
                            // Email
                                    string.Empty,
                            // passwordQuestion
                                    node["Password"].InnerText,
                            // Comment
                                    true,
                            // approved
                                    false,
                            // isLockedOut
                                    DateTime.Now,
                            // creationDate
                                    DateTime.Parse(node["LastLoginTime"].InnerText, CultureInfo.InvariantCulture),
                            // lastLoginDate
                                    DateTime.Now,
                            // lastActivityDate
                                    DateTime.Now,
                            // lastPasswordChangedDate
                                    new DateTime(1980, 1, 1))))
                {
                    this.users[blogId].Add(user.UserName, user);
                }
            }
        }

        #endregion
    }
}
