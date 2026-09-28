using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace BlogEngine.Core.Services.Security
{
    /// <summary>
    /// Gives each child blog its own authentication cookie ("{name}-{blogId}"), as the original
    /// per-blog forms authentication cookie did, unless BlogEngine.SingleSignOn is enabled.
    /// Without it, signing in to a child blog would replace the primary blog's session.
    /// </summary>
    public sealed class BlogInstanceCookieManager : ICookieManager
    {
        private readonly ChunkingCookieManager _inner = new ChunkingCookieManager();

        public string GetRequestCookie(HttpContext context, string key) =>
            _inner.GetRequestCookie(context, GetName(key));

        public void AppendResponseCookie(HttpContext context, string key, string value, CookieOptions options) =>
            _inner.AppendResponseCookie(context, GetName(key), value, options);

        public void DeleteCookie(HttpContext context, string key, CookieOptions options) =>
            _inner.DeleteCookie(context, GetName(key), options);

        private static string GetName(string key)
        {
            var blog = Blog.CurrentInstance;
            return BlogConfig.SingleSignOn || blog == null || blog.IsPrimary ? key : $"{key}-{blog.Id}";
        }
    }
}
