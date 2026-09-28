using BlogEngine.Core.Web.HttpModules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BlogEngine.Core
{
    /// <summary>
    /// Extension methods for registering BlogEngine Core middleware in the ASP.NET Core pipeline.
    /// </summary>
    public static class BlogEngineCoreMiddlewareExtensions
    {
        /// <summary>
        /// Adds the BlogEngine request pipeline modules (formerly web.config HttpModules):
        /// child blog virtual paths, www subdomain handling, HTTP compression and referrer tracking.
        /// Call before static files and routing.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseBlogEngineMiddleware(this IApplicationBuilder app)
        {
            app.Use(MoveBlogVirtualPathToPathBase);

            // RightModule: load the rights of the requested blog instance
            app.Use((context, next) =>
            {
                Right.EnsureInitialized();
                return next(context);
            });

            // WwwSubDomainModule was first in the original web.config module order
            app.UseMiddleware<WwwSubDomainMiddleware>();

            // CompressionModule: honour the "EnableHttpCompression" blog setting per request
            app.UseWhen(_ => BlogSettings.Instance.EnableHttpCompression, branch => branch.UseResponseCompression());

            app.UseMiddleware<ReferrerMiddleware>();

            return app;
        }

        /// <summary>
        /// Child blogs live under a virtual path (e.g. /blog2/). The blog instance is resolved from the
        /// full URL, then the virtual path moves to PathBase so routes, static files and endpoints match
        /// as for the primary blog, the way System.Web rewrote child blog URLs.
        /// </summary>
        private static async System.Threading.Tasks.Task MoveBlogVirtualPathToPathBase(HttpContext context, RequestDelegate next)
        {
            var blog = Blog.CurrentInstance; // resolved from the full path and cached for the request
            var blogRoot = blog?.RelativeWebRoot?.TrimEnd('/');
            var appRoot = Utils.ApplicationRelativeWebRoot.TrimEnd('/');

            if (blog == null || string.IsNullOrEmpty(blogRoot) || blogRoot.Length <= appRoot.Length ||
                !(context.Request.PathBase + context.Request.Path).StartsWithSegments(blogRoot, System.StringComparison.OrdinalIgnoreCase, out var remaining))
            {
                await next(context);
                return;
            }

            var originalPathBase = context.Request.PathBase;
            var originalPath = context.Request.Path;
            context.Request.PathBase = new PathString(blogRoot);
            context.Request.Path = remaining.HasValue ? remaining : new PathString("/");
            try
            {
                await next(context);
            }
            finally
            {
                context.Request.PathBase = originalPathBase;
                context.Request.Path = originalPath;
            }
        }
    }
}
