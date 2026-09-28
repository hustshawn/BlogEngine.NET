using System.Collections.Generic;
using System.Linq;

namespace BlogEngine.Core
{
    /// <summary>
    /// Static facade replacing System.Web.Security.Membership.
    /// Delegates to the registered IMembershipService.
    /// </summary>
    public static class Membership
    {
        private static Services.Security.IMembershipService _service;

        public static void SetProvider(Services.Security.IMembershipService service) => _service = service;

        public static Services.Security.IMembershipService Provider => _service;

        /// <summary>
        /// Gets the user with the given name, or null if there is no such user.
        /// </summary>
        public static MembershipUser GetUser(string username)
        {
            return MembershipUser.From(_service?.GetUser(username));
        }

        public static MembershipUser GetUser(string username, bool userIsOnline)
        {
            return GetUser(username);
        }

        /// <summary>
        /// Creates a user; returns null if the provider failed to create it.
        /// </summary>
        public static MembershipUser CreateUser(string username, string password, string email)
        {
            return MembershipUser.From(_service?.CreateUser(username, password, email));
        }

        public static bool DeleteUser(string username)
        {
            return _service?.DeleteUser(username, true) ?? false;
        }

        /// <summary>
        /// Persists the e-mail and last login date of the user (not the password).
        /// </summary>
        public static void UpdateUser(MembershipUser user)
        {
            var stored = user == null ? null : _service?.GetUser(user.UserName);
            if (stored == null)
            {
                return;
            }

            stored.Email = user.Email;
            stored.LastLoginDate = user.LastLoginDate;
            stored.IsApproved = user.IsApproved;
            _service.UpdateUser(stored);
        }

        public static bool ValidateUser(string username, string password)
        {
            return _service?.ValidateUser(username, password) ?? false;
        }

        public static MembershipUserCollection GetAllUsers()
        {
            return new MembershipUserCollection(_service);
        }

        /// <summary>
        /// Minimum password length (the original XmlMembershipProvider/DbMembershipProvider used 5).
        /// </summary>
        public static int MinRequiredPasswordLength => 5;

        public static string GetUserNameByEmail(string email)
        {
            return _service?.GetUserNameByEmail(email);
        }

        public static MembershipUser CreateUser(string username, string password, string email,
            string passwordQuestion, string passwordAnswer, bool isApproved, out MembershipCreateStatus status)
        {
            try
            {
                if (_service?.GetUser(username) != null)
                {
                    status = MembershipCreateStatus.DuplicateUserName;
                    return null;
                }

                var user = MembershipUser.From(_service?.CreateUser(username, password, email));
                status = user != null ? MembershipCreateStatus.Success : MembershipCreateStatus.ProviderError;
                return user;
            }
            catch (System.Exception ex)
            {
                Utils.Log("Membership.CreateUser failed", ex);
                status = MembershipCreateStatus.ProviderError;
                return null;
            }
        }

        public static MembershipUser GetUser()
        {
            // Return current authenticated user
            var username = Security.CurrentUser?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                return GetUser(username);
            }
            return null;
        }
    }

    /// <summary>
    /// Replacement for System.Web.Security.MembershipCreateStatus.
    /// </summary>
    public enum MembershipCreateStatus
    {
        Success,
        InvalidUserName,
        InvalidPassword,
        InvalidEmail,
        DuplicateUserName,
        DuplicateEmail,
        ProviderError
    }

    /// <summary>
    /// Lightweight replacement for System.Web.Security.MembershipUserCollection.
    /// </summary>
    public class MembershipUserCollection : IEnumerable<MembershipUser>
    {
        private readonly Services.Security.IMembershipService _service;
        public MembershipUserCollection(Services.Security.IMembershipService service) => _service = service;
        public System.Collections.IEnumerator GetEnumerator() => ((IEnumerable<MembershipUser>)this).GetEnumerator();
        IEnumerator<MembershipUser> IEnumerable<MembershipUser>.GetEnumerator()
        {
            if (_service == null)
            {
                yield break;
            }

            foreach (var user in _service.GetAllUsers(out _))
            {
                yield return MembershipUser.From(user);
            }
        }
    }

    /// <summary>
    /// Lightweight replacement for System.Web.Security.MembershipUser.
    /// </summary>
    public class MembershipUser
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public System.DateTime CreationDate { get; set; }
        public System.DateTime LastLoginDate { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsApproved { get; set; }

        /// <summary>
        /// Maps a membership service user to the facade type (null stays null).
        /// </summary>
        internal static MembershipUser From(BlogMembershipUser user)
        {
            if (user == null)
            {
                return null;
            }

            return new MembershipUser
            {
                UserName = user.UserName,
                Email = user.Email,
                CreationDate = user.CreationDate,
                LastLoginDate = user.LastLoginDate,
                IsLockedOut = user.IsLockedOut,
                IsApproved = user.IsApproved
            };
        }

        public bool ChangePassword(string oldPassword, string newPassword)
        {
            return Membership.Provider?.ChangePassword(UserName, oldPassword, newPassword) ?? false;
        }
    }

    /// <summary>
    /// Static facade replacing System.Web.Security.Roles.
    /// Delegates to the registered IRoleService.
    /// </summary>
    public static class Roles
    {
        private static Services.Security.IRoleService _service;

        public static void SetProvider(Services.Security.IRoleService service) => _service = service;

        public static Services.Security.IRoleService Provider => _service;

        public static bool IsUserInRole(string username, string roleName)
            => _service?.IsUserInRole(username, roleName) ?? false;

        public static string[] GetRolesForUser(string username)
            => _service?.GetRolesForUser(username) ?? System.Array.Empty<string>();

        public static string[] GetAllRoles()
            => _service?.GetAllRoles() ?? System.Array.Empty<string>();

        public static void AddUserToRole(string username, string roleName)
            => _service?.AddUsersToRoles(new[] { username }, new[] { roleName });

        public static void RemoveUserFromRole(string username, string roleName)
            => _service?.RemoveUsersFromRoles(new[] { username }, new[] { roleName });

        public static bool RoleExists(string roleName)
            => _service?.RoleExists(roleName) ?? false;

        public static void CreateRole(string roleName)
            => _service?.CreateRole(roleName);

        public static bool DeleteRole(string roleName)
        {
            _service?.DeleteRole(roleName, false);
            return true;
        }

        public static void AddUsersToRoles(string[] usernames, string[] roleNames)
            => _service?.AddUsersToRoles(usernames, roleNames);

        public static void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
            => _service?.RemoveUsersFromRoles(usernames, roleNames);

        public static void RemoveUserFromRoles(string username, string[] roleNames)
            => _service?.RemoveUsersFromRoles(new[] { username }, roleNames);

        public static string[] GetUsersInRole(string roleName)
            => _service?.GetUsersInRole(roleName) ?? System.Array.Empty<string>();
    }
}
