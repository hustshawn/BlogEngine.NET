using BlogEngine.Core;
using System;
using System.Globalization;
using System.Threading;

namespace BlogEngine.NET.App_Start
{
    /// <summary>
    /// BlogEngine configuration helpers.
    /// Initialize and SetCulture are now called from middleware in Program.cs.
    /// RegisterBundles is removed — static files are served directly from wwwroot.
    /// RegisterWebApi is removed — controllers use attribute routing [Route("api/[controller]")].
    /// RegisterDiContainer is removed — DI is configured in Program.cs with built-in container.
    /// </summary>
    public static class BlogEngineConfig
    {
        private static bool _initializedAlready = false;
        private static readonly object _SyncRoot = new object();

        /// <summary>
        /// One-time initialization: loads extensions.
        /// Called from middleware in Program.cs (ported from Application_BeginRequest).
        /// </summary>
        public static void Initialize()
        {
            if (_initializedAlready) { return; }

            lock (_SyncRoot)
            {
                if (_initializedAlready) { return; }

                Utils.LoadExtensions();

                _initializedAlready = true;
            }
        }

        /// <summary>
        /// Sets the current thread culture based on blog settings.
        /// Called from middleware in Program.cs (ported from Application_PreRequestHandlerExecute).
        /// </summary>
        public static void SetCulture()
        {
            var culture = BlogSettings.Instance.Culture;
            if (!string.IsNullOrEmpty(culture) && !culture.Equals("Auto"))
            {
                CultureInfo defaultCulture = Utils.GetDefaultCulture();
                Thread.CurrentThread.CurrentUICulture = defaultCulture;
                Thread.CurrentThread.CurrentCulture = defaultCulture;
            }
        }
    }
}
