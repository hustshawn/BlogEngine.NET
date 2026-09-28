namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    using BlogEngine.Core.Providers;

    /// <summary>
    /// The ImageHandler serves all images that are uploaded from the admin pages.
    /// </summary>
    public static class ImageEndpoint
    {
        #region Events

        /// <summary>
        ///     Occurs when the requested file does not exist.
        /// </summary>
        public static event EventHandler<EventArgs> BadRequest;

        /// <summary>
        ///     Occurs when a file is served.
        /// </summary>
        public static event EventHandler<EventArgs> Served;

        /// <summary>
        ///     Occurs before the requested image is served.
        /// </summary>
        public static event EventHandler<EventArgs> Serving;

        #endregion

        /// <summary>
        /// Maps the image handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapImageHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/image.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var picture = context.Request.Query["picture"].ToString();
            if (!string.IsNullOrEmpty(picture))
            {
                var fileName = picture;
                OnServing(fileName);
                try
                {
                    fileName = !fileName.StartsWith("/") ? $"/{fileName}" : fileName;

                    // prevent directory traversal
                    if (fileName.Contains(".."))
                    {
                        OnBadRequest(fileName);
                        context.Response.Redirect($"{Utils.AbsoluteWebRoot}error404.aspx");
                        return;
                    }

                    var file = BlogService.GetFile($"{Blog.CurrentInstance.StorageLocation}files{fileName}");
                    if (file != null)
                    {
                        context.Response.Headers.CacheControl = "public";
                        context.Response.Headers.Expires = DateTime.Now.AddYears(1).ToString("R");

                        if (Utils.SetConditionalGetHeaders(file.DateCreated.ToUniversalTime()))
                            return;

                        var index = fileName.LastIndexOf(".") + 1;
                        var extension = fileName.Substring(index).ToUpperInvariant();
                        context.Response.ContentType = string.Compare(extension, "JPG") == 0 ? "image/jpeg" : $"image/{extension}";
                        await context.Response.Body.WriteAsync(file.FileContents);

                        OnServed(fileName);
                    }
                    else
                    {
                        OnBadRequest(fileName);
                        context.Response.Redirect($"{Utils.AbsoluteWebRoot}error404.aspx");
                    }
                }
                catch (Exception ex)
                {
                    OnBadRequest(ex.Message);
                    context.Response.Redirect($"{Utils.AbsoluteWebRoot}error404.aspx");
                }
            }
        }

        private static void OnBadRequest(string file)
        {
            BadRequest?.Invoke(file, EventArgs.Empty);
        }

        private static void OnServed(string file)
        {
            Served?.Invoke(file, EventArgs.Empty);
        }

        private static void OnServing(string file)
        {
            Serving?.Invoke(file, EventArgs.Empty);
        }
    }
}
