using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using BlogEngine.Core.Providers;

namespace BlogEngine.Core.FileSystem
{
    /// <summary>
    /// Utility class for various FileSystem operations.
    /// </summary>
    public class FileSystemUtilities
    {
        /// <summary>
        /// Compresses a directory using Zip compression into a specified directory
        /// </summary>
        /// <param name="ArchiveOutputLocation">the output directory path (including file name)</param>
        /// <param name="ArchiveDirectory">the Directory object to be compressed</param>
        /// <remarks>
        /// is recursive
        /// </remarks>
        public void CompressDirectory(string ArchiveOutputLocation, Directory ArchiveDirectory)
        {
            using (ZipOutputStream zStream = new ZipOutputStream(System.IO.File.Create(ArchiveOutputLocation)))
            {
                foreach (var file in ArchiveDirectory.Files)
                {
                    ZipEntry fileEntry = new ZipEntry(file.FullPath);
                    zStream.PutNextEntry(fileEntry);
                    zStream.Write(file.FileContents, 0, file.FileContents.Length);
                }
                foreach (var directory in ArchiveDirectory.Directories)
                    ZipDirectory(ArchiveDirectory, directory, zStream);
                zStream.Finish();
                zStream.Close();
            }
        }

        /// <summary>
        /// Adds a directory to a specific ZipStream
        /// </summary>
        private void ZipDirectory(Directory RootDirectory, Directory CurrentDirectory, ZipOutputStream zStream)
        {
            foreach (var file in CurrentDirectory.Files)
            {
                ZipEntry fileEntry = new ZipEntry(file.FilePath);
                zStream.PutNextEntry(fileEntry);
                zStream.Write(file.FileContents, 0, file.FileContents.Length);
            }

            foreach (var subDirectory in CurrentDirectory.Directories)
                ZipDirectory(RootDirectory, subDirectory, zStream);
        }

        /// <summary>
        /// Dumps the file store provider to a zip file, clears the old file store, switches and reloads a new provider,
        ///     clears the new file storage provider, then loads the new file store provider from the zip.
        /// </summary>
        /// <param name="NewProviderName">The name of the new provider (unused in DI model, kept for API compat).</param>
        /// <param name="ArchiveOutputLocation">The output archive location.</param>
        /// <returns>Message string, empty if successful</returns>
        /// <remarks>
        /// In the DI-based model, provider switching requires re-initializing via DI.
        /// This method preserves the archive/restore logic but provider switching
        /// should be handled at the DI composition root level.
        /// </remarks>
        public string DumpProvider(string NewProviderName, string ArchiveOutputLocation)
        {
            string Message = string.Empty;
            CompressDirectory(ArchiveOutputLocation, Blog.CurrentInstance.RootFileStore);
            try
            {
                BlogService.ClearFileSystem();
            }
            catch
            {
                Message = "Error while clearing the old file system provider, there may have been a lock on a file. You will have to manually clear the old file system. The update operation has bypassed clearing the old file storage, however will continue to import your data into your new file storage.<br/>";
            }

            // REVIEW: In the DI-based model, provider switching is done at startup configuration level.
            // The archive/restore still works, but dynamic provider switching at runtime
            // requires re-initializing BlogService.InitializeFileSystem() with the new provider.
            try
            {
                BlogService.ReloadFileSystemProvider();
                BlogService.ClearFileSystem();
                ExtractDirectory(ArchiveOutputLocation);
            }
            catch
            {
                // Restore failed - try to restore original data
                BlogService.ReloadFileSystemProvider();
                BlogService.ClearFileSystem();
                ExtractDirectory(ArchiveOutputLocation);
                Message = "Error while copying data to the new file storage provider, your changes were not successful.<br/>";
            }
            if (System.IO.File.Exists(ArchiveOutputLocation))
                System.IO.File.Delete(ArchiveOutputLocation);
            return Message;
        }

        private void ExtractDirectory(string ArchiveOutputLocation)
        {
            using (ZipInputStream zStream = new ZipInputStream(System.IO.File.OpenRead(ArchiveOutputLocation)))
            {
                ZipEntry theEntry;
                while ((theEntry = zStream.GetNextEntry()) != null)
                {
                    var exDir = BlogService.GetDirectory(Path.GetDirectoryName(theEntry.Name).Replace(@"\","/"));
                    var exName = Path.GetFileName(theEntry.Name);
                    using (MemoryStream streamWriter = new MemoryStream())
                    {
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = zStream.Read(data, 0, data.Length);
                            if (size > 0)
                                streamWriter.Write(data, 0, size);
                            else
                                break;

                        }
                        var arr = streamWriter.ToArray();
                        BlogService.UploadFile(arr,
                                               exName,
                                               exDir);
                    }

                }
            }
        }

        /// <summary>
        /// Retrieves the blog provider from DI.
        /// In the DI model, providers are resolved from the service container, not from web.config.
        /// </summary>
        /// <param name="ProviderName">Unused in DI model - kept for API compatibility.</param>
        /// <returns>The current blog provider instance.</returns>
        public static BlogProvider CreateProvider(string ProviderName)
        {
            // In the DI-based model, the provider is always BlogService.Provider
            return BlogService.Provider as BlogProvider;
        }

    }        
}
