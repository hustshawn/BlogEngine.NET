namespace BlogEngine.Core
{
    using System.Security.Claims;
    using System.Security.Principal;

    /// <summary>
    /// Custom principal used for authorization. Role membership is resolved per call from the
    /// role service (as the original did with Roles.IsUserInRole), never from a claims snapshot.
    /// </summary>
    public class CustomPrincipal : ClaimsPrincipal
    {
        private readonly IIdentity _identity;

        /// <summary>
        /// Creates a new instance using the given IIdentity object.
        /// </summary>
        public CustomPrincipal(IIdentity identity)
            : base(identity as ClaimsIdentity ?? new ClaimsIdentity())
        {
            _identity = identity;
        }

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
            return Security.RoleService?.IsUserInRole(Identity.Name, roleName) ?? false;
        }
    }
}
