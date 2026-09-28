using BlogEngine.Core.Services.Packaging;
﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BlogEngine.Core.Providers;
using BlogEngine.Core.Data.Services;

namespace BlogEngine.Core.Packaging
{
    /// <summary>
    /// Responsible for install/uninstall operations.
    /// Replaces the old NuGet.Core-based PackageManager with direct HTTP download + file extraction.
    /// </summary>
    public static class Installer
    {
        private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        });

        /// <summary>
        /// Install package from gallery by downloading the .nupkg and extracting it.
        /// </summary>
        /// <param name="pkgId">Package ID to install.</param>
        public static bool InstallPackage(string pkgId)
        {
            try
            {
                // if package already installed - uninstall it
                if (BlogService.InstalledFromGalleryPackages() != null)
                {
                    if (BlogService.InstalledFromGalleryPackages().Find(p => p.PackageId == pkgId) != null)
                    {
                        UninstallPackage(pkgId);
                    }
                }

                // Download and extract the package
                var packagesPath = BlogConfig.MapPath(Utils.ApplicationRelativeWebRoot + "App_Data/packages");
                if (!Directory.Exists(packagesPath))
                    Directory.CreateDirectory(packagesPath);

                var packageUrl = $"{BlogConfig.GalleryFeedUrl.TrimEnd('/')}/package/{pkgId}";
                var nupkgPath = Path.Combine(packagesPath, $"{pkgId}.nupkg");

                var response = _httpClient.GetAsync(packageUrl).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    using var fs = System.IO.File.Create(nupkgPath);
                    response.Content.ReadAsStream().CopyTo(fs);
                }

                // Extract the package using SharpZipLib
                var pkgDir = Path.Combine(packagesPath, pkgId);
                if (Directory.Exists(pkgDir))
                    Directory.Delete(pkgDir, true);

                ICSharpCode.SharpZipLib.Zip.FastZip fastZip = new();
                fastZip.ExtractZip(nupkgPath, pkgDir, null);

                var iPkg = new InstalledPackage { PackageId = pkgId, Version = "1.0.0" };
                BlogService.InsertPackage(iPkg);

                var packageFiles = FileSystem.InstallPackage(new NuGetPackageInfo { Id = pkgId, Version = new Version("1.0.0") });
                BlogService.InsertPackageFiles(packageFiles);

                Blog.CurrentInstance.Cache.Remove(Constants.CacheKey);
                CustomFieldsParser.ClearCache();

                Utils.Log($"Installed package {pkgId} by {Security.CurrentUser.Identity.Name}");
            }
            catch (Exception ex)
            {
                Utils.Log("BlogEngine.Core.Packaging.Installer.InstallPackage(" + pkgId + ")", ex);
                UninstallPackage(pkgId);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Uninstall package
        /// </summary>
        /// <param name="pkgId">Package ID to uninstall.</param>
        /// <returns>True if successful.</returns>
        public static bool UninstallPackage(string pkgId)
        {
            try
            {
                FileSystem.UninstallPackage(pkgId);

                // remove packagefiles.xml and packages.xml (or DB records)
                BlogService.DeletePackage(pkgId);

                // Remove the extracted package files
                var packagesPath = BlogConfig.MapPath(Utils.ApplicationRelativeWebRoot + "App_Data/packages");
                var pkgDir = Path.Combine(packagesPath, pkgId);
                if (Directory.Exists(pkgDir))
                    Directory.Delete(pkgDir, true);

                var nupkgPath = Path.Combine(packagesPath, $"{pkgId}.nupkg");
                if (System.IO.File.Exists(nupkgPath))
                    System.IO.File.Delete(nupkgPath);

                // reset cache
                Blog.CurrentInstance.Cache.Remove(Constants.CacheKey);

                Utils.Log($"Uninstalled package {pkgId} by {Security.CurrentUser.Identity.Name}");
            }
            catch (Exception ex)
            {
                Utils.Log($"Error uninstalling package {pkgId}: {ex.Message}");
                throw;
            }

            return true;
        }
    }
}
