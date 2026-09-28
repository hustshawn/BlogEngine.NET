using BlogEngine.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlogEngine.NET.Components.Widgets
{
    /// <summary>
    /// Isolates a failing widget (logs the exception and renders the error content),
    /// like the try/catch around each widget in the original WidgetZone.
    /// </summary>
    public class WidgetErrorBoundary : ErrorBoundaryBase
    {
        [Parameter] public string WidgetName { get; set; }

        protected override Task OnErrorAsync(Exception exception)
        {
            Utils.Log($"WidgetZone: widget {WidgetName}", exception);
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, CurrentException == null ? ChildContent : ErrorContent?.Invoke(CurrentException));
        }
    }
}
