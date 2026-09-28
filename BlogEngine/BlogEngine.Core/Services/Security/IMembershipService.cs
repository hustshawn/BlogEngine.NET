namespace BlogEngine.Core.Services.Security
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Service interface for user membership operations.
    /// Replaces System.Web.Security.MembershipProvider.
    /// </summary>
    public interface IMembershipService
    {
        /// <summary>
        /// Validates the given username and password.
        /// </summary>
        bool ValidateUser(string username, string password);

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="email">The email.</param>
        /// <returns>The created membership user.</returns>
        BlogMembershipUser CreateUser(string username, string password, string email);

        /// <summary>
        /// Retrieves a user by username.
        /// </summary>
        BlogMembershipUser GetUser(string username);

        /// <summary>
        /// Retrieves a user by provider user key.
        /// </summary>
        BlogMembershipUser GetUser(object providerUserKey);

        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        bool ChangePassword(string username, string oldPassword, string newPassword);

        /// <summary>
        /// Resets a user's password to a new, automatically generated password.
        /// </summary>
        string ResetPassword(string username);

        /// <summary>
        /// Retrieves a collection of all users.
        /// </summary>
        ICollection<BlogMembershipUser> GetAllUsers(out int totalRecords);

        /// <summary>
        /// Removes a user.
        /// </summary>
        bool DeleteUser(string username, bool deleteAllRelatedData);

        /// <summary>
        /// Updates a user (not password).
        /// </summary>
        void UpdateUser(BlogMembershipUser user);

        /// <summary>
        /// Gets the user name associated with the specified e-mail address.
        /// </summary>
        string GetUserNameByEmail(string email);

        /// <summary>
        /// Gets the number of users currently online. Returns 0 if not supported.
        /// </summary>
        int GetNumberOfUsersOnline();

        /// <summary>
        /// Clears a lock so that the membership user can be validated.
        /// </summary>
        bool UnlockUser(string userName);

        /// <summary>
        /// Gets a value indicating the format for storing passwords.
        /// </summary>
        PasswordFormat PasswordFormatSetting { get; }
    }

    /// <summary>
    /// Password storage format, replacing System.Web.Security.MembershipPasswordFormat.
    /// </summary>
    public enum PasswordFormat
    {
        /// <summary>
        /// Password stored in clear text.
        /// </summary>
        Clear = 0,

        /// <summary>
        /// Password stored as a hash.
        /// </summary>
        Hashed = 1
    }
}
