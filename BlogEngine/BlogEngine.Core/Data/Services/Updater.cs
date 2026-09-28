using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Net.Http;

namespace BlogEngine.Core.Data.Services
{
    /// <summary>
    /// Used by auto-upgrade to update to latest version.
    /// Converted from WebClient/WebRequest to static HttpClient.
    /// </summary>
    public class Updater
    {
        private static string _upgradeReleases = BlogConfig.GalleryFeedUrl.Replace("nuget", "/Releases/");
        private string _versionsTxt = _upgradeReleases + "versions.txt";
        private string _setupTxt = _upgradeReleases + "setup.txt";
        private string _latestSetup = _upgradeReleases + "setup.zip";
        private string _root;

        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="Updater"/> class.
        /// </summary>
        /// <param name="contentRootPath">The application content root path (replaces BlogConfig.MapPath).</param>
        public Updater(string contentRootPath)
        {
            _root = contentRootPath ?? string.Empty;
        }

        /// <summary>
        /// Check if newer version exists
        /// </summary>
        /// <param name="version">Current BE version</param>
        /// <returns>Latest BE version</returns>
        public string Check(string version)
        {
            try
            {
                if (_root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    _root = _root.Substring(0, _root.Length - 1);

                var content = _httpClient.GetStringAsync(_versionsTxt).GetAwaiter().GetResult();
                using var reader = new StringReader(content);
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(version) && line.Contains("|"))
                    {
                        var iCurrent = int.Parse(version.Replace(".", ""));
                        var iFrom = int.Parse(line.Substring(0, line.IndexOf("|")).Replace(".", ""));
                        var iTo = int.Parse(line.Substring(line.LastIndexOf("|") + 1).Replace(".", ""));

                        if (iCurrent >= iFrom && iCurrent < iTo)
                        {
                            UpdateSetupFolder();
                            return line.Substring(line.LastIndexOf("|") + 1);
                        }
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        void UpdateSetupFolder()
        {
            var localVersionPath = Path.Combine(_root, "setup", "upgrade", "setup.txt");

            if (!File.Exists(localVersionPath))
                return;

            try
            {
                var localVersion = File.ReadAllText(localVersionPath);

                var remoteVersion = _httpClient.GetStringAsync(_setupTxt).GetAwaiter().GetResult();

                if (localVersion != remoteVersion)
                {
                    // local setup files differ from latest
                    UpdateSetupFiles();
                }
            }
            catch (Exception) { }
        }

        void UpdateSetupFiles()
        {
            try
            {
                var setupFolder = Path.Combine(_root, "setup", "upgrade");

                if (Directory.Exists(setupFolder))
                    Directory.Delete(setupFolder, true);

                Directory.CreateDirectory(setupFolder);

                using var responseStream = _httpClient.GetStreamAsync(_latestSetup).GetAwaiter().GetResult();
                ZipInputStream myZipInputStream = new ZipInputStream(responseStream);
                ZipEntry myZipEntry;

                while ((myZipEntry = myZipInputStream.GetNextEntry()) != null)
                {
                    if (myZipEntry.IsDirectory)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(setupFolder, myZipEntry.Name)));
                    }
                    else
                    {
                        // Ensure non-empty file name
                        if (myZipEntry.Name.Length > 0)
                        {
                            // Create a new file, get the file stream
                            FileStream myFileStream = File.Create(Path.Combine(setupFolder, myZipEntry.Name));
                            int size = 2048;
                            byte[] data = new byte[2048];

                            // Write out the file
                            while (true)
                            {
                                size = myZipInputStream.Read(data, 0, data.Length);

                                if (size > 0)
                                {
                                    myFileStream.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            myFileStream.Close();
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
