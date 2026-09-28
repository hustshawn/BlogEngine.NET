namespace BlogEngine.Core.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    using BlogEngine.Core.DataStore;
    using BlogEngine.Core.Packaging;

    /// <summary>
    /// Interface for blog data providers (replaces abstract BlogProvider : ProviderBase).
    /// </summary>
    public interface IBlogProvider
    {
        // BlogRoll
        void DeleteBlogRollItem(BlogRollItem blogRollItem);
        BlogRollItem SelectBlogRollItem(Guid id);
        void InsertBlogRollItem(BlogRollItem blogRollItem);
        void UpdateBlogRollItem(BlogRollItem blogRollItem);
        List<BlogRollItem> FillBlogRoll();

        // Blog
        void DeleteBlog(Blog blog);
        bool DeleteBlogStorageContainer(Blog blog);
        Blog SelectBlog(Guid id);
        void InsertBlog(Blog blog);
        void UpdateBlog(Blog blog);
        List<Blog> FillBlogs();
        bool SetupBlogFromExistingBlog(Blog existingBlog, Blog newBlog);
        bool SetupNewBlog(Blog newBlog, string userName, string email, string password);

        // Category
        void DeleteCategory(Category category);
        Category SelectCategory(Guid id);
        void InsertCategory(Category category);
        void UpdateCategory(Category category);
        List<Category> FillCategories(Blog blog);

        // Page
        void DeletePage(Page page);
        Page SelectPage(Guid id);
        void InsertPage(Page page);
        void UpdatePage(Page page);
        List<Page> FillPages();

        // Post
        void DeletePost(Post post);
        Post SelectPost(Guid id);
        void InsertPost(Post post);
        void UpdatePost(Post post);
        List<Post> FillPosts();

        // Profile
        void DeleteProfile(AuthorProfile profile);
        AuthorProfile SelectProfile(string id);
        void InsertProfile(AuthorProfile profile);
        void UpdateProfile(AuthorProfile profile);
        List<AuthorProfile> FillProfiles();

        // Referrer
        void InsertReferrer(Referrer referrer);
        void UpdateReferrer(Referrer referrer);
        Referrer SelectReferrer(Guid id);
        List<Referrer> FillReferrers();

        // Rights
        IDictionary<string, IEnumerable<string>> FillRights();
        void SaveRights(IEnumerable<Right> rights);

        // Settings
        StringDictionary LoadSettings(Blog blog);
        void SaveSettings(StringDictionary settings);

        // DataStore
        object LoadFromDataStore(ExtensionType extensionType, string extensionId);
        void RemoveFromDataStore(ExtensionType extensionType, string extensionId);
        void SaveToDataStore(ExtensionType extensionType, string extensionId, object settings);

        // Ping Services
        StringCollection LoadPingServices();
        void SavePingServices(StringCollection services);

        // Stop Words
        StringCollection LoadStopWords();

        // Packaging
        void SavePackage(InstalledPackage package);
        void SavePackageFiles(List<PackageFile> packageFiles);
        List<PackageFile> FillPackageFiles(string packageId);
        List<InstalledPackage> FillPackages();
        void DeletePackage(string packageId);

        // Custom Fields
        void SaveCustomField(BlogEngine.Core.Data.Models.CustomField field);
        List<BlogEngine.Core.Data.Models.CustomField> FillCustomFields();
        void DeleteCustomField(BlogEngine.Core.Data.Models.CustomField field);
        void ClearCustomFields(string blogId, string customType, string objectType);
    }
}
