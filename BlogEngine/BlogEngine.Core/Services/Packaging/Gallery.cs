using BlogEngine.Core.Data.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace BlogEngine.Core.Packaging
{
    /// <summary>
    /// Online gallery - uses HttpClient to fetch packages from the gallery feed.
    /// </summary>
    public static class Gallery
    {
        // Shared HttpClient instance for gallery operations
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Load gallery packages
        /// </summary>
        /// <param name="packages">Packages to load</param>
        public static void Load(List<Package> packages)
        {
            try
            {
                var packs = GetNugetPackages();
                var extras = GetPackageExtras();

                if (packs == null) return;

                foreach (var pkg in packs)
                {
                    var jp = new Package
                    {
                        Id = pkg.Id,
                        Authors = pkg.Authors ?? "unknown",
                        Description = pkg.Description != null && pkg.Description.Length > 140 
                            ? string.Format("{0}...", pkg.Description.Substring(0, 140)) 
                            : (pkg.Description ?? ""),
                        DownloadCount = pkg.DownloadCount,
                        LastUpdated = pkg.LastUpdated ?? "",
                        Title = string.IsNullOrEmpty(pkg.Title) ? pkg.Id : pkg.Title,
                        OnlineVersion = pkg.Version ?? "",
                        Website = pkg.ProjectUrl,
                        Tags = pkg.Tags,
                        IconUrl = pkg.IconUrl
                    };

                    if (!string.IsNullOrEmpty(jp.IconUrl) && !jp.IconUrl.StartsWith("http:"))
                        jp.IconUrl = Constants.GalleryUrl + jp.IconUrl;

                    if (string.IsNullOrEmpty(jp.IconUrl))
                        jp.IconUrl = DefaultThumbnail("");

                    if (extras != null && extras.Any())
                    {
                        var extra = extras.FirstOrDefault(e => e.Id.ToLower() == pkg.Id.ToLower() + "." + pkg.Version);

                        if (extra != null)
                        {
                            jp.Extra = extra;
                            jp.DownloadCount = extra.DownloadCount;
                            jp.Rating = extra.Rating;
                            jp.PackageType = extra.PkgType.ToString();
                        }
                    }
                    packages.Add(jp);
                }

            }
            catch (Exception ex)
            {
                Utils.Log("BlogEngine.Core.Packaging.Load", ex);
            }   
        }

        /// <summary>
        /// Gets extra fields from remote gallery if gallery supports it
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Object with extra package fields</returns>
        public static PackageExtra GetPackageExtra(string id)
        {
            try
            {
                var url = BlogConfig.GalleryFeedUrl.Replace("/nuget", "/api/extras/" + id);
                string json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<PackageExtra>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// BlogEngine.Gallery implements Nuget.Server
        /// and adds fields like download counts, reviews, ratings etc.
        /// </summary>
        /// <returns>List of extra fields if exist</returns>
        public static IEnumerable<PackageExtra> GetPackageExtras()
        {
            try
            {
                var url = BlogConfig.GalleryFeedUrl.Replace("/nuget", "/api/extras");
                string json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<List<PackageExtra>>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Rate package
        /// </summary>
        /// <param name="id">Pacakge ID</param>
        /// <param name="review">Review</param>
        /// <returns>Empty if success, message otherwise</returns>
        public static string RatePackage(string id, Review review)
        {
            try
            {
                var url = BlogConfig.GalleryFeedUrl.Replace("/nuget", "/api/review?pkgId=" + id);
                var data = JsonConvert.SerializeObject(review);
                var content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
                var response = _httpClient.PutAsync(url, content).GetAwaiter().GetResult();
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // sanitize result replacing garbage returned by service
                if (result == "\"\"") result = string.Empty;
                result = result.Replace("\"", "").Replace("\\", "");

                Blog.CurrentInstance.Cache.Remove(Constants.CacheKey);

                return result;
            }
            catch (Exception ex)
            {
                Utils.Log("Error rating package", ex);
                return ex.Message;
            }
        }

        #region Private methods

        /// <summary>
        /// Gets NuGet packages from the gallery feed using HttpClient.
        /// </summary>
        static List<GalleryPackage> GetNugetPackages()
        {
            try
            {
                // The gallery feed URL is a NuGet v2 OData feed.
                // Fetch packages as JSON from the feed endpoint.
                var url = BlogConfig.GalleryFeedUrl + "/Packages?$filter=IsLatestVersion&$format=json";
                string json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();

                // Try to parse OData response
                var odata = JsonConvert.DeserializeObject<ODataResponse>(json);
                if (odata?.D?.Results != null)
                    return odata.D.Results;

                // Fallback: try direct array deserialization
                return JsonConvert.DeserializeObject<List<GalleryPackage>>(json);
            }
            catch (Exception ex)
            {
                Utils.Log("Gallery.GetNugetPackages", ex);
                return new List<GalleryPackage>();
            }
        }

        static string DefaultThumbnail(string packageType)
        {
            switch (packageType)
            {
                case "Theme":
                    return $"{Utils.ApplicationRelativeWebRoot}Content/images/blog/Theme.png";
                case "Extension":
                    return $"{Utils.ApplicationRelativeWebRoot}Content/images/blog/ext.png";
                case "Widget":
                    return $"{Utils.ApplicationRelativeWebRoot}Content/images/blog/Widget.png";
            }
            return $"{Utils.ApplicationRelativeWebRoot}Content/images/blog/pkg.png";
        }

        #endregion

        #region Internal models for OData deserialization

        private class ODataResponse
        {
            [JsonProperty("d")]
            public ODataD D { get; set; }
        }

        private class ODataD
        {
            [JsonProperty("results")]
            public List<GalleryPackage> Results { get; set; }
        }

        /// <summary>
        /// Represents a package from the gallery NuGet feed.
        /// Replaces the NuGet.IPackage dependency.
        /// </summary>
        private class GalleryPackage
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Version { get; set; }
            public string Authors { get; set; }
            public string Description { get; set; }
            public int DownloadCount { get; set; }
            public string LastUpdated { get; set; }
            public string ProjectUrl { get; set; }
            public string Tags { get; set; }
            public string IconUrl { get; set; }
            public bool IsLatestVersion { get; set; }
        }

        #endregion
    }
}
