using BlogEngine.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Helper class for Razor theme rendering.
/// In the original WebForms implementation, this used BuildManager.GetCompiledType and
/// System.Web.WebPages to dynamically compile and render .cshtml files.
/// In modern .NET, themed content is handled through Blazor components in 
/// Components/Shared/Themes/{ThemeName}/ instead.
/// These constants and helper methods are preserved for backward compatibility
/// with any code that references them.
/// </summary>
public static class RazorHelpers
{
    public const string PAGE_BODY_MARKER = "-|-|-|-|-|-|- PAGE-BODY -|-|-|-|-|-|-";
    public const string NESTED_COMMENTS_MARKER = "-|-|-|-|-|-|- NESTED-COMMENTS -|-|-|-|-|-|-";
    public const string WIDGET_CONTAINER_BODY_MARKER = "-|-|-|-|-|-|- WIDGET-CONTAINER-BODY -|-|-|-|-|-|-";
    public const string RAZOR_HOST_PAGE_VPATH = "~/Custom/Themes/RazorHost/page.cshtml";

    /// <summary>
    /// Attempts to parse a Razor (.cshtml) template file.
    /// In modern .NET, this is a no-op as themed rendering is handled via Blazor components.
    /// Returns null to indicate no dynamic compilation is available.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the .cshtml file.</param>
    /// <param name="model">The model object to pass to the template.</param>
    /// <returns>The rendered HTML string, or null if rendering is not available.</returns>
    public static string ParseRazor(string virtualPath, object model)
    {
        // REVIEW: In modern .NET, Razor pages are not dynamically compiled via BuildManager.
        // The RazorHost theme should use Blazor components for rendering.
        // This method returns null to signal that the caller should use fallback rendering.
        Utils.Log($"RazorHelpers.ParseRazor called for {virtualPath} - dynamic Razor compilation not available in ASP.NET Core. Use Blazor components instead.");
        return null;
    }
}
