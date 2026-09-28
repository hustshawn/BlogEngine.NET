namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    using BlogEngine.Core.Providers;

    /// <summary>
    /// The FileHandler serves all files that are uploaded from the admin pages except for images.
    /// </summary>
    public static class FileEndpoint
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
        ///     Occurs when the requested file does not exist.
        /// </summary>
        public static event EventHandler<EventArgs> Serving;

        #endregion

        /// <summary>
        /// Maps the file handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapFileHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/file.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            var fileQuery = context.Request.Query["file"].ToString();
            if (!string.IsNullOrEmpty(fileQuery))
            {
                var fileName = fileQuery;

                // prevent directory traversal
                if (fileName.Contains(".."))
                {
                    OnBadRequest(fileName);
                    context.Response.Redirect($"{Utils.AbsoluteWebRoot}error404.aspx");
                    return;
                }

                OnServing(fileName);
                fileName = !fileName.StartsWith("/") ? $"/{fileName}" : fileName;
                try
                {
                    var file = BlogService.GetFile($"{Blog.CurrentInstance.StorageLocation}files{fileName}");
                    if (file != null)
                    {
                        context.Response.Headers.Append("Content-Disposition", $"inline; filename=\"{file.Name}\"");
                        SetContentType(context, file.Name);
                        if (Utils.SetConditionalGetHeaders(file.DateCreated.ToUniversalTime()))
                            return;

                        await context.Response.Body.WriteAsync(file.FileContents);
                        OnServed(fileName);
                    }
                    else
                    {
                        OnBadRequest(fileName);
                        context.Response.Redirect($"{Utils.AbsoluteWebRoot}error404.aspx");
                    }
                }
                catch (Exception)
                {
                    OnBadRequest(fileName);
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

        /// <summary>
        /// Sets the content type depending on the filename's extension.
        /// </summary>
        private static void SetContentType(HttpContext context, string fileName)
        {
            context.Response.Headers.Append(
                "Content-Type", fileName.EndsWith(".pdf") ? "application/pdf" : "application/octet-stream");
        }
    }
}
