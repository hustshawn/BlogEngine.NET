using BlogEngine.Core.Web.Extensions;

namespace App_Code.Extensions
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using System.Collections.Generic;
    using BlogEngine.Core;
    using BlogEngine.Core.Web.Controls;

    /// <summary>
    /// Subscribes to Log events and records the events in a file.
    /// </summary>
    [Extension("Subscribes to Log events and records the events in a file.", "1.0", "BlogEngine.NET", 0, false)]
    public class Logger
    {
        #region Constants and Fields

        private const string BaseFilename = "logger.txt";

        private const int MaxLogSizeMb = 25;

        /// <summary>
        /// The sync root.
        /// </summary>
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// The file name.
        /// </summary>
        private static Dictionary<Guid, string> blogsfileName = new Dictionary<Guid, string>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes static members of the <see cref="Logger"/> class.
        /// </summary>
        static Logger()
        {
            Utils.OnLog += OnLog;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns>The file name.</returns>
        private static string Filename
        {
            get
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;

                // If Blog.CurrentInstance is not available, use default storage location
                try
                {
                    Guid blogId = Blog.CurrentInstance.Id;

                    if (!blogsfileName.ContainsKey(blogId))
                    {
                        lock (SyncRoot)
                        {
                            if (!blogsfileName.ContainsKey(blogId))
                            {
                                var storagePath = Blog.CurrentInstance.StorageLocation;
                                if (storagePath.StartsWith("~/"))
                                    storagePath = storagePath.Substring(2);
                                blogsfileName[blogId] = Path.Combine(basePath, storagePath, BaseFilename);
                            }
                        }
                    }

                    return blogsfileName[blogId];
                }
                catch
                {
                    var storagePath = BlogConfig.StorageLocation;
                    if (storagePath.StartsWith("~/"))
                        storagePath = storagePath.Substring(2);
                    return Path.Combine(basePath, storagePath, BaseFilename);
                }
            }
        }

        /// <summary>
        /// The event handler that is triggered every time there is a log notification.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private static void OnLog(object sender, EventArgs e)
        {
            if (sender == null || !(sender is string))
                return;

            if (!ExtensionManager.ExtensionEnabled("Logger"))
                return;

            var logMsg = (string)sender;

            if (string.IsNullOrEmpty(logMsg))
                return;

            var file = Filename;

            lock (SyncRoot)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);

                    // 1048576 is the number of bytes in a megabyte.
                    if (fi != null && fi.Exists && fi.Length > (1048576 * MaxLogSizeMb))
                    {
                        fi.Delete();
                    }

                    using (var fs = new FileStream(file, FileMode.Append))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(@"*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*");
                        sw.WriteLine("{0}: {1}", "Date", DateTime.Now);
                        sw.WriteLine(logMsg);
                    }
                }
                catch
                {
                    // Absorb the error.
                }
            }
        }

        #endregion
    }
}