using BlogEngine.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlogEngine.NET.AppCode.Admin
{
    /// <summary>
    /// Base model for the admin shell pages (ported from the WebPages admin/*.cshtml).
    /// Like the original layout, every admin page demands <see cref="Rights.AccessAdminPages"/>
    /// and sends unauthorized users to the login page.
    /// </summary>
    public class AdminPageModel : PageModel
    {
        /// <summary>
        /// Rights the current user must hold (all of them) to view the page.
        /// </summary>
        protected virtual Rights[] RequiredRights => [Rights.AccessAdminPages];

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            if (!Security.IsAuthorizedTo(AuthorizationCheck.HasAll, RequiredRights))
            {
                // Core sets the redirect (login page with ReturnURL, or home when coming from login)
                Security.RedirectForUnauthorizedRequest();
                context.Result = new EmptyResult();
            }
        }
    }

    /// <summary>
    /// Post and page editor shells: admin access plus the right to edit own posts.
    /// </summary>
    public class EditorPageModel : AdminPageModel
    {
        protected override Rights[] RequiredRights => [Rights.AccessAdminPages, Rights.EditOwnPosts];
    }

    /// <summary>
    /// Known values of <see cref="BlogConfig.DefaultEditor"/> (the editor partial rendered by the post/page editors).
    /// </summary>
    public static class AdminEditors
    {
        public const string TinyMce = "~/admin/editors/tinymce/editor.cshtml";
        public const string Summernote = "~/admin/editors/summernote/editor.cshtml";
        public const string BootstrapWysiwyg = "~/admin/editors/bootstrap-wysiwyg/editor.cshtml";
    }

    /// <summary>
    /// The SPA shell at /admin/. Its templates use relative URLs, so like IIS
    /// (directory default document) "/admin" is redirected to "/admin/" ("/admin/index.cshtml" works as is).
    /// </summary>
    public class AdminIndexModel : AdminPageModel
    {
        public IActionResult OnGet()
        {
            if (string.Equals(Request.Path.Value, "/admin", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(Request.PathBase + Request.Path + "/" + Request.QueryString);
            }

            return Page();
        }
    }

    /// <summary>
    /// File manager dialog used by the editors (no admin layout): only needs the right to edit own posts.
    /// </summary>
    public class FileManagerPageModel : AdminPageModel
    {
        protected override Rights[] RequiredRights => [Rights.EditOwnPosts];
    }
}
