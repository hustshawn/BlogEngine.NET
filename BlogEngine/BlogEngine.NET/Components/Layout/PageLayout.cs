namespace BlogEngine.NET.Components.Layout
{
    /// <summary>
    /// Layout for pages that used the theme's page.master in the original
    /// (archive, contact, page, search, errors). Renders the same theme master as <see cref="MainLayout"/>.
    /// </summary>
    public class PageLayout : MainLayout
    {
        protected override bool IsPageLayout => true;
    }
}
