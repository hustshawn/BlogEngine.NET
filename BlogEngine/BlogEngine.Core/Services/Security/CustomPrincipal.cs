using System.Security.Claims;
﻿namespace BlogEngine.Core
{
    using System.Security.Principal;
    using BlogEngine.Core.Services.Security;

    /// <summary>
    /// Custom IPrincipal class used for authorization.
    /// </summary>
    public class CustomPrincipal : ClaimsPrincipal
    {
        private IIdentity _identity;

        /// <summary>
        /// Gets the identity of the current principal.
        /// </summary>
        public override IIdentity Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Determines whether the current principal belongs to the specified role.
        /// </summary>
        /// <param name="roleName">The name of the role for which to check membership.</param>
        /// <returns>true if the current principal is a member of the specified role; otherwise, false.</returns>
        public override bool IsInRole(string roleName)
        {
            if (Identity == null || !Identity.IsAuthenticated || string.IsNullOrEmpty(Identity.Name))
                return false;

            // Note: Cannot use "Security.CurrentUser.IsInRole" or anything similar since
            // Security.CurrentUser.IsInRole will look to this IsInRole() method here --
            // resulting in an endless loop. Need to query the role service directly.

            // Access the role service via reflection from Security class (same pattern as CustomIdentity)
            try
            {
                var field = typeof(Security).GetField("_roleService",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    var roleService = field.GetValue(null) as IRoleService;
                    if (roleService != null)
                    {
                        return roleService.IsUserInRole(Identity.Name, roleName);
                    }
                }
            }
            catch
            {
                // Fall through
            }

            return false;
        }

        /// <summary>
        /// Creates a new instance using the given IIdentity object.
        /// </summary>
        public CustomPrincipal(IIdentity identity)
        {
            _identity = identity;
        }
    }
}
