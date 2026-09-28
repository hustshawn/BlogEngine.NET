using Microsoft.AspNetCore.Authentication;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using BlogEngine.Core.Services.Security;
using Microsoft.AspNetCore.Http;

namespace BlogEngine.Core
{
    /// <summary>
    /// Class to provide a unified area of authentication/authorization checking.
    /// Converted from IHttpModule to a service class with static helpers.
    /// </summary>
    public partial class Security
    {
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
        /// Gets the current HttpContext, or null if unavailable.
        /// </summary>
        private static HttpContext CurrentHttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// Name of the authentication cookie for the current blog instance.
        /// </summary>
        public static string AuthCookieName
        {
            get
            {
                const string baseCookieName = ".BlogEngine.Auth";
                if (BlogConfig.SingleSignOn)
                {
                    return baseCookieName;
                }
                return baseCookieName + "-" + Blog.CurrentInstance.Id.ToString();
            }
        }

        /// <summary>
        /// Signs out user out of the current blog instance.
        /// </summary>
        public static void SignOut()
        {
            var context = CurrentHttpContext;
            if (context != null)
            {
                // Sign out using ASP.NET Core cookie authentication
                context.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
            }

            // admin UI injects user info in JavaScript resource file
            // so it can be used in Angular scripts. Clear cache on log off.
            string cacheKey = "admin.resource.axd - " + BlogSettings.Instance.Culture;
            Blog.CurrentInstance.Cache.Remove(cacheKey);
        }

        /// <summary>
        /// Attempts to sign the user into the current blog instance.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="rememberMe">Whether or not to persist the user's sign-in state.</param>
        /// <returns>True if the user is successfully authenticated and signed in; false otherwise.</returns>
        public static bool AuthenticateUser(string username, string password, bool rememberMe)
        {
            string un = (username ?? string.Empty).Trim();
            string pw = (password ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(un) && !string.IsNullOrWhiteSpace(pw))
            {
                bool isValidated = false;
                if (_membershipService != null)
                {
                    isValidated = _membershipService.ValidateUser(un, pw);
                }

                if (isValidated)
                {
                    var context = CurrentHttpContext;
                    if (context != null)
                    {
                        // Build claims-based identity
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, un)
                        };

                        // Add role claims
                        if (_roleService != null)
                        {
                            var roles = _roleService.GetRolesForUser(un);
                            foreach (var role in roles)
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }
                        }

                        // Add blog instance claim for security validation
                        if (!BlogConfig.SingleSignOn)
                        {
                            claims.Add(new Claim("BlogInstanceId", Blog.CurrentInstance.Id.ToString()));
                            claims.Add(new Claim("SecurityValidationKey", SecurityValidationKey));
                        }

                        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);
                        context.User = principal;

                        // Use ASP.NET Core's cookie authentication for persistence
                        var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                        {
                            IsPersistent = rememberMe,
                            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : (DateTimeOffset?)null
                        };

                        context.SignInAsync(
                            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                            principal,
                            authProperties).GetAwaiter().GetResult();

                        string returnUrl = context.Request.Query["returnUrl"];

                        // ignore Return URLs not beginning with a forward slash, such as remote sites.
                        if (IsLocalUrl(returnUrl))
                        {
                            context.Response.Redirect(returnUrl);
                        }
                        else
                        {
                            context.Response.Redirect(Utils.RelativeWebRoot);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            else
            {
                return ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) || // "/" or "/foo" but not "//" or "/\"
                        (url.Length > 1 && url[0] == '~' && url[1] == '/')); // "~/" or "~/foo"
            }
        }

        private const string AUTH_TKT_USERDATA_DELIMITER = "-|-";

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
        public static System.Security.Principal.IPrincipal CurrentUser
        {
            get
            {
                var context = CurrentHttpContext;
                if (context?.User?.Identity?.IsAuthenticated == true)
                {
                    return context.User;
                }

                // Anonymous: like System.Web, expose an empty (not null) user name
                var identity = new CustomIdentity(string.Empty, false);
                return new CustomPrincipal(identity);
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
