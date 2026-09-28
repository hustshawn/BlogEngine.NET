using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace BlogEngine.Core.Providers
{
    /// <summary>
    /// DbBlogProvider Parial class for manageing all FileSystem methods
    /// </summary>
    public partial class DbFileSystemProvider : BlogFileSystemProvider
    {

        /// <summary>
        /// The conn string name.
        /// </summary>
        private string connStringName;

        /// <summary>
        /// The parm prefix.
        /// </summary>
        private string parmPrefix;

        /// <summary>
        /// The table prefix.
        /// </summary>
        private string tablePrefix;

        /// <summary>
        /// The provider name for database connections.
        /// </summary>
        private string providerName;

        /// <summary>
        /// The connection string value.
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DbFileSystemProvider()
        {
        }

        /// <summary>
        /// Constructor accepting IConfiguration for connection string resolution.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="connectionStringName">Name of the connection string. Defaults to "BlogEngine".</param>
        /// <param name="tablePrefix">Table prefix. Defaults to "be_".</param>
        /// <param name="parmPrefix">Parameter prefix. Defaults to "@".</param>
        public DbFileSystemProvider(IConfiguration configuration, string connectionStringName = "BlogEngine", string tablePrefix = "be_", string parmPrefix = "@")
        {
            this.connStringName = connectionStringName;
            this.tablePrefix = tablePrefix;
            this.parmPrefix = parmPrefix;

            this._connectionString = configuration.GetConnectionString(connectionStringName);
            this.providerName = configuration[$"ConnectionStrings:{connectionStringName}_ProviderName"]
                ?? "Microsoft.Data.SqlClient";
        }

        #region Contansts & Fields
        /// <summary>
        /// active connection string
        /// </summary>
        private string connectionString { get { return _connectionString; } }
        #endregion

        #region Methods

        /// <summary>
        /// Clears a file system. This will delete all files and folders recursivly.
        /// </summary>
        public override void ClearFileSystem()
        {
            var root = Blog.CurrentInstance.RootFileStore;
            foreach (var directory in root.Directories)
                directory.Delete();
            foreach (var file in root.Files)
                file.Delete();
            root.Delete();
        }

        /// <summary>
        /// Creates a directory at a specific path
        /// </summary>
        public override FileSystem.Directory CreateDirectory(string VirtualPath)
        {
            VirtualPath = VirtualPath.VirtualPathToDbPath();
            if (DirectoryExists(VirtualPath))
                return GetDirectory(VirtualPath);
            var directoryName = "root";
            if (!string.IsNullOrWhiteSpace(VirtualPath))
                directoryName = string.Format("/{0}", VirtualPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last());
            FileSystem.FileStoreDb db = new FileSystem.FileStoreDb(this.connectionString);
            FileSystem.FileStoreDirectory dir = new FileSystem.FileStoreDirectory
            {
                BlogID = Blog.CurrentInstance.Id,
                CreateDate = DateTime.Now,
                FullPath = VirtualPath,
                Id = Guid.NewGuid(),
                LastAccess = DateTime.Now,
                Name = directoryName,
                LastModify = DateTime.Now
            };
            if (!string.IsNullOrWhiteSpace(VirtualPath))
            {
                var parentPath = VirtualPath.Contains("/") ? VirtualPath.Substring(0, VirtualPath.LastIndexOf("/")) : string.Empty;
                dir.ParentID = this.GetDirectory(parentPath).Id;
            }
            db.FileStoreDirectories.Add(dir);
            db.SubmitChanges();
            db.Dispose();
            return GetDirectory(VirtualPath);
        }

        /// <summary>
        /// Deletes a spefic directory from a virtual path
        /// </summary>
        public override void DeleteDirectory(string VirtualPath)
        {
            if (!DirectoryExists(VirtualPath.VirtualPathToDbPath()))
                return;
            FileSystem.FileStoreDb db = new FileSystem.FileStoreDb(this.connectionString);
            var query = db.FileStoreDirectories.Where(x => x.FullPath.ToLower() == VirtualPath.VirtualPathToDbPath().ToLower() && x.BlogID == Blog.CurrentInstance.Id);
            foreach (var item in query)
            {
                var subDirectories = db.FileStoreDirectories.Where(x => x.ParentID == item.Id);
                foreach (var sb in subDirectories)
                    DeleteDirectory(sb.FullPath);
            }
            db.FileStoreDirectories.Clear(); // Replaced DeleteAllOnSubmit - delete individually //query);
            db.SubmitChanges();
            db.Dispose();
        }

        /// <summary>
        /// Returns wether or not the specific directory by virtual path exists
        /// </summary>
        public override bool DirectoryExists(string VirtualPath)
        {
            VirtualPath = VirtualPath.VirtualPathToDbPath();
            return new FileSystem.FileStoreDb(this.connectionString).FileStoreDirectories.Any(x => x.FullPath.ToLower() == VirtualPath.ToLower() && x.BlogID == Blog.CurrentInstance.Id);
        }

        /// <summary>
        /// gets a directory by the virtual path, creates the directory path if it does not exist
        /// </summary>
        public override FileSystem.Directory GetDirectory(string VirtualPath)
        {
            return GetDirectory(VirtualPath, true);   
        }

        /// <summary>
        /// gets a directory by the virtual path, with a flag to create if not found
        /// </summary>
        public override FileSystem.Directory GetDirectory(string VirtualPath, bool CreateNew)
        {
            VirtualPath = VirtualPath.VirtualPathToDbPath();
            FileSystem.FileStoreDb db = new FileSystem.FileStoreDb(this.connectionString);
            if (string.IsNullOrWhiteSpace(VirtualPath))
            {
                var directory = db.FileStoreDirectories.FirstOrDefault(x => x.BlogID == Blog.CurrentInstance.Id && x.ParentID == null);
                if (directory == null)
                {
                    db.Dispose();
                    return this.CreateDirectory(VirtualPath);
                }
                var obj = directory.CopyToDirectory();
                db.Dispose();
                return obj;
            }
            else
            {
                var dir = db.FileStoreDirectories.FirstOrDefault(x => x.FullPath.ToLower() == VirtualPath.ToLower() && x.BlogID == Blog.CurrentInstance.Id);
                if (dir == null)
                {
                    var newDirectoryPieces = VirtualPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var cPath = string.Empty;
                    foreach (var pieces in newDirectoryPieces)
                    {
                        cPath = $"{cPath}/{pieces}";
                        if (!DirectoryExists(cPath))
                            CreateDirectory(cPath);
                    }
                    var nObj = GetDirectory(VirtualPath);
                    db.Dispose();
                    return nObj;
                }
                var obj = dir.CopyToDirectory();
                db.Dispose();
                return obj;
            }
        }

        /// <summary>
        /// gets a directory by a basedirectory and a string array of sub path tree
        /// </summary>
        public override FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, params string[] SubPath)
        {
            return GetDirectory(string.Concat(BaseDirectory.FullPath, string.Join("/", SubPath)));
        }

        /// <summary>
        /// gets a directory by a basedirectory and a string array of sub path tree.
        /// </summary>
        public override FileSystem.Directory GetDirectory(FileSystem.Directory BaseDirectory, bool CreateNew, params string[] SubPath)
        {
            return GetDirectory(string.Concat(BaseDirectory.FullPath, string.Join("/", SubPath)), CreateNew);
        }

        /// <summary>
        /// gets all the directories underneath a base directory. Only searches one level.
        /// </summary>
        public override IEnumerable<FileSystem.Directory> GetDirectories(FileSystem.Directory BaseDirectory)
        {
            FileSystem.FileStoreDb db = new FileSystem.FileStoreDb(this.connectionString);
            var dirs = db.FileStoreDirectories.Where(x => x.ParentID == BaseDirectory.Id && x.BlogID == Blog.CurrentInstance.Id);
            List<FileSystem.Directory> directories = new List<FileSystem.Directory>();
            foreach (var d in dirs)
                directories.Add(d.CopyToDirectory());
            db.Dispose();
            return directories;
        }

        /// <summary>
        /// gets all the files in a directory, only searches one level
        /// </summary>
        public override IEnumerable<FileSystem.File> GetFiles(FileSystem.Directory BaseDirectory)
        {
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var fileDir = db.FileStoreDirectories.FirstOrDefault(x=>x.Id == BaseDirectory.Id);
            if (fileDir == null)
                return new List<FileSystem.File>();
            var arr = fileDir.FileStoreFiles.Select(x => x.CopyToFile()).ToList();
            db.Dispose();
            return arr;
        }

        /// <summary>
        /// gets a specific file by virtual path
        /// </summary>
        public override FileSystem.File GetFile(string VirtualPath)
        {
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var file = db.FileStoreFiles.FirstOrDefault(x => x.FullPath == VirtualPath.VirtualPathToDbPath() && x.FileStoreDirectory.BlogID == Blog.CurrentInstance.Id).CopyToFile();
            db.Dispose();
            return file;
        }

        /// <summary>
        /// boolean wether a file exists by its virtual path
        /// </summary>
        public override bool FileExists(string VirtualPath)
        {
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var file = db.FileStoreFiles.FirstOrDefault(x => x.FullPath.ToLower() == VirtualPath.VirtualPathToDbPath() && x.FileStoreDirectory.BlogID == Blog.CurrentInstance.Id) == null;
            db.Dispose();
            return file;
        }

        /// <summary>
        /// deletes a file by virtual path
        /// </summary>
        public override void DeleteFile(string VirtualPath)
        {
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var file = db.FileStoreFiles.Where(x => x.FullPath == VirtualPath.VirtualPathToDbPath() && x.FileStoreDirectory.BlogID == Blog.CurrentInstance.Id);
            db.FileStoreFiles.Clear(); // Replaced DeleteAllOnSubmit - delete individually //file);
            db.SubmitChanges();
            db.Dispose();
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public override FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory)
        {
            return UploadFile(FileBinary, FileName, BaseDirectory, false);
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public override FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory, bool Overwrite)
        {
            var virtualPath = BaseDirectory.FullPath + "/" + FileName;
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var files = db.FileStoreFiles.Where(x => x.FileStoreDirectory.Id == BaseDirectory.Id && x.FullPath.ToLower() == virtualPath);
            if (files.Count() > 0)
            {
                if (!Overwrite)
                {
                    db.Dispose();
                    throw new Exception("File " + FileName + " already exists in this path.");
                }
                db.FileStoreFiles.Clear(); // Replaced DeleteAllOnSubmit - delete individually //files);
                db.SubmitChanges();
            }
            var file = new FileSystem.FileStoreFile()
            {
                Contents = FileBinary,
                CreateDate = DateTime.Now,
                FileID = Guid.NewGuid(),
                FullPath = virtualPath,
                LastAccess = DateTime.Now,
                LastModify = DateTime.Now,
                Name = FileName,
                ParentDirectoryID = BaseDirectory.Id,
                Size = FileBinary.Length
            };
            db.FileStoreFiles.Add(file);
            db.SubmitChanges();
            db.Dispose();
            return file.CopyToFile();
        }

        /// <summary>
        /// gets the file contents via Lazy load
        /// </summary>
        public override FileSystem.File GetFileContents(FileSystem.File BaseFile)
        {
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var file = db.FileStoreFiles.FirstOrDefault(x => x.FileID == Guid.Parse(BaseFile.Id));
            if (file == null)
                throw new ArgumentException("File not found in dataset");
            BaseFile.FileContents = file.Contents.ToArray();
            db.Dispose();
            return BaseFile;
        }

        /// <summary>
        /// Returns a thumbnail image at a maximum size.
        /// </summary>
        public override FileSystem.Image ImageThumbnail(string VirtualPath, int MaximumSize)
        {
            var file = GetFile(VirtualPath);
            if (!file.IsImage)
                return null;
            var db = new FileSystem.FileStoreDb(this.connectionString);
            var image = file.AsImage;
            var thumbnail = db.FileStoreFileThumbs.FirstOrDefault(x => x.FileId == Guid.Parse(image.Id));
            if (thumbnail == null)
            {

                FileSystem.FileStoreFileThumb thumb = new FileSystem.FileStoreFileThumb()
                {
                    contents = FileSystem.Image.ResizeImageThumbnail(MaximumSize, image.FileContents),
                    FileId = Guid.Parse(image.Id),
                    size = MaximumSize,
                    thumbnailId = Guid.NewGuid()
                };
                db.FileStoreFileThumbs.Add(thumb);
                db.SubmitChanges();
                image.FileContents = thumb.contents.ToArray();
            }
            else
                image.FileContents = thumbnail.contents.ToArray();
            db.Dispose();
            return image;
        }

        #endregion
    }

    #region Extension Methods
    /// <summary>
    /// static classes for the DbFileSystem
    /// </summary>
    public static class DbFileSystemExtensions
    {
        /// <summary>
        /// copy's the database directory object to a Directory object
        /// </summary>
        public static FileSystem.Directory CopyToDirectory(this FileSystem.FileStoreDirectory inObj)
        {
            if (inObj == null)
                return null;
            return new FileSystem.Directory()
            {
                DateCreated = inObj.CreateDate,
                DateModified = inObj.LastModify,
                FullPath = inObj.FullPath,
                Id = inObj.Id,
                LastAccessTime = inObj.LastAccess,
                Name = inObj.Name,
                IsRoot = inObj.ParentID == null,
            };
        }

        /// <summary>
        /// copys a database File object to a File object
        /// </summary>
        public static FileSystem.File CopyToFile(this FileSystem.FileStoreFile inObj)
        {
            if (inObj == null)
                return null;
            return new FileSystem.File()
            {
                DateCreated = inObj.CreateDate,
                DateModified = inObj.LastModify,
                FilePath = inObj.FullPath,
                FileSize = inObj.Size,
                Id = inObj.FileID.ToString(),
                LastAccessTime = inObj.LastAccess,
                Name = inObj.Name,
                FileContents = inObj.Contents.ToArray(),
                FullPath = inObj.FullPath
            };

        }

        /// <summary>
        /// removes the virtual path of BlogStorageLocation + files from the virtual path. 
        /// </summary>
        public static string VirtualPathToDbPath(this string VirtualPath)
        {
            return VirtualPath.Replace(Blog.CurrentInstance.StorageLocation + Utils.FilesFolder, "");
        }
    }
    #endregion

}
