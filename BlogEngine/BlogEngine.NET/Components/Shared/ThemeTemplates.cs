using BlogEngine.Core;
using BlogEngine.Core.Web.Controls;
using Microsoft.AspNetCore.Components;

namespace BlogEngine.NET.Components.Shared
{
    // Base classes of the theme templates. They replace the WebForms user controls of the themes
    // (PostView.ascx, CommentView.ascx, ...) which inherited the view base classes of BlogEngine.Core.

    /// <summary>
    /// Base of PostView templates (Custom/Themes/{theme}/PostView.ascx).
    /// </summary>
    public abstract class PostViewTemplate : ComponentBase
    {
        [Parameter, EditorRequired] public PostViewBase View { get; set; }

        protected Post Post => View.Post;

        protected bool SinglePost => View.Location == ServingLocation.SinglePost;

        /// <summary>Post body (or excerpt) after the Serving extensions ran.</summary>
        protected string Body { get; private set; } = string.Empty;

        protected string AdminLinks { get; private set; } = string.Empty;

        protected string AuthorUrl => Utils.AbsoluteWebRoot + "author/" + Utils.RemoveIllegalCharacters(Post.Author + BlogConfig.FileExtension);

        protected string AuthorName => Post.AuthorProfile != null
            ? Utils.RemoveIllegalCharacters(Post.AuthorProfile.DisplayName)
            : Utils.RemoveIllegalCharacters(Post.Author);

        protected override void OnParametersSet()
        {
            Body = View.Body;
            AdminLinks = View.AdminLinks;
        }
    }

    /// <summary>
    /// Base of CommentView templates. Nested comments are passed as child content.
    /// </summary>
    public abstract class CommentViewTemplate : ComponentBase
    {
        [Parameter, EditorRequired] public CommentViewBase View { get; set; }

        /// <summary>Nested replies (the phSubComments placeholder of the original templates).</summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected Comment Comment => View.Comment;

        protected Post Post => View.Post;

        protected string Text { get; private set; } = string.Empty;

        protected string AdminLinks { get; private set; } = string.Empty;

        protected string ReplyToLink { get; private set; } = string.Empty;

        protected bool HideReplies => Comment.Comments.Count == 0 || Comment.Email == "pingback" || Comment.Email == "trackback";

        protected override void OnParametersSet()
        {
            Text = View.Text;
            AdminLinks = View.AdminLinks;
            ReplyToLink = View.ReplyToLink;
        }
    }

    /// <summary>
    /// Base of CommentForm templates. The fields are posted by the SSR form of <c>CommentList</c>:
    /// txtName, txtEmail, txtWebsite, txtContent and cbNotify.
    /// </summary>
    public abstract class CommentFormTemplate : ComponentBase
    {
        [Parameter, EditorRequired] public CommentFormBase View { get; set; }

        /// <summary>Values to redisplay (e.g. after a failed captcha check or from the comment cookie).</summary>
        [Parameter] public CommentFormValues Values { get; set; } = new();
    }

    /// <summary>
    /// Values of the comment form fields.
    /// </summary>
    public class CommentFormValues
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Content { get; set; }
        public bool Notify { get; set; }
    }

    /// <summary>
    /// Base of PostNavigation templates.
    /// </summary>
    public abstract class PostNavigationTemplate : ComponentBase
    {
        [Parameter, EditorRequired] public PostNavigationBase View { get; set; }
    }

    /// <summary>
    /// Base of RelatedPosts templates.
    /// </summary>
    public abstract class RelatedPostsTemplate : ComponentBase
    {
        [Parameter, EditorRequired] public RelatedPostsBase View { get; set; }

        protected List<RelatedPost> RelatedPostList { get; private set; } = new();

        protected override void OnParametersSet()
        {
            RelatedPostList = View.RelatedPostList;
        }
    }
}
