using BlogEngine.Core;
using BlogEngine.Core.Data.Services;
using System.Collections.Concurrent;

namespace BlogEngine.NET.Components.Shared
{
    /// <summary>
    /// Resolves the active theme and its Blazor templates.
    /// Replaces the WebForms lookups of Custom/Themes/{theme}/*.master|*.ascx with fall back
    /// to Custom/Controls/Defaults (or the Standard theme for master pages).
    /// Theme templates live in Components/Shared/Themes/{ThemeFolderWithoutDashes}/{Template}.razor.
    /// </summary>
    public static class Theme
    {
        public const string DefaultTheme = "Standard";

        private const string ThemesNamespace = "BlogEngine.NET.Components.Shared.Themes";
        private const string DefaultsNamespace = "BlogEngine.NET.Components.Shared.Defaults";

        private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Theme folder name for the current request (query string / cookie override, razor host adjustment).
        /// </summary>
        public static string Current
        {
            get
            {
                var theme = BlogSettings.Instance.GetThemeWithAdjustments(null);
                return string.IsNullOrWhiteSpace(theme) || FindType(theme, "SiteMaster") == null ? DefaultTheme : theme;
            }
        }

        /// <summary>
        /// Relative URL of the current theme folder, e.g. /Custom/Themes/Standard/
        /// </summary>
        public static string Url => $"{Utils.ApplicationRelativeWebRoot}Custom/Themes/{Current}/";

        /// <summary>
        /// Template for the given name from the current theme, or the default template.
        /// </summary>
        public static Type Template(string name)
        {
            return FindType(Current, name) ?? FindType(null, name);
        }

        /// <summary>
        /// Master page (layout) for the current theme. The component receives the page
        /// body and whether page.master (archive, contact, page, search, errors) was requested.
        /// </summary>
        public static Type Master => FindType(Current, "SiteMaster") ?? FindType(DefaultTheme, "SiteMaster");

        /// <summary>
        /// Resolves a [CUSTOMFIELD|THEME|{theme}|{key}|{default}/] value.
        /// </summary>
        public static string Field(string key, string defaultValue, string theme = null)
        {
            try
            {
                return CustomFieldsParser.GetPageHtml($"[CUSTOMFIELD|THEME|{theme ?? DefaultTheme}|{key}|{defaultValue}/]");
            }
            catch (Exception ex)
            {
                Utils.Log("Theme.Field", ex);
                return defaultValue;
            }
        }

        private static Type FindType(string theme, string name)
        {
            var ns = theme == null ? DefaultsNamespace : $"{ThemesNamespace}.{theme.Replace("-", string.Empty).Replace(" ", string.Empty)}";
            var fullName = $"{ns}.{name}";
            return TypeCache.GetOrAdd(fullName, n => typeof(Theme).Assembly.GetType(n, false, true));
        }
    }
}
