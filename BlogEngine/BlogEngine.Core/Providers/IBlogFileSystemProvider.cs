using System.Collections.Generic;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Interface for blog file system providers (replaces abstract BlogFileSystemProvider : ProviderBase).
    /// </summary>
    public interface IBlogFileSystemProvider
    {
        void ClearFileSystem();
        FileSystem.Directory CreateDirectory(string VirtualPath);
        void DeleteDirectory(string VirtualPath);
        bool DirectoryExists(string VirtualPath);
        FileSystem.Directory GetDirectory(string VirtualPath);
        FileSystem.Directory GetDirectory(string VirtualPath, bool CreateNew);
        FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, params string[] SubPath);
        FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, bool CreateNew, params string[] SubPath);
        IEnumerable<FileSystem.Directory> GetDirectories(FileSystem.Directory BaseDirectory);
        IEnumerable<FileSystem.File> GetFiles(FileSystem.Directory BaseDirectory);
        FileSystem.File GetFile(string VirtualPath);
        bool FileExists(string VirtualPath);
        void DeleteFile(string VirtualPath);
        FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory);
        FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory, bool Overwrite);
        FileSystem.File GetFileContents(FileSystem.File BaseFile);
        FileSystem.Image ImageThumbnail(string VirtualPath, int MaximumSize);
    }
}
