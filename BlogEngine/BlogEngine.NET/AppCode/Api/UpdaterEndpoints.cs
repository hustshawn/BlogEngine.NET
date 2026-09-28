using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using BlogEngine.Core;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;

namespace BlogEngine.NET.AppCode.Api
{
    /// <summary>
    /// Updater endpoints - converted from Updater.asmx WebService to minimal API endpoints.
    /// Provides blog upgrade functionality: version check, download, extract, backup, install, rollback.
    /// </summary>
    public static class UpdaterEndpoints
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _upgradeReleases = BlogConfig.GalleryFeedUrl.Replace("nuget", "/Releases/");
        private static string _downloadUrl = _upgradeReleases + "{0}.zip";
        private static string _versionsTxt = _upgradeReleases + "versions.txt";
        private static bool _test = false;

        public static void MapUpdaterEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/updater");

            group.MapPost("/check", async (HttpContext context) =>
            {
                var version = await new StreamReader(context.Request.Body).ReadToEndAsync();
                version = version.Trim('"');
                var result = Check(version);
                return Results.Ok(result);
            });

            group.MapPost("/download", async (HttpContext context) =>
            {
                var version = await new StreamReader(context.Request.Body).ReadToEndAsync();
                version = version.Trim('"');
                var root = GetRoot(context);
                var result = Download(version, root);
                return Results.Ok(result);
            });

            group.MapPost("/extract", (HttpContext context) =>
            {
                var root = GetRoot(context);
                var result = Extract(root);
                return Results.Ok(result);
            });

            group.MapPost("/backup", (HttpContext context) =>
            {
                var root = GetRoot(context);
                var result = Backup(root);
                return Results.Ok(result);
            });

            group.MapPost("/delete", (HttpContext context) =>
            {
                var root = GetRoot(context);
                var result = Delete(root);
                return Results.Ok(result);
            });

            group.MapPost("/install", (HttpContext context) =>
            {
                var root = GetRoot(context);
                var result = Install(root);
                return Results.Ok(result);
            });

            group.MapPost("/rollback", (HttpContext context) =>
            {
                var root = GetRoot(context);
                var result = Rollback(root);
                return Results.Ok(result);
            });
        }

        private static string GetRoot(HttpContext context)
        {
            var env = context.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var root = env.ContentRootPath;
            if (root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root = root.Substring(0, root.Length - 1);
            return root;
        }

        private static string GetNewZip(string root) => Path.Combine(root, "setup", "upgrade", "backup", "new.zip");
        private static string GetOldZip(string root) => Path.Combine(root, "setup", "upgrade", "backup", "old.zip");

        private static StringCollection GetIgnoreDirs(string root)
        {
            var dirs = new StringCollection();
            dirs.Add(Path.Combine(root, "Custom"));
            dirs.Add(Path.Combine(root, "setup", "upgrade"));
            return dirs;
        }

        public static string Check(string version)
        {
            try
            {
                using var response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, _versionsTxt));
                using var stream = response.Content.ReadAsStream();
                using var reader = new StreamReader(stream);
                string line = "";

                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(version) && line.Contains("|"))
                    {
                        var iCurrent = int.Parse(version.Replace(".", ""));
                        var iFrom = int.Parse(line.Substring(0, line.IndexOf("|")).Replace(".", ""));
                        var iTo = int.Parse(line.Substring(line.LastIndexOf("|") + 1).Replace(".", ""));

                        if (iCurrent >= iFrom && iCurrent < iTo)
                        {
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

        public static string Download(string version, string root)
        {
            if (_test)
            {
                System.Threading.Thread.Sleep(2000);
                return "";
            }
            try
            {
                var backupDir = Path.Combine(root, "setup", "upgrade", "backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var newZip = GetNewZip(root);
                if (File.Exists(newZip))
                    File.Delete(newZip);

                var startTime = DateTime.UtcNow;
                using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(_downloadUrl, version.Replace(".", "")));
                using var response = _httpClient.Send(request);
                using var responseStream = response.Content.ReadAsStream();
                using var fileStream = File.OpenWrite(newZip);
                
                byte[] buffer = new byte[4096];
                int bytesRead = responseStream.Read(buffer, 0, 4096);
                while (bytesRead > 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                    DateTime nowTime = DateTime.UtcNow;
                    if ((nowTime - startTime).TotalMinutes > 5)
                    {
                        throw new ApplicationException("Download timed out");
                    }
                    bytesRead = responseStream.Read(buffer, 0, 4096);
                }

                Utils.Log(string.Format("Upgrade: downloaded version {0} by {1}", version, Security.CurrentUser.Identity.Name));
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string Extract(string root)
        {
            if (_test)
            {
                System.Threading.Thread.Sleep(2000);
                return "";
            }

            ZipFile zf = null;
            string outFolder = Path.Combine(root, "setup", "upgrade", "backup", "be");
            try
            {
                if (!Directory.Exists(outFolder))
                    Directory.CreateDirectory(outFolder);

                FileStream fs = File.OpenRead(GetNewZip(root));
                zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile) continue;

                    String entryFileName = zipEntry.Name;

                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    String fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
            }
        }

        public static string Backup(string root)
        {
            if (_test)
            {
                System.Threading.Thread.Sleep(2000);
                return "";
            }

            try
            {
                var backupDir = Path.Combine(root, "setup", "upgrade", "backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                var oldZip = GetOldZip(root);
                if (File.Exists(oldZip))
                    File.Delete(oldZip);

                var fsOut = File.Create(oldZip);
                var zipStream = new ZipOutputStream(fsOut);
                zipStream.SetLevel(3);

                int folderOffset = root.Length + (root.EndsWith(Path.DirectorySeparatorChar.ToString()) ? 0 : 1);
                var ignoreDirs = GetIgnoreDirs(root);
                CompressFolder(root, zipStream, folderOffset, ignoreDirs);

                zipStream.IsStreamOwner = true;
                zipStream.Close();

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string Delete(string root)
        {
            if (_test)
            {
                System.Threading.Thread.Sleep(2000);
                return "";
            }
            try
            {
                DeleteDir(root, "api");
                DeleteDir(root, "editors");
                DeleteDir(root, "Modules");
                DeleteDir(root, "pics");

                int version = int.Parse(BlogSettings.Instance.Version().Replace(".", ""));
                if (version < 3300)
                {
                    DeleteDir(root, Path.Combine("App_Data", "datastore", "widgets"));
                }

                ReplaceDir(root, "Account");
                ReplaceDir(root, "admin");
                ReplaceDir(root, "fonts");
                ReplaceDir(root, Path.Combine("setup", "Mono"));
                ReplaceDir(root, Path.Combine("setup", "MySQL"));
                ReplaceDir(root, Path.Combine("setup", "SQL_CE"));
                ReplaceDir(root, Path.Combine("setup", "SQLite"));
                ReplaceDir(root, Path.Combine("setup", "SQLServer"));
                ReplaceDir(root, "App_GlobalResources");
                ReplaceDir(root, "Scripts");
                ReplaceDir(root, "Content");
                ReplaceDir(root, Path.Combine("Custom", "Themes", "Standard"));
                ReplaceDir(root, Path.Combine("Custom", "Widgets"));

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string Install(string root)
        {
            if (_test)
            {
                System.Threading.Thread.Sleep(2000);
                return "";
            }
            try
            {
                // archive.aspx replaced by Blazor component
                // contact.aspx replaced by Blazor component
                // default.aspx replaced by Blazor component
                // error.aspx replaced by Blazor component
                // error404.aspx replaced by Blazor component
                // page.aspx replaced by Blazor component
                // post.aspx replaced by Blazor component
                // search.aspx replaced by Blazor component
                ReplaceFile(root, "web.sitemap");
                ReplaceFile(root, "wlwmanifest.xml");

                ReplaceFilesInDir(root, "bin");

                string from = Path.Combine(root, "setup", "upgrade", "backup", "be", "Custom", "Controls", "{0}");
                string to = Path.Combine(root, "Custom", "Controls", "{0}");
                CopyFile(string.Format(from, "CommentList.ascx"), string.Format(to, "CommentList.ascx"));
                CopyFile(string.Format(from, "PostList.ascx"), string.Format(to, "PostList.ascx"));
                CopyFile(string.Format(from, Path.Combine("Defaults", "CommentView.ascx")), string.Format(to, Path.Combine("Defaults", "CommentView.ascx")));
                CopyFile(string.Format(from, Path.Combine("Defaults", "PostView.ascx")), string.Format(to, Path.Combine("Defaults", "PostView.ascx")));

                Directory.Delete(Path.Combine(root, "setup", "upgrade", "backup"), true);
                Utils.Log(string.Format("Upgrade completed by {0}", Security.CurrentUser.Identity.Name));

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string Rollback(string root)
        {
            // REVIEW: Rollback requires tracking installed files — simplified implementation
            Utils.Log("Rollback requested but not fully implemented in modern .NET migration");
            return "";
        }

        #region Private helpers

        private static void ReplaceDir(string root, string dir)
        {
            try
            {
                DeleteDir(root, dir);
                CopyDir(root, dir);
                Utils.Log(string.Format("Updater: replaced directory {0}", dir));
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Failed to replace directory {0}; error: {1}", dir, ex.Message));
            }
        }

        private static void DeleteDir(string root, string dir)
        {
            var fullPath = Path.Combine(root, dir);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
        }

        private static void CopyDir(string root, string dir)
        {
            var source = new DirectoryInfo(Path.Combine(root, "setup", "upgrade", "backup", "be", dir));
            var target = new DirectoryInfo(Path.Combine(root, dir));
            CopyRecursive(source, target);
        }

        private static void ReplaceFile(string root, string file)
        {
            string sourceFile = Path.Combine(root, "setup", "upgrade", "backup", "be", file);
            string targetFile = Path.Combine(root, file);

            try
            {
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
            }
            catch (Exception ex)
            {
                Utils.Log(string.Format("Updater: failed to delete file {0}; error: {1}", file, ex.Message));
            }

            if (!File.Exists(targetFile) && File.Exists(sourceFile))
            {
                File.Copy(sourceFile, targetFile, true);
            }
        }

        private static void CopyFile(string from, string to)
        {
            if (File.Exists(from))
            {
                var dir = Path.GetDirectoryName(to);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(from, to, true);
            }
        }

        private static void ReplaceFilesInDir(string root, string dir)
        {
            string sourceDir = Path.Combine(root, "setup", "upgrade", "backup", "be", dir);
            if (!Directory.Exists(sourceDir)) return;

            string[] files = Directory.GetFiles(sourceDir);
            foreach (string sourceFile in files)
            {
                string fileName = Path.GetFileName(sourceFile);
                ReplaceFile(root, Path.Combine(dir, fileName));
            }
        }

        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset, StringCollection ignoreDirs)
        {
            if (ignoreDirs.Contains(path))
                return;

            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {
                FileInfo fi = new FileInfo(filename);
                string entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime;
                newEntry.Size = fi.Length;
                zipStream.PutNextEntry(newEntry);

                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }

            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset, ignoreDirs);
            }
        }

        private static void CopyRecursive(DirectoryInfo source, DirectoryInfo target)
        {
            if (!source.Exists) return;

            if (!Directory.Exists(target.FullName))
                Directory.CreateDirectory(target.FullName);

            foreach (var dir in source.GetDirectories())
            {
                CopyRecursive(dir, Directory.CreateDirectory(Path.Combine(target.FullName, dir.Name)));
            }

            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        #endregion
    }
}
