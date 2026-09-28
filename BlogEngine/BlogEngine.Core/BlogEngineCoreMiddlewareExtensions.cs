using BlogEngine.Core.Web.HttpModules;
using Microsoft.AspNetCore.Builder;

namespace BlogEngine.Core
{
    /// <summary>
    /// Extension methods for registering BlogEngine Core middleware in the ASP.NET Core pipeline.
    /// </summary>
    public static class BlogEngineCoreMiddlewareExtensions
    {
        /// <summary>
        /// Adds BlogEngine middleware to the ASP.NET Core request pipeline.
        /// Registers URL rewriting, www subdomain handling, and referrer tracking middleware.
        /// For response compression, use the built-in app.UseResponseCompression() instead.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseBlogEngineMiddleware(this IApplicationBuilder app)
        {
            // WwwSubDomainMiddleware should run first (it was first in original web.config module order)
            app.UseMiddleware<WwwSubDomainMiddleware>();

            // URL rewriting
            app.UseMiddleware<UrlRewriteMiddleware>();

            // Referrer tracking (runs on EndRequest equivalent - after the response)
            app.UseMiddleware<ReferrerMiddleware>();

            // CompressionModule → use built-in app.UseResponseCompression() instead
            // The CompressionMiddleware class is available for conditional compression logic,
            // but ASP.NET Core's built-in ResponseCompression middleware is preferred.

            return app;
        }
    }
}
