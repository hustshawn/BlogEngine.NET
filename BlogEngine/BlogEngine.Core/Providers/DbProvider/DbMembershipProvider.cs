namespace BlogEngine.Core.Providers
{
    using System;
    using BlogEngine.Core.Services.Security;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Generic Db Membership Service.
    /// Replaces the legacy DbMembershipProvider (MembershipProvider).
    /// </summary>
    public class DbMembershipService : IMembershipService
    {
        #region Constants and Fields

        /// <summary>
        /// The conn string name.
        /// </summary>
        private string connStringName;

        /// <summary>
        /// The parm prefix.
        /// </summary>
        private string parmPrefix;

        /// <summary>
        /// The password format.
        /// </summary>
        private PasswordFormat passwordFormat;

        /// <summary>
        /// The table prefix.
        /// </summary>
        private string tablePrefix;

        /// <summary>
        /// The provider name for the database connection.
        /// </summary>
        private string providerName;

        /// <summary>
        /// The connection string.
        /// </summary>
        private string connectionString;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new DbMembershipService with the specified configuration.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="connectionStringName">Name of the connection string. Defaults to "BlogEngine".</param>
        /// <param name="tablePrefix">Table prefix. Defaults to "be_".</param>
        /// <param name="parmPrefix">Parameter prefix. Defaults to "@".</param>
        /// <param name="passwordFormat">Password storage format. Defaults to Hashed.</param>
        public DbMembershipService(
            IConfiguration configuration,
            string connectionStringName = "BlogEngine",
            string tablePrefix = "be_",
            string parmPrefix = "@",
            PasswordFormat passwordFormat = PasswordFormat.Hashed)
        {
            this.connStringName = connectionStringName;
            this.tablePrefix = tablePrefix;
            this.parmPrefix = parmPrefix;
            this.passwordFormat = passwordFormat;

            this.connectionString = configuration.GetConnectionString(connectionStringName);
            this.providerName = configuration[$"ConnectionStrings:{connectionStringName}_ProviderName"]
                ?? "Microsoft.Data.SqlClient";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Password format (Clear or Hashed).
        /// </summary>
        public PasswordFormat PasswordFormatSetting
        {
            get { return this.passwordFormat; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Change the password if the old password matches what is stored.
        /// </summary>
        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var oldPasswordCorrect = false;
            var success = false;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT password FROM {0}Users WHERE BlogID = {1}blogid AND userName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                var actualPassword = rdr.GetString(0);
                                if (actualPassword == string.Empty)
                                {
                                    // This is a special case used for resetting.
                                    if (oldPassword.ToLower() == "admin")
                                    {
                                        oldPasswordCorrect = true;
                                    }
                                }
                                else
                                {
                                    if (this.passwordFormat == PasswordFormat.Hashed)
                                    {
                                        if (actualPassword == Utils.HashPassword(oldPassword))
                                        {
                                            oldPasswordCorrect = true;
                                        }
                                    }
                                    else if (actualPassword == oldPassword)
                                    {
                                        oldPasswordCorrect = true;
                                    }
                                }
                            }
                        }

                        // Update New Password
                        if (oldPasswordCorrect)
                        {
                            cmd.CommandText = string.Format("UPDATE {0}Users SET password = {1}pwd WHERE BlogID = {1}blogid AND userName = {1}name", this.tablePrefix, this.parmPrefix);

                            cmd.Parameters.Add(conn.CreateParameter(FormatParamName("pwd"), (this.passwordFormat == PasswordFormat.Hashed ? Utils.HashPassword(newPassword) : newPassword)));

                            cmd.ExecuteNonQuery();
                            success = true;
                        }
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Add new user to database.
        /// </summary>
        public BlogMembershipUser CreateUser(string username, string password, string email)
        {
            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    var sqlQuery = string.Format("INSERT INTO {0}Users (blogId, userName, password, emailAddress, lastLoginTime) VALUES ({1}blogid, {1}name, {1}pwd, {1}email, {1}login)", this.tablePrefix, this.parmPrefix);
                    using (var cmd = conn.CreateTextCommand(sqlQuery))
                    {
                        var parms = cmd.Parameters;
                        parms.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        parms.Add(conn.CreateParameter(FormatParamName("name"), username));
                        parms.Add(conn.CreateParameter(FormatParamName("pwd"), (this.passwordFormat == PasswordFormat.Hashed ? Utils.HashPassword(password) : password)));
                        parms.Add(conn.CreateParameter(FormatParamName("email"), email));
                        parms.Add(conn.CreateParameter(FormatParamName("login"), DateTime.Now));

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            BlogMembershipUser user = this.GetMembershipUser(username, email, DateTime.Now);
            return user;
        }

        /// <summary>
        /// Delete user from database.
        /// </summary>
        public bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            bool success = false;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("DELETE FROM {0}Users WHERE blogId = {1}blogid AND userName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        try
                        {
                            cmd.ExecuteNonQuery();
                            success = true;
                        }
                        catch (Exception)
                        {
                            success = false;
                            throw;
                        }
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Return all users.
        /// </summary>
        public System.Collections.Generic.ICollection<BlogMembershipUser> GetAllUsers(out int totalRecords)
        {
            var users = new System.Collections.Generic.List<BlogMembershipUser>();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    if (Blog.CurrentInstance.IsSiteAggregation)
                    {
                        using (var cmd = conn.CreateTextCommand($"SELECT username, EmailAddress, lastLoginTime FROM {tablePrefix}Users "))
                        {
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    users.Add(this.GetMembershipUser(rdr.GetString(0), rdr.GetString(1), rdr.GetDateTime(2)));
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = conn.CreateTextCommand($"SELECT username, EmailAddress, lastLoginTime FROM {tablePrefix}Users WHERE BlogID = {parmPrefix}blogid "))
                        {
                            cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));

                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    users.Add(this.GetMembershipUser(rdr.GetString(0), rdr.GetString(1), rdr.GetDateTime(2)));
                                }
                            }
                        }
                    }
                }
            }

            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        /// Not supported for DB provider — returns 0.
        /// </summary>
        public int GetNumberOfUsersOnline()
        {
            return 0;
        }

        /// <summary>
        /// Gets user information by provider user key.
        /// </summary>
        public BlogMembershipUser GetUser(object providerUserKey)
        {
            return this.GetUser(providerUserKey.ToString());
        }

        /// <summary>
        /// Gets information for a user by username.
        /// </summary>
        public BlogMembershipUser GetUser(string username)
        {
            BlogMembershipUser user = null;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT username, EmailAddress, lastLoginTime FROM {0}Users WHERE BlogID = {1}blogid AND UserName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                user = this.GetMembershipUser(username, rdr.GetString(1), rdr.GetDateTime(2));
                            }
                        }
                    }
                }
            }

            return user;
        }

        /// <summary>
        /// Gets the user name associated with the specified e-mail address.
        /// </summary>
        public string GetUserNameByEmail(string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            string userName = null;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT userName FROM {0}Users WHERE BlogID = {1}blogid AND emailAddress = {1}email", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("email"), email));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                userName = rdr.GetString(0);
                            }
                        }
                    }
                }
            }

            return userName;
        }

        /// <summary>
        /// Resets a user's password to a new, automatically generated password.
        /// </summary>
        public string ResetPassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return string.Empty;
            }

            var oldPassword = string.Empty;
            var randomPassword = Utils.RandomPassword();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT password FROM {0}Users WHERE BlogID = {1}blogid AND userName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                oldPassword = rdr.GetString(0);
                            }
                        }

                        // Update Password
                        if (!string.IsNullOrEmpty(oldPassword))
                        {
                            cmd.CommandText = string.Format("UPDATE {0}Users SET password = {1}pwd WHERE BlogID = {1}blogid AND userName = {1}name", this.tablePrefix, this.parmPrefix);

                            cmd.Parameters.Add(conn.CreateParameter(FormatParamName("pwd"), (this.passwordFormat == PasswordFormat.Hashed ? Utils.HashPassword(randomPassword) : randomPassword)));

                            cmd.ExecuteNonQuery();
                            return randomPassword;
                        }
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Not supported — returns false.
        /// </summary>
        public bool UnlockUser(string userName)
        {
            return false;
        }

        /// <summary>
        /// Update User Data (not password).
        /// </summary>
        public void UpdateUser(BlogMembershipUser user)
        {
            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("UPDATE {0}Users SET emailAddress = {1}email WHERE BlogId = {1}blogId AND userName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        var parms = cmd.Parameters;
                        parms.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        parms.Add(conn.CreateParameter(FormatParamName("name"), user.UserName));
                        parms.Add(conn.CreateParameter(FormatParamName("email"), user.Email));

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Check username and password.
        /// </summary>
        public bool ValidateUser(string username, string password)
        {
            var validated = false;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT password FROM {0}Users WHERE BlogID = {1}blogid AND UserName = {1}name", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                var storedPwd = rdr.GetString(0);

                                if (storedPwd == string.Empty)
                                {
                                    // This is a special case used for resetting.
                                    if (password.ToLower() == "admin")
                                    {
                                        validated = true;
                                    }
                                }
                                else
                                {
                                    if (this.passwordFormat == PasswordFormat.Hashed)
                                    {
                                        if (storedPwd == Utils.HashPassword(password))
                                        {
                                            validated = true;
                                        }
                                    }
                                    else if (storedPwd == password)
                                    {
                                        validated = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return validated;
        }

        #endregion

        #region Methods

        private DbConnectionHelper CreateConnection()
        {
            return new DbConnectionHelper(this.providerName, this.connectionString);
        }

        /// <summary>
        /// Returns a formatted parameter name to include this instance's paramPrefix.
        /// </summary>
        private string FormatParamName(string parameterName)
        {
            return $"{parmPrefix}{parameterName}";
        }

        /// <summary>
        /// Gets membership user.
        /// </summary>
        private BlogMembershipUser GetMembershipUser(string userName, string email, DateTime lastLogin)
        {
            var user = new BlogMembershipUser(
                "DbMembershipService", // Provider name
                userName, // Username
                userName, // providerUserKey
                email, // Email
                string.Empty, // passwordQuestion
                string.Empty, // Comment
                true, // approved
                false, // isLockedOut
                DateTime.Now, // creationDate
                lastLogin, // lastLoginDate
                DateTime.Now, // lastActivityDate
                DateTime.Now, // lastPasswordChangedDate
                new DateTime(1980, 1, 1)); // lastLockoutDate
            return user;
        }

        #endregion
    }
}
