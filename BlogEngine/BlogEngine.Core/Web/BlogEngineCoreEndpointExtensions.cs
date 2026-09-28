namespace BlogEngine.Core.Web
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;

    using BlogEngine.Core.API.MetaWeblog;
    using BlogEngine.Core.Web.HttpHandlers;

    /// <summary>
    /// Extension methods for registering all BlogEngine.Core endpoints with ASP.NET Core routing.
    /// </summary>
    public static class BlogEngineCoreEndpointExtensions
    {
        /// <summary>
        /// Maps all BlogEngine.Core handler endpoints (syndication, images, files, etc.)
        /// to the ASP.NET Core endpoint routing pipeline.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapBlogEngineEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Feed/syndication endpoints
            endpoints.MapSyndication();

            // File serving endpoints
            endpoints.MapImageHandler();
            endpoints.MapFileHandler();
            endpoints.MapJavaScriptHandler();

            // Export endpoints
            endpoints.MapBlogMLExport();

            // Discovery/metadata endpoints
            endpoints.MapOpmlHandler();
            endpoints.MapOpenSearchHandler();
            endpoints.MapRsdHandler();
            endpoints.MapSiteMapHandler();

            // Semantic web endpoints
            endpoints.MapApmlHandler();
            endpoints.MapFoafHandler();
            endpoints.MapSiocHandler();

            // Resource serving endpoints
            endpoints.MapResourceHandler();

            // Rating endpoint
            endpoints.MapRatingHandler();

            // Trackback/Pingback endpoints
            endpoints.MapPingbackHandler();
            endpoints.MapTrackbackHandler();

            // MetaWeblog XML-RPC API endpoint
            endpoints.MapMetaWeblogHandler();

            return endpoints;
        }
    }
}
