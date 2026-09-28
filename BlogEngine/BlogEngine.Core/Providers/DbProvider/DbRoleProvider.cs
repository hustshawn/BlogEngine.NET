namespace BlogEngine.Core.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using BlogEngine.Core.Services.Security;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Generic Db Role Service.
    /// Replaces the legacy DbRoleProvider (RoleProvider).
    /// </summary>
    public class DbRoleService : IRoleService
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
        /// Creates a new DbRoleService with the specified configuration.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="connectionStringName">Name of the connection string. Defaults to "BlogEngine".</param>
        /// <param name="tablePrefix">Table prefix. Defaults to "be_".</param>
        /// <param name="parmPrefix">Parameter prefix. Defaults to "@".</param>
        public DbRoleService(
            IConfiguration configuration,
            string connectionStringName = "BlogEngine",
            string tablePrefix = "be_",
            string parmPrefix = "@")
        {
            this.connStringName = connectionStringName;
            this.tablePrefix = tablePrefix;
            this.parmPrefix = parmPrefix;

            this.connectionString = configuration.GetConnectionString(connectionStringName);
            this.providerName = configuration[$"ConnectionStrings:{connectionStringName}_ProviderName"]
                ?? "Microsoft.Data.SqlClient";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds all users in user array to all roles in role array.
        /// </summary>
        public void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;

                        var parms = cmd.Parameters;

                        foreach (var user in usernames)
                        {
                            foreach (var role in roleNames)
                            {
                                if (!role.Equals(BlogConfig.AnonymousRole))
                                {
                                    parms.Clear();
                                    cmd.CommandText = string.Format("INSERT INTO {0}UserRoles (BlogID, UserName, Role) VALUES ({1}blogID, {1}username, {1}role)", this.tablePrefix, this.parmPrefix);

                                    parms.Add(conn.CreateParameter(FormatParamName("blogID"), Blog.CurrentInstance.Id.ToString()));
                                    parms.Add(conn.CreateParameter(FormatParamName("username"), user.Trim()));
                                    parms.Add(conn.CreateParameter(FormatParamName("role"), role.Trim()));

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }

            // This needs to be called in order to keep the Right class in sync.
            Right.RefreshAllRights();
        }

        /// <summary>
        /// Adds a new role to the database.
        /// </summary>
        public void CreateRole(string roleName)
        {
            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("INSERT INTO {0}Roles (BlogID, role) VALUES ({1}blogid, {1}role)", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("role"), roleName));
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // This needs to be called in order to keep the Right class in sync.
            // SQL Server on slow connections need few seconds to complete query
            System.Threading.Thread.Sleep(5000);
            Right.RefreshAllRights();
        }

        /// <summary>
        /// Removes a role from database.
        /// </summary>
        public bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            var success = false;

            if (!Security.IsSystemRole(roleName))
            {
                using (var conn = this.CreateConnection())
                {
                    if (conn.HasConnection)
                    {
                        using (var cmd = conn.CreateTextCommand(string.Format("DELETE FROM {0}Roles WHERE BlogID = {1}blogid AND Role = {1}role", this.tablePrefix, this.parmPrefix)))
                        {
                            cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                            cmd.Parameters.Add(conn.CreateParameter(FormatParamName("role"), roleName));
                            cmd.ExecuteNonQuery();
                            success = true;
                        }
                    }
                }
            }

            // This needs to be called in order to keep the Right class in sync.
            Right.RefreshAllRights();

            return success;
        }

        /// <summary>
        /// Returns all users in selected role with names that match usernameToMatch.
        /// </summary>
        public string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            var users = new List<string>();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    var sqlQuery = string.Format(
                        " SELECT ur.UserName " +
                        " FROM {0}UsersRoles ur " +
                        " WHERE ur.BlogID = {1}blogid " +
                        " AND   ur.Role   = {1}role " +
                        " AND   ur.UserName LIKE {1}name", this.tablePrefix, this.parmPrefix);

                    using (var cmd = conn.CreateTextCommand(sqlQuery))
                    {
                        var parms = cmd.Parameters;
                        parms.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        parms.Add(conn.CreateParameter(FormatParamName("role"), roleName));
                        parms.Add(conn.CreateParameter(FormatParamName("name"), $"{usernameToMatch}%"));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0))
                                {
                                    users.Add(rdr.GetString(0));
                                }
                            }
                        }
                    }
                }
            }

            return users.ToArray();
        }

        /// <summary>
        /// Returns array of all roles in database.
        /// </summary>
        public string[] GetAllRoles()
        {
            var roles = new List<string>();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand($"SELECT role FROM {tablePrefix}Roles WHERE BlogID = {parmPrefix}blogid "))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0))
                                {
                                    roles.Add(rdr.GetString(0));
                                }
                            }
                        }
                    }
                }
            }

            return roles.ToArray();
        }

        /// <summary>
        /// Return an array of roles that user is in.
        /// </summary>
        public string[] GetRolesForUser(string username)
        {
            var roles = new List<string>();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    var sqlQuery = string.Format(
                        " SELECT ur.role " +
                        " FROM {0}UserRoles ur " +
                        " WHERE ur.BlogID   = {1}blogid " +
                        " AND   ur.UserName = {1}name", this.tablePrefix, this.parmPrefix);

                    using (var cmd = conn.CreateTextCommand(sqlQuery))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("name"), username));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0))
                                {
                                    roles.Add(rdr.GetString(0));
                                }
                            }
                        }
                    }
                }
            }

            return roles.ToArray();
        }

        /// <summary>
        /// Returns array of users in selected role.
        /// </summary>
        public string[] GetUsersInRole(string roleName)
        {
            var users = new List<string>();

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    var sqlQuery = string.Format(
                        " SELECT ur.UserName " +
                        " FROM {0}UserRoles ur " +
                        " WHERE ur.BlogID = {1}blogid " +
                        " AND   ur.Role  = {1}role", this.tablePrefix, this.parmPrefix);

                    using (var cmd = conn.CreateTextCommand(sqlQuery))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("role"), roleName));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (!rdr.IsDBNull(0))
                                {
                                    users.Add(rdr.GetString(0));
                                }
                            }
                        }
                    }
                }
            }

            return users.ToArray();
        }

        /// <summary>
        /// Check to see if user is in a role.
        /// </summary>
        public bool IsUserInRole(string username, string roleName)
        {
            var roleFound = false;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    var sqlQuery = string.Format(
                        " SELECT ur.UserRoleID " +
                        " FROM {0}UserRoles ur " +
                        " WHERE ur.BlogID = {1}blogid " +
                        " AND   ur.UserName = {1}name " +
                        " AND   ur.role = {1}role", this.tablePrefix, this.parmPrefix);

                    using (var cmd = conn.CreateTextCommand(sqlQuery))
                    {
                        var parms = cmd.Parameters;
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        parms.Add(conn.CreateParameter(FormatParamName("name"), username));
                        parms.Add(conn.CreateParameter(FormatParamName("role"), roleName));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            roleFound = rdr.Read();
                        }
                    }
                }
            }

            return roleFound;
        }

        /// <summary>
        /// Removes all users in user array from all roles in role array.
        /// </summary>
        public void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;

                        var parms = cmd.Parameters;

                        foreach (var user in usernames)
                        {
                            foreach (var role in roleNames)
                            {
                                parms.Clear();
                                cmd.CommandText = string.Format("DELETE FROM {0}UserRoles WHERE BlogID = {1}blogid AND UserName = {1}username AND Role = {1}role", this.tablePrefix, this.parmPrefix);

                                parms.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                                parms.Add(conn.CreateParameter(FormatParamName("username"), user.Trim()));
                                parms.Add(conn.CreateParameter(FormatParamName("role"), role.Trim()));

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            // This needs to be called in order to keep the Right class in sync.
            Right.RefreshAllRights();
        }

        /// <summary>
        /// Checks to see if role exists.
        /// </summary>
        public bool RoleExists(string roleName)
        {
            var roleFound = false;

            using (var conn = this.CreateConnection())
            {
                if (conn.HasConnection)
                {
                    using (var cmd = conn.CreateTextCommand(string.Format("SELECT roleID FROM {0}Roles WHERE BlogID = {1}blogid AND role = {1}role", this.tablePrefix, this.parmPrefix)))
                    {
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("blogid"), Blog.CurrentInstance.Id.ToString()));
                        cmd.Parameters.Add(conn.CreateParameter(FormatParamName("role"), roleName));

                        using (var rdr = cmd.ExecuteReader())
                        {
                            roleFound = rdr.Read();
                        }
                    }
                }
            }

            return roleFound;
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

        #endregion
    }
}
