using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlogEngine.Core.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace BlogEngine.Core
{
    /// <summary>
    /// Class to provide a unified area of authentication/authorization checking.
    /// Converted from IHttpModule to a service class with static helpers; the per-request
    /// ticket validation of the original AuthenticateRequest handler lives in <see cref="ValidatePrincipalAsync"/>.
    /// </summary>
    public partial class Security
    {
        /// <summary>
        /// The authentication scheme used for the BlogEngine auth cookie (configured in Program.cs).
        /// </summary>
        public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Claim holding the id of the blog instance the user signed in to.
        /// </summary>
        public const string BlogInstanceIdClaimType = "BlogEngine.BlogInstanceId";

        /// <summary>
        /// Claim holding a hash of the blog's SecurityValidationKey at sign-in time.
        /// </summary>
        public const string SecurityValidationClaimType = "BlogEngine.SecurityValidation";

        /// <summary>
        /// Static reference to the role service.
        /// </summary>
        private static IRoleService _roleService;

        /// <summary>
        /// Static reference to the membership service.
        /// </summary>
        private static IMembershipService _membershipService;

        /// <summary>
        /// Static reference to the HTTP context accessor.
        /// </summary>
        private static IHttpContextAccessor _httpContextAccessor;

        static Security()
        {
        }

        /// <summary>
        /// Initializes the Security class with required services.
        /// Must be called during application startup.
        /// </summary>
        public static void Initialize(
            IRoleService roleService,
            IMembershipService membershipService,
            IHttpContextAccessor httpContextAccessor)
        {
            _roleService = roleService;
            _membershipService = membershipService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The role service used to resolve roles per request (CustomPrincipal).
        /// </summary>
        internal static IRoleService RoleService => _roleService;

        /// <summary>
        /// The membership service used to validate credentials (CustomIdentity).
        /// </summary>
        internal static IMembershipService MembershipService => _membershipService;

        /// <summary>
        /// Gets the current HttpContext, or null if unavailable.
        /// </summary>
        private static HttpContext CurrentHttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// Signs out user out of the current blog instance.
        /// </summary>
        public static void SignOut()
        {
            SignOutAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Signs out user out of the current blog instance.
        /// </summary>
        public static async Task SignOutAsync()
        {
            var context = CurrentHttpContext;
            if (context != null)
            {
                await context.SignOutAsync(AuthenticationScheme);
                context.User = new CustomPrincipal(new CustomIdentity(string.Empty, false));
            }

            // admin UI injects user info in JavaScript resource file
            // so it can be used in Angular scripts. Clear cache on log off.
            string cacheKey = "admin.resource.axd - " + BlogSettings.Instance.Culture;
            Blog.CurrentInstance.Cache.Remove(cacheKey);
        }

        /// <summary>
        /// Attempts to sign the user into the current blog instance.
        /// Unlike the original (which issued Response.Redirect), the caller is responsible for redirecting
        /// afterwards, e.g. to <see cref="GetSafeReturnUrl"/>.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="rememberMe">Whether or not to persist the user's sign-in state.</param>
        /// <returns>True if the user is successfully authenticated and signed in; false otherwise.</returns>
        public static bool AuthenticateUser(string username, string password, bool rememberMe)
        {
            return AuthenticateUserAsync(username, password, rememberMe).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Attempts to sign the user into the current blog instance.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="rememberMe">Whether or not to persist the user's sign-in state.</param>
        /// <returns>True if the user is successfully authenticated and signed in; false otherwise.</returns>
        public static async Task<bool> AuthenticateUserAsync(string username, string password, bool rememberMe)
        {
            string un = (username ?? string.Empty).Trim();
            string pw = (password ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(pw) || _membershipService == null)
            {
                return false;
            }

            if (!_membershipService.ValidateUser(un, pw))
            {
                return false;
            }

            var context = CurrentHttpContext;
            if (context == null)
            {
                return false;
            }

            // use the stored user name casing
            var user = _membershipService.GetUser(un);
            if (user != null && !string.IsNullOrEmpty(user.UserName))
            {
                un = user.UserName;
            }

            // Roles are intentionally not stored in the cookie: they are resolved per request
            // from the role service by CustomPrincipal, as the original role provider did.
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, un) };

            if (!BlogConfig.SingleSignOn)
            {
                // Original: the forms ticket UserData carried "SecurityValidationKey-|-BlogId" so a ticket
                // issued for another blog instance (or before the key was changed) is rejected.
                claims.Add(new Claim(BlogInstanceIdClaimType, Blog.CurrentInstance.Id.ToString()));
                claims.Add(new Claim(SecurityValidationClaimType, SecurityValidationHash));
            }

            var identity = new ClaimsIdentity(claims, AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            // Expiration and sliding renewal come from the cookie options (Program.cs);
            // a non-persistent cookie is a browser-session cookie, as in the original.
            var properties = new AuthenticationProperties { IsPersistent = rememberMe };

            await context.SignInAsync(AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            // make the user available for the rest of this request
            context.User = new CustomPrincipal(identity);

            return true;
        }

        /// <summary>
        /// Validates the auth cookie principal on every request (port of the original
        /// AuthenticateRequest handler). Rejects tickets issued for another blog instance or with a
        /// stale SecurityValidationKey, and replaces the principal with a <see cref="CustomPrincipal"/>
        /// so that roles are resolved from the role service rather than from the cookie.
        /// Wire it to <c>CookieAuthenticationEvents.OnValidatePrincipal</c>.
        /// </summary>
        public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            var identity = context.Principal?.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated || string.IsNullOrEmpty(identity.Name))
            {
                await RejectAsync(context);
                return;
            }

            if (!BlogConfig.SingleSignOn)
            {
                string blogId = identity.FindFirst(BlogInstanceIdClaimType)?.Value;
                string validation = identity.FindFirst(SecurityValidationClaimType)?.Value;

                if (blogId == null || validation == null ||
                    !blogId.Equals(Blog.CurrentInstance.Id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(validation), Encoding.ASCII.GetBytes(SecurityValidationHash)))
                {
                    await RejectAsync(context);
                    return;
                }
            }

            context.ReplacePrincipal(new CustomPrincipal(identity));
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AuthenticationScheme);
        }

        /// <summary>
        /// Returns whether the url is a local (same application) url; rejects "//host", "/\host"
        /// and control characters that browsers strip. Mirrors ASP.NET Core's Url.IsLocalUrl.
        /// </summary>
        public static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            int start;
            if (url[0] == '/')
            {
                start = 1;
            }
            else if (url.Length > 1 && url[0] == '~' && url[1] == '/')
            {
                start = 2;
            }
            else
            {
                return false;
            }

            if (url.Length == start)
            {
                return true;
            }

            if (url[start] == '/' || url[start] == '\\')
            {
                return false;
            }

            return !url.Skip(start).Any(char.IsControl);
        }

        /// <summary>
        /// Returns the given return url if it is local, otherwise the blog root.
        /// "~/" urls are resolved against the blog's relative web root.
        /// </summary>
        public static string GetSafeReturnUrl(string returnUrl)
        {
            if (!IsLocalUrl(returnUrl))
            {
                return Utils.RelativeWebRoot;
            }

            return returnUrl.StartsWith("~/", StringComparison.Ordinal)
                ? Utils.RelativeWebRoot + returnUrl.Substring(2)
                : returnUrl;
        }

        private static string SecurityValidationKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BlogSettings.Instance.SecurityValidationKey))
                {
                    BlogSettings.Instance.SecurityValidationKey = string.Format("{0}{1}{2}",
                        Guid.NewGuid().ToString().Replace("-", string.Empty),
                        Guid.NewGuid().ToString().Replace("-", string.Empty),
                        Guid.NewGuid().ToString().Replace("-", string.Empty));
                    BlogSettings.Instance.Save();
                }

                return BlogSettings.Instance.SecurityValidationKey;
            }
        }

        /// <summary>
        /// Hash of the SecurityValidationKey stored in the (encrypted) auth cookie,
        /// so the key itself never leaves the server.
        /// </summary>
        private static string SecurityValidationHash
        {
            get
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(SecurityValidationKey));
                return Convert.ToHexString(hash);
            }
        }

        #region "Properties"

        /// <summary>
        /// If the current user is authenticated, returns the current BlogMembershipUser. If not, returns null.
        /// </summary>
        public static BlogMembershipUser CurrentMembershipUser
        {
            get
            {
                if (!IsAuthenticated)
                    return null;

                return _membershipService?.GetUser(CurrentUser.Identity.Name);
            }
        }

        /// <summary>
        /// Gets the current user for the current HttpContext.
        /// </summary>
        /// <remarks>
        /// Always a <see cref="CustomPrincipal"/>, whose IsInRole queries the role service, so role and
        /// right checks reflect the current roles data rather than a snapshot taken at sign-in.
        /// </remarks>
        public static System.Security.Principal.IPrincipal CurrentUser
        {
            get
            {
                var user = CurrentHttpContext?.User;
                if (user is CustomPrincipal)
                {
                    return user;
                }

                if (user?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(user.Identity.Name))
                {
                    return new CustomPrincipal(user.Identity);
                }

                // Anonymous: like System.Web, expose an empty (not null) user name
                return new CustomPrincipal(new CustomIdentity(string.Empty, false));
            }
        }

        /// <summary>
        /// Gets whether the current user is logged in.
        /// </summary>
        public static bool IsAuthenticated
        {
            get
            {
                return Security.CurrentUser.Identity.IsAuthenticated;
            }
        }

        /// <summary>
        /// Gets whether the currently logged in user is in the administrator role.
        /// </summary>
        public static bool IsAdministrator
        {
            get
            {
                return (Security.IsAuthenticated && Security.CurrentUser.IsInRole(BlogConfig.AdministratorRole));
            }
        }

        /// <summary>
        /// Returns an IEnumerable of Rights that belong to the current user.
        /// </summary>
        public static IEnumerable<Right> CurrentUserRights()
        {
            return Right.GetRights(Security.GetCurrentUserRoles());
        }

        #endregion

        #region "Public Methods"

        /// <summary>
        /// If the current user does not have the requested right, either redirects to the login page,
        /// or throws a SecurityException.
        /// </summary>
        public static void DemandUserHasRight(Rights right, bool redirectToLoginPage)
        {
            DemandUserHasRight(AuthorizationCheck.HasAny, redirectToLoginPage, new[] { right });
        }

        /// <summary>
        /// If the current user does not have the requested rights, either redirects to the login page,
        /// or throws a SecurityException.
        /// </summary>
        public static void DemandUserHasRight(AuthorizationCheck authCheck, bool redirectIfUnauthorized, params Rights[] rights)
        {
            if (!IsAuthorizedTo(authCheck, rights))
            {
                if (redirectIfUnauthorized)
                {
                    RedirectForUnauthorizedRequest();
                }
                else
                {
                    throw new SecurityException("User doesn't have the right to perform this");
                }
            }
        }

        /// <summary>
        /// Redirects unauthenticated users to the login page.
        /// </summary>
        public static void RedirectForUnauthorizedRequest()
        {
            var context = CurrentHttpContext;
            if (context == null) return;

            var referrer = context.Request.Headers["Referer"].ToString();
            bool isFromLoginPage = !string.IsNullOrEmpty(referrer) &&
                referrer.IndexOf("/Account/login", StringComparison.OrdinalIgnoreCase) != -1;

            // If the user was just redirected from the login page to the current page,
            // redirect them to the homepage to prevent confusion.
            if (isFromLoginPage)
            {
                context.Response.Redirect(Utils.RelativeWebRoot);
            }
            else
            {
                context.Response.Redirect(string.Format("{0}Account/login?ReturnURL={1}",
                    Utils.RelativeWebRoot,
                    Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)));
            }
        }

        /// <summary>
        /// Returns whether or not the current user has the passed in Right.
        /// </summary>
        public static bool IsAuthorizedTo(Rights right)
        {
            return Right.HasRight(right, Security.GetCurrentUserRoles());
        }

        /// <summary>
        /// Returns whether the current user passes authorization on the rights based on the given AuthorizationCheck.
        /// </summary>
        public static bool IsAuthorizedTo(AuthorizationCheck authCheck, IEnumerable<Rights> rights)
        {
            if (!rights.Any())
            {
                return false;
            }
            else
            {
                var roles = Security.GetCurrentUserRoles();

                if (authCheck == AuthorizationCheck.HasAny)
                {
                    foreach (var right in rights)
                    {
                        if (Right.HasRight(right, roles))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                else if (authCheck == AuthorizationCheck.HasAll)
                {
                    bool authCheckPassed = true;

                    foreach (var right in rights)
                    {
                        if (!Right.HasRight(right, roles))
                        {
                            authCheckPassed = false;
                            break;
                        }
                    }
                    return authCheckPassed;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Returns whether a role is a System role.
        /// </summary>
        public static bool IsSystemRole(string roleName)
        {
            if (roleName.Equals(BlogConfig.AdministratorRole, StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals(BlogConfig.AnonymousRole, StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals(BlogConfig.EditorsRole, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the current user passes authorization on the rights based on the given AuthorizationCheck.
        /// </summary>
        public static bool IsAuthorizedTo(AuthorizationCheck authCheck, params Rights[] rights)
        {
            return IsAuthorizedTo(authCheck, rights.ToList());
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Helper method that returns the correct roles based on authentication.
        /// </summary>
        public static string[] GetCurrentUserRoles()
        {
            if (!IsAuthenticated)
            {
                return new[] { BlogConfig.AnonymousRole };
            }
            else
            {
                if (_roleService != null)
                {
                    return _roleService.GetRolesForUser(CurrentUser.Identity.Name);
                }

                // Fall back to claims-based roles
                var claimsPrincipal = CurrentUser as ClaimsPrincipal;
                if (claimsPrincipal != null)
                {
                    return claimsPrincipal.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray();
                }

                return new[] { BlogConfig.AnonymousRole };
            }
        }

        /// <summary>
        /// Impersonates a user for the duration of the HTTP request.
        /// </summary>
        public static bool ImpersonateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            CustomIdentity identity = new CustomIdentity(username, password);
            if (!identity.IsAuthenticated) { return false; }

            CustomPrincipal principal = new CustomPrincipal(identity);

            // Make the custom principal be the user for the rest of this request.
            var context = CurrentHttpContext;
            if (context != null)
            {
                context.User = principal;
            }

            return true;
        }

        #endregion
    }


    /// <summary>
    /// Enum for setting how rights should be checked for.
    /// </summary>
    public enum AuthorizationCheck
    {
        /// <summary>
        /// A user will be considered authorized if they have any of the given Rights.
        /// </summary>
        HasAny,

        /// <summary>
        /// A user will be considered authorized if they have all of the given Rights.
        /// </summary>
        HasAll
    }
}
