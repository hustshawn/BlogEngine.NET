namespace BlogEngine.Core.Web.HttpHandlers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Receives and records all ratings coming in from the rating control.
    /// </summary>
    public static class RatingEndpoint
    {
        /// <summary>
        /// Maps the rating handler endpoint.
        /// </summary>
        public static IEndpointRouteBuilder MapRatingHandler(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/rate.axd", HandleAsync);
            return endpoints;
        }

        private static async Task HandleAsync(HttpContext context)
        {
            if (!BlogSettings.Instance.EnableRating || !Security.IsAuthorizedTo(Rights.SubmitRatingsOnPosts))
            {
                throw new System.Security.SecurityException();
            }
            else
            {
                var rating = context.Request.Query["rating"].ToString();
                int rate;
                if (!string.IsNullOrEmpty(rating) && int.TryParse(rating, out rate))
                {
                    var id = context.Request.Query["id"].ToString();
                    if (!string.IsNullOrEmpty(id) && id.Length == 36 && rate > 0 && rate < 6)
                    {
                        try
                        {
                            if (HasRated(id, context))
                            {
                                await context.Response.WriteAsync($"{rate}HASRATED");
                                return;
                            }
                            else
                            {
                                var post = Post.GetPost(new Guid(id));
                                post.Rate(rate);

                                SetCookie(id, context);
                                await context.Response.WriteAsync($"{rate}OK");
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            // Something failed.
                        }
                    }
                }
                await context.Response.WriteAsync("FAIL");
            }
        }

        /// <summary>
        /// Determines whether the specified post id has been rated.
        /// </summary>
        private static bool HasRated(string postId, HttpContext context)
        {
            var ratingCookie = context.Request.Cookies["rating"];

            if (ratingCookie != null)
            {
                return ratingCookie.Contains(postId);
            }

            return false;
        }

        /// <summary>
        /// Sets the cookie.
        /// </summary>
        private static void SetCookie(string id, HttpContext context)
        {
            var existingValue = context.Request.Cookies["rating"] ?? string.Empty;

            context.Response.Cookies.Append("rating", existingValue + id, new CookieOptions
            {
                Expires = DateTime.Now.AddYears(2)
            });
        }
    }
}
