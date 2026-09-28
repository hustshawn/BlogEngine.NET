namespace BlogEngine.Core.Web
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
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
        public static IEndpointRouteBuilder MapBlogEngineEndpoints(this IEndpointRouteBuilder routes)
        {
            // The handlers were IHttpHandlers writing synchronously (XmlWriter, StreamWriter) to the response
            var endpoints = routes.MapGroup(string.Empty);
            ((IEndpointConventionBuilder)endpoints).Add(builder =>
            {
                var handler = builder.RequestDelegate;
                if (handler == null)
                {
                    return;
                }

                builder.RequestDelegate = context =>
                {
                    var bodyControl = context.Features.Get<IHttpBodyControlFeature>();
                    if (bodyControl != null)
                    {
                        bodyControl.AllowSynchronousIO = true;
                    }

                    return handler(context);
                };
            });

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

            return routes;
        }
    }
}
