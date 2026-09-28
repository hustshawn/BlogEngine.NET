using System.Collections.Generic;


namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// Abstract base class for managing all FileSystem methods.
    /// Implements IBlogFileSystemProvider for DI registration.
    /// </summary>
    public abstract class BlogFileSystemProvider : IBlogFileSystemProvider
    {
        #region FileSystem

        /// <summary>
        /// Clears a file system. This will delete all files and folders recursivly.
        /// </summary>
        public abstract void ClearFileSystem();

        /// <summary>
        /// Creates a directory at a specific path
        /// </summary>
        /// <param name="VirtualPath">The virtual path to be created</param>
        /// <returns>the new FileSystem.Directory object created</returns>
        public abstract FileSystem.Directory CreateDirectory(string VirtualPath);

        /// <summary>
        /// Deletes a spefic directory from a virtual path
        /// </summary>
        /// <param name="VirtualPath">The path to delete</param>
        public abstract void DeleteDirectory(string VirtualPath);

        /// <summary>
        /// Returns wether or not the specific directory by virtual path exists
        /// </summary>
        /// <param name="VirtualPath">The virtual path to query</param>
        /// <returns>boolean</returns>
        public abstract bool DirectoryExists(string VirtualPath);

        /// <summary>
        /// gets a directory by the virtual path, creates the directory path if it does not exist
        /// </summary>
        /// <param name="VirtualPath">the virtual path</param>
        /// <returns>the directory object</returns>
        public abstract FileSystem.Directory GetDirectory(string VirtualPath);

        /// <summary>
        /// gets a directory by the virtual path, with a flag to create if not found
        /// </summary>
        /// <param name="VirtualPath">The virtual path</param>
        /// <param name="CreateNew">bool yes \ no to create the director.</param>
        /// <returns>the directory object, or null if the create flag is set to false</returns>
        public abstract FileSystem.Directory GetDirectory(string VirtualPath, bool CreateNew);

        /// <summary>
        /// gets a directory by a basedirectory and a string array of sub path tree, directory will be created if not found
        /// </summary>
        /// <param name="BaseDirectory">the base directory object</param>
        /// <param name="SubPath">the params of sub path</param>
        /// <returns>the directory found</returns>
        public abstract FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, params string[] SubPath);

        /// <summary>
        /// gets a directory by a basedirectory and a string array of sub path tree
        /// </summary>
        /// <param name="BaseDirectory">the base directory object</param>
        /// <param name="CreateNew">if set will create the directory structure</param>
        /// <param name="SubPath">the params of sub path</param>
        /// <returns>the directory found, or null for no directory found</returns>
        public abstract FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, bool CreateNew, params string[] SubPath);

        /// <summary>
        /// gets all the directories underneath a base directory. Only searches one level.
        /// </summary>
        /// <param name="BaseDirectory">the base directory</param>
        /// <returns>collection of FileSystem.Directory objects</returns>
        public abstract IEnumerable<FileSystem.Directory> GetDirectories(FileSystem.Directory BaseDirectory);

        /// <summary>
        /// gets all the files in a directory, only searches one level
        /// </summary>
        /// <param name="BaseDirectory">the base directory</param>
        /// <returns>collection of FileSystem.File objects</returns>
        public abstract IEnumerable<FileSystem.File> GetFiles(FileSystem.Directory BaseDirectory);

        /// <summary>
        /// gets a specific file by virtual path
        /// </summary>
        /// <param name="VirtualPath">the virtual path of the file</param>
        /// <returns></returns>
        public abstract FileSystem.File GetFile(string VirtualPath);

        /// <summary>
        /// boolean wether a file exists by its virtual path
        /// </summary>
        /// <param name="VirtualPath">the virtual path</param>
        /// <returns>boolean</returns>
        public abstract bool FileExists(string VirtualPath);

        /// <summary>
        /// deletes a file by virtual path
        /// </summary>
        /// <param name="VirtualPath">virtual path</param>
        public abstract void DeleteFile(string VirtualPath);

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        /// <param name="FileBinary">file contents as byte array</param>
        /// <param name="FileName">the file name</param>
        /// <param name="BaseDirectory">directory object that is the owner</param>
        /// <returns>the new file object</returns>
        public abstract FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory);

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        /// <param name="FileBinary">the contents of the file as a byte array</param>
        /// <param name="FileName">the file name</param>
        /// <param name="BaseDirectory">the directory object that is the owner</param>
        /// <param name="Overwrite">boolean wether to overwrite the file if it exists.</param>
        /// <returns>the new file object</returns>
        public abstract FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory, bool Overwrite);

        /// <summary>
        /// gets the file contents via Lazy load
        /// </summary>
        /// <param name="BaseFile">the baseFile object to fill</param>
        /// <returns>the original file object</returns>
        public abstract FileSystem.File GetFileContents(FileSystem.File BaseFile);

        /// <summary>
        /// Subclasses may override this method to generate a thumbnail image at a maximum size.
        /// </summary>
        /// <param name="VirtualPath">The virtual path of the image</param>
        /// <param name="MaximumSize">The maximum size for the image</param>
        /// <returns>The image with the thumbnail contents</returns>
        public abstract FileSystem.Image ImageThumbnail(string VirtualPath, int MaximumSize);
        #endregion
    }
}
