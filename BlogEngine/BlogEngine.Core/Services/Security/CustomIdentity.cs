namespace BlogEngine.Core
{
    using System;
    using System.Security.Principal;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// IIdentity class used for authentication.
    /// </summary>
    public class CustomIdentity : IIdentity
    {
        /// <summary>
        /// Gets the type of authentication used by BlogEngine.NET.
        /// </summary>
        public string AuthenticationType
        {
            get { return "BlogEngine.NET Custom Identity"; }
        }

        private bool _isAuthenticated;

        /// <summary>
        /// Gets a value that indicates whether the user has been authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
        }

        private string _name;

        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Creates a new instance using the specified username and isAuthenticated bit.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="isAuthenticated">Whether or not the user is authenticated.</param>
        public CustomIdentity(string username, bool isAuthenticated)
        {
            _name = username;
            _isAuthenticated = isAuthenticated;
        }

        /// <summary>
        /// Creates a new instance, authenticating the user using the given credentials.
        /// Uses the static membership service reference from the Security class.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        public CustomIdentity(string username, string password)
        {
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username));

            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            // Use the membership service registered with Security to validate
            var membershipUser = Security.CurrentMembershipUser;

            // Validate via a fresh call to the membership service
            // We access it through a helper since the service is static on Security
            bool validated = ValidateViaService(username, password);
            if (!validated) { return; }

            _isAuthenticated = true;
            _name = username;
        }

        /// <summary>
        /// Validates credentials using the membership service.
        /// </summary>
        private static bool ValidateViaService(string username, string password)
        {
            // The membership service is accessible via the Security class internals.
            // Since CustomIdentity needs to validate users, we expose a validation path.
            try
            {
                // Use the static membership service reference
                var field = typeof(Security).GetField("_membershipService",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    var service = field.GetValue(null) as IMembershipService;
                    if (service != null)
                    {
                        return service.ValidateUser(username, password);
                    }
                }
            }
            catch
            {
                // Fall through
            }

            return false;
        }
    }
}
