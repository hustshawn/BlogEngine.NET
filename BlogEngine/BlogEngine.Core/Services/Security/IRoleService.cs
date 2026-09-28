namespace BlogEngine.Core.Services.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// Service interface for role management operations.
    /// Replaces System.Web.Security.RoleProvider.
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// Adds a new role.
        /// </summary>
        void CreateRole(string roleName);

        /// <summary>
        /// Removes a role.
        /// </summary>
        bool DeleteRole(string roleName, bool throwOnPopulatedRole);

        /// <summary>
        /// Adds the specified user names to the specified roles.
        /// </summary>
        void AddUsersToRoles(string[] usernames, string[] roleNames);

        /// <summary>
        /// Removes the specified user names from the specified roles.
        /// </summary>
        void RemoveUsersFromRoles(string[] usernames, string[] roleNames);

        /// <summary>
        /// Gets a list of the roles that a specified user is in.
        /// </summary>
        string[] GetRolesForUser(string username);

        /// <summary>
        /// Gets a list of users in the specified role.
        /// </summary>
        string[] GetUsersInRole(string roleName);

        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role.
        /// </summary>
        bool IsUserInRole(string username, string roleName);

        /// <summary>
        /// Gets a value indicating whether the specified role name already exists.
        /// </summary>
        bool RoleExists(string roleName);

        /// <summary>
        /// Gets a list of all the roles.
        /// </summary>
        string[] GetAllRoles();

        /// <summary>
        /// Gets an array of user names in a role where the user name contains the specified user name to match.
        /// </summary>
        string[] FindUsersInRole(string roleName, string usernameToMatch);
    }
}
