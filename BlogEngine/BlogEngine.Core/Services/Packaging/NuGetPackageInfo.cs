using System;
using System.Collections.Generic;

namespace BlogEngine.Core.Services.Packaging
{
    /// <summary>
    /// Simple POCO representing package metadata, replacing the NuGet.IPackage interface.
    /// </summary>
    public class NuGetPackageInfo
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> Authors { get; set; }
        public Uri ProjectUrl { get; set; }
        public Uri IconUrl { get; set; }
        public Version Version { get; set; }
    }
}
