using BlogEngine.NET.Custom.Widgets;
using Microsoft.AspNetCore.Components;
using System.Collections.Specialized;

namespace BlogEngine.NET.Components.Widgets
{
    /// <summary>
    /// Base class of the widget components (ports of Custom/Widgets/{Name}/widget.cshtml).
    /// A widget "Foo" in a widget zone is rendered by the component <c>FooWidget</c> of this namespace.
    /// The parameters mirror the anonymous model the original WidgetZone passed to the Razor template.
    /// </summary>
    public abstract class WidgetComponentBase : ComponentBase
    {
        /// <summary>Widget instance id (key of its settings in App_Data/datastore/widgets).</summary>
        [Parameter] public string Id { get; set; }

        /// <summary>Widget name (folder in Custom/Widgets).</summary>
        [Parameter] public string Name { get; set; }

        /// <summary>Title configured for this widget instance.</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>Settings stored for this widget instance.</summary>
        protected StringDictionary Settings => Common.GetSettings(Id);

        /// <summary>
        /// Resolves the component type for a widget name, or null when the widget has no Blazor port.
        /// </summary>
        public static Type Resolve(string widgetName)
        {
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return null;
            }

            var typeName = $"{typeof(WidgetComponentBase).Namespace}.{widgetName.Replace(" ", string.Empty)}Widget";
            var type = typeof(WidgetComponentBase).Assembly.GetType(typeName, false, true);
            return type != null && typeof(WidgetComponentBase).IsAssignableFrom(type) ? type : null;
        }
    }
}
