using BlogEngine.Core.Packaging;

namespace BlogEngine.Core.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    using BlogEngine.Core.DataStore;
    using BlogEngine.Core.FileSystem;

    /// <summary>
    /// The proxy class for communication between
    ///     the business objects and the providers.
    /// Providers are set via DI at startup using Initialize methods.
    /// </summary>
    public static class BlogService
    {
        #region Constants and Fields

        /// <summary>
        /// The provider backing field.
        /// </summary>
        private static IBlogProvider _provider;

        /// <summary>
        /// The file storage provider backing field.
        /// </summary>
        private static IBlogFileSystemProvider _fileStorageProvider;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the current FileSystem provider.
        /// </summary>
        public static IBlogFileSystemProvider FileSystemProvider
        {
            get
            {
                return _fileStorageProvider ?? throw new InvalidOperationException(
                    "BlogService file system provider not initialized. Call AddBlogEngineCore() in startup.");
            }
        }

        /// <summary>
        ///     Gets the current provider.
        /// </summary>
        public static IBlogProvider Provider
        {
            get
            {
                return _provider ?? throw new InvalidOperationException(
                    "BlogService provider not initialized. Call AddBlogEngineCore() in startup.");
            }
        }

        /// <summary>
        /// Initializes the blog provider. Called from DI setup.
        /// </summary>
        /// <param name="provider">The blog provider instance.</param>
        public static void Initialize(IBlogProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Initializes the file system provider. Called from DI setup.
        /// </summary>
        /// <param name="fileSystemProvider">The file system provider instance.</param>
        public static void InitializeFileSystem(IBlogFileSystemProvider fileSystemProvider)
        {
            _fileStorageProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
        }

        internal static void ReloadFileSystemProvider()
        {
            // In the DI-based model, reloading is a no-op unless re-initialized
            // This is kept for API compatibility with FileSystemUtilities
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Deletes the specified BlogRoll from the current provider.
        /// </summary>
        public static void DeleteBlogRoll(BlogRollItem blogRoll)
        {
            Provider.DeleteBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Deletes the specified Blog from the current provider.
        /// </summary>
        public static void DeleteBlog(Blog blog)
        {
            Provider.DeleteBlog(blog);
        }

        /// <summary>
        /// Deletes the storage container for the specified Blog from the current provider.
        /// </summary>
        public static bool DeleteBlogStorageContainer(Blog blog)
        {
            return Provider.DeleteBlogStorageContainer(blog);
        }

        /// <summary>
        /// Deletes the specified Category from the current provider.
        /// </summary>
        public static void DeleteCategory(Category category)
        {
            Provider.DeleteCategory(category);
        }

        /// <summary>
        /// Deletes the specified Page from the current provider.
        /// </summary>
        public static void DeletePage(Page page)
        {
            Provider.DeletePage(page);
        }

        /// <summary>
        /// Deletes the specified Post from the current provider.
        /// </summary>
        public static void DeletePost(Post post)
        {
            Provider.DeletePost(post);
        }

        /// <summary>
        /// Deletes the specified Page from the current provider.
        /// </summary>
        public static void DeleteProfile(AuthorProfile profile)
        {
            Provider.DeleteProfile(profile);
        }

        /// <summary>
        /// Returns a list of all BlogRolls in the current provider.
        /// </summary>
        public static List<BlogRollItem> FillBlogRolls()
        {
            return Provider.FillBlogRoll();
        }

        /// <summary>
        /// The fill categories.
        /// </summary>
        public static List<Category> FillCategories(Blog blog)
        {
            return Provider.FillCategories(blog);
        }

        /// <summary>
        /// The fill pages.
        /// </summary>
        public static List<Page> FillPages()
        {
            return Provider.FillPages();
        }

        /// <summary>
        /// The fill posts.
        /// </summary>
        public static List<Post> FillPosts()
        {
            return Provider.FillPosts();
        }

        /// <summary>
        /// The fill blogs.
        /// </summary>
        public static List<Blog> FillBlogs()
        {
            return Provider.FillBlogs();
        }

        /// <summary>
        /// The fill profiles.
        /// </summary>
        public static List<AuthorProfile> FillProfiles()
        {
            return Provider.FillProfiles();
        }

        /// <summary>
        /// Returns a list of all Referrers in the current provider.
        /// </summary>
        public static List<Referrer> FillReferrers()
        {
            return Provider.FillReferrers();
        }

        /// <summary>
        /// Returns a dictionary representing rights and the roles that allow them.
        /// </summary>
        public static IDictionary<string, IEnumerable<string>> FillRights()
        {
            return Provider.FillRights();
        }

        /// <summary>
        /// Persists a new BlogRoll in the current provider.
        /// </summary>
        public static void InsertBlogRoll(BlogRollItem blogRoll)
        {
            Provider.InsertBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Persists a new Blog in the current provider.
        /// </summary>
        public static void InsertBlog(Blog blog)
        {
            Provider.InsertBlog(blog);
        }

        /// <summary>
        /// Persists a new Category in the current provider.
        /// </summary>
        public static void InsertCategory(Category category)
        {
            Provider.InsertCategory(category);
        }

        /// <summary>
        /// Persists a new Page in the current provider.
        /// </summary>
        public static void InsertPage(Page page)
        {
            Provider.InsertPage(page);
        }

        /// <summary>
        /// Persists a new Post in the current provider.
        /// </summary>
        public static void InsertPost(Post post)
        {
            Provider.InsertPost(post);
        }

        /// <summary>
        /// Persists a new Page in the current provider.
        /// </summary>
        public static void InsertProfile(AuthorProfile profile)
        {
            Provider.InsertProfile(profile);
        }

        /// <summary>
        /// Persists a new Referrer in the current provider.
        /// </summary>
        public static void InsertReferrer(Referrer referrer)
        {
            Provider.InsertReferrer(referrer);
        }

        /// <summary>
        /// Loads settings from data storage
        /// </summary>
        public static object LoadFromDataStore(ExtensionType extensionType, string extensionId)
        {
            return Provider.LoadFromDataStore(extensionType, extensionId);
        }

        /// <summary>
        /// Loads the ping services.
        /// </summary>
        public static StringCollection LoadPingServices()
        {
            return Provider.LoadPingServices();
        }

        /// <summary>
        /// Loads the settings from the provider.
        /// </summary>
        public static StringDictionary LoadSettings(Blog blog)
        {
            return Provider.LoadSettings(blog);
        }

        /// <summary>
        /// Loads the stop words from the data store.
        /// </summary>
        public static StringCollection LoadStopWords()
        {
            return Provider.LoadStopWords();
        }

        /// <summary>
        /// Removes object from data store
        /// </summary>
        public static void RemoveFromDataStore(ExtensionType extensionType, string extensionId)
        {
            Provider.RemoveFromDataStore(extensionType, extensionId);
        }

        /// <summary>
        /// Saves the ping services.
        /// </summary>
        public static void SavePingServices(StringCollection services)
        {
            Provider.SavePingServices(services);
        }

        /// <summary>
        /// Saves all of the current BlogEngine rights to the provider.
        /// </summary>
        public static void SaveRights()
        {
            Provider.SaveRights(Right.GetAllRights());

            // This needs to be called after rights are changed.
            Right.RefreshAllRights();
        }

        /// <summary>
        /// Save the settings to the current provider.
        /// </summary>
        public static void SaveSettings(StringDictionary settings)
        {
            Provider.SaveSettings(settings);
        }

        /// <summary>
        /// Saves settings to data store
        /// </summary>
        public static void SaveToDataStore(ExtensionType extensionType, string extensionId, object settings)
        {
            Provider.SaveToDataStore(extensionType, extensionId, settings);
        }

        /// <summary>
        /// Returns a BlogRoll based on the specified id.
        /// </summary>
        public static BlogRollItem SelectBlogRoll(Guid id)
        {
            return Provider.SelectBlogRollItem(id);
        }

        /// <summary>
        /// Returns a Blog based on the specified id.
        /// </summary>
        public static Blog SelectBlog(Guid id)
        {
            return Provider.SelectBlog(id);
        }

        /// <summary>
        /// Returns a Category based on the specified id.
        /// </summary>
        public static Category SelectCategory(Guid id)
        {
            return Provider.SelectCategory(id);
        }

        /// <summary>
        /// Returns a Page based on the specified id.
        /// </summary>
        public static Page SelectPage(Guid id)
        {
            return Provider.SelectPage(id);
        }

        /// <summary>
        /// Returns a Post based on the specified id.
        /// </summary>
        public static Post SelectPost(Guid id)
        {
            return Provider.SelectPost(id);
        }

        /// <summary>
        /// Returns a Page based on the specified id.
        /// </summary>
        public static AuthorProfile SelectProfile(string id)
        {
            return Provider.SelectProfile(id);
        }

        /// <summary>
        /// Returns a Referrer based on the specified id.
        /// </summary>
        public static Referrer SelectReferrer(Guid id)
        {
            return Provider.SelectReferrer(id);
        }

        /// <summary>
        /// Sets up the required storage files/tables for a new Blog instance.
        /// </summary>
        public static bool SetupBlogFromExistingBlog(Blog existingBlog, Blog newBlog)
        {
            return Provider.SetupBlogFromExistingBlog(existingBlog, newBlog);
        }

        /// <summary>
        /// Setup new blog
        /// </summary>
        public static bool SetupNewBlog(Blog newBlog, string userName, string email, string password)
        {
            return Provider.SetupNewBlog(newBlog, userName, email, password);
        }

        /// <summary>
        /// Updates an exsiting BlogRoll.
        /// </summary>
        public static void UpdateBlogRoll(BlogRollItem blogRoll)
        {
            Provider.UpdateBlogRollItem(blogRoll);
        }

        /// <summary>
        /// Updates an exsiting Blog.
        /// </summary>
        public static void UpdateBlog(Blog blog)
        {
            Provider.UpdateBlog(blog);
        }

        /// <summary>
        /// Updates an exsiting Category.
        /// </summary>
        public static void UpdateCategory(Category category)
        {
            Provider.UpdateCategory(category);
        }

        /// <summary>
        /// Updates an exsiting Page.
        /// </summary>
        public static void UpdatePage(Page page)
        {
            Provider.UpdatePage(page);
        }

        /// <summary>
        /// Updates an exsiting Post.
        /// </summary>
        public static void UpdatePost(Post post)
        {
            Provider.UpdatePost(post);
        }

        /// <summary>
        /// Updates an exsiting Page.
        /// </summary>
        public static void UpdateProfile(AuthorProfile profile)
        {
            Provider.UpdateProfile(profile);
        }

        /// <summary>
        /// Updates an existing Referrer.
        /// </summary>
        public static void UpdateReferrer(Referrer referrer)
        {
            Provider.UpdateReferrer(referrer);
        }

        #region FileSystem Static Methods

        internal static void ClearFileSystem()
        {
            FileSystemProvider.ClearFileSystem();
        }

        /// <summary>
        /// Creates a directory at a specific path
        /// </summary>
        public static Directory CreateDirectory(string VirtualPath)
        {
            return FileSystemProvider.CreateDirectory(VirtualPath);
        }

        /// <summary>
        /// Deletes a spefic directory from a virtual path
        /// </summary>
        public static void DeleteDirectory(string VirtualPath)
        {
            FileSystemProvider.DeleteDirectory(VirtualPath);
        }

        /// <summary>
        /// Deletes a directory by passing in the Directory object
        /// </summary>
        public static void DeleteDirectory(Directory DirectoryObj)
        {
            DeleteDirectory(DirectoryObj.FullPath);
        }

        /// <summary>
        /// Returns wether or not the specific directory by virtual path exists
        /// </summary>
        public static bool DirectoryExists(string VirtualPath)
        {
            return FileSystemProvider.DirectoryExists(VirtualPath);
        }

        /// <summary>
        /// gets a directory by the virtual path
        /// </summary>
        public static Directory GetDirectory(string VirtualPath)
        {
            return FileSystemProvider.GetDirectory(VirtualPath);
        }

        /// <summary>
        /// gets a directory by a basedirectory and a string array of sub path tree
        /// </summary>
        public static Directory GetDirectory(Directory BaseDirectory, params string[] SubPath)
        {
            return FileSystemProvider.GetDirectory(BaseDirectory, SubPath);
        }

        /// <summary>
        /// gets all the directories underneath a base directory. Only searches one level.
        /// </summary>
        internal static IEnumerable<Directory> GetDirectories(Directory BaseDirectory)
        {
            return FileSystemProvider.GetDirectories(BaseDirectory);
        }

        /// <summary>
        /// gets all the files in a directory, only searches one level
        /// </summary>
        public static IEnumerable<File> GetFiles(Directory BaseDirectory)
        {
            return FileSystemProvider.GetFiles(BaseDirectory);
        }

        /// <summary>
        /// gets a specific file by virtual path
        /// </summary>
        public static File GetFile(string VirtualPath)
        {
            return FileSystemProvider.GetFile(VirtualPath);
        }

        /// <summary>
        /// boolean wether a file exists by its virtual path
        /// </summary>
        public static bool FileExists(string VirtualPath)
        {
            return FileSystemProvider.FileExists(VirtualPath);
        }

        /// <summary>
        /// deletes a file by virtual path
        /// </summary>
        public static void DeleteFile(string VirtualPath)
        {
            FileSystemProvider.DeleteFile(VirtualPath);
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public static FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory)
        {
            return FileSystemProvider.UploadFile(FileBinary, FileName, BaseDirectory);
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public static FileSystem.File UploadFile(byte[] FileBinary, string FileName, FileSystem.Directory BaseDirectory, bool Overwrite)
        {
            return FileSystemProvider.UploadFile(FileBinary, FileName, BaseDirectory, Overwrite);
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public static FileSystem.File UploadFile(System.IO.Stream FileStream, string FileName, FileSystem.Directory BaseDirectory)
        {
            return UploadFile(FileStream, FileName, BaseDirectory, false);
        }

        /// <summary>
        /// uploads a file to the provider container
        /// </summary>
        public static FileSystem.File UploadFile(System.IO.Stream FileStream, string FileName, FileSystem.Directory BaseDirectory, bool Overwrite)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            FileStream.CopyTo(ms);
            FileStream.Close();
            byte[] binary = ms.ToArray();
            ms.Close();
            return FileSystemProvider.UploadFile(binary, FileName, BaseDirectory, Overwrite);
        }

        /// <summary>
        /// gets the file contents via Lazy load
        /// </summary>
        internal static File GetFileContents(File BaseFile)
        {
            return FileSystemProvider.GetFileContents(BaseFile);
        }
        #endregion

        #region Packaging

        /// <summary>
        /// Save installed gallery package
        /// </summary>
        public static void InsertPackage(InstalledPackage package)
        {
            Provider.SavePackage(package);
        }

        /// <summary>
        /// Save package files
        /// </summary>
        public static void InsertPackageFiles(List<PackageFile> packageFiles)
        {
            Provider.SavePackageFiles(packageFiles);
        }

        /// <summary>
        /// Packages installed from online gallery
        /// </summary>
        public static List<InstalledPackage> InstalledFromGalleryPackages()
        {
            return Provider.FillPackages();
        }

        /// <summary>
        /// Log of files installed by gallery package
        /// </summary>
        public static List<PackageFile> InstalledFromGalleryPackageFiles(string packageId)
        {
            return Provider.FillPackageFiles(packageId);
        }

        /// <summary>
        /// Delete all installed by package files from application
        /// </summary>
        public static void DeletePackage(string packageId)
        {
            Provider.DeletePackage(packageId);
        }

        #endregion

        #region Custom fields

        /// <summary>
        /// Saves custom field
        /// </summary>
        public static void SaveCustomField(BlogEngine.Core.Data.Models.CustomField field)
        {
            Provider.SaveCustomField(field);
        }

        /// <summary>
        /// Fills list of custom fields for a blog
        /// </summary>
        public static List<BlogEngine.Core.Data.Models.CustomField> FillCustomFields()
        {
            return Provider.FillCustomFields();
        }

        /// <summary>
        /// Deletes custom field
        /// </summary>
        public static void DeleteCustomField(BlogEngine.Core.Data.Models.CustomField field)
        {
            Provider.DeleteCustomField(field);
        }

        /// <summary>
        /// Clear custom fields for a type (post, theme etc)
        /// </summary>
        public static void ClearCustomFields(string blogId, string customType, string objectType)
        {
            Provider.ClearCustomFields(blogId, customType, objectType);
        }

        /// <summary>
        /// Returns an array of all user names from profiles.
        /// This replaces the legacy Membership.GetAllUsers() call.
        /// </summary>
        /// <returns>Array of user names, or null if no profiles exist.</returns>
        public static string[] GetUserNames()
        {
            var profiles = FillProfiles();
            if (profiles == null || profiles.Count == 0)
                return null;

            return profiles.Select(p => p.UserName).ToArray();
        }

        #endregion

        #endregion
    }
}
