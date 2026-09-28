using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BlogEngine.Core.Providers;
using BlogEngine.Core.Services.Security;
using BlogEngine.Core.Web.Controls;

namespace BlogEngine.Core
{
    /// <summary>
    /// Extension methods for registering BlogEngine.Core services with DI.
    /// </summary>
    public static class BlogEngineCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds BlogEngine.Core services to the DI container.
        /// Registers IBlogProvider, IBlogFileSystemProvider, IMembershipService,
        /// IRoleService, and ISiteMapService based on configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddBlogEngineCore(this IServiceCollection services, IConfiguration configuration)
        {
            // Set static configuration on BlogConfig so static accessors work
            BlogConfig.Configuration = configuration;

            // Register Microsoft.Data.SqlClient as a DbProviderFactory
            System.Data.Common.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);

            // Ensure IHttpContextAccessor is registered
            services.AddHttpContextAccessor();

            // Determine which provider to use from configuration
            var providerType = configuration["BlogEngine:Provider"] ?? "Xml";

            if (providerType.Equals("Db", System.StringComparison.OrdinalIgnoreCase) ||
                providerType.Equals("DbBlogProvider", System.StringComparison.OrdinalIgnoreCase))
            {
                // Database provider
                services.AddSingleton<IBlogProvider>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var provider = new DbBlogProvider(config);
                    BlogService.Initialize(provider);
                    return provider;
                });

                services.AddSingleton<IBlogFileSystemProvider>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var provider = new DbFileSystemProvider(config);
                    BlogService.InitializeFileSystem(provider);
                    return provider;
                });

                // Database membership and role services
                services.AddSingleton<IMembershipService>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return new DbMembershipService(config);
                });

                services.AddSingleton<IRoleService>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return new DbRoleService(config);
                });
            }
            else
            {
                // XML provider (default)
                services.AddSingleton<IBlogProvider>(sp =>
                {
                    var provider = new XmlBlogProvider();
                    BlogService.Initialize(provider);
                    return provider;
                });

                services.AddSingleton<IBlogFileSystemProvider>(sp =>
                {
                    var provider = new XmlFileSystemProvider();
                    BlogService.InitializeFileSystem(provider);
                    return provider;
                });

                // XML membership and role services
                services.AddSingleton<IMembershipService>(sp =>
                {
                    return new XmlMembershipService();
                });

                services.AddSingleton<IRoleService>(sp =>
                {
                    var membershipService = sp.GetRequiredService<IMembershipService>();
                    return new XmlRoleService(membershipService);
                });
            }

            // Site map services
            services.AddSingleton<ISiteMapService, PageSiteMap>();

            // Initialize Security static services after all are registered
            services.AddSingleton<SecurityInitializer>();


            // Wire up static context holders on first resolution
            services.AddMemoryCache();
            var sp = services.BuildServiceProvider();
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            var memoryCache = sp.GetService<IMemoryCache>();
            if (httpContextAccessor != null)
            {
                Blog.SetHttpContextAccessor(httpContextAccessor);
                Utils.SetHttpContextAccessor(httpContextAccessor);
            }
            if (memoryCache != null)
            {
                Blog.SetMemoryCache(memoryCache);
            }

            return services;
        }

        /// <summary>
        /// Sets the content root path for BlogEngine path resolution.
        /// Call this in Program.cs after building the host.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="contentRootPath">The physical content root path.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection SetBlogEngineContentRoot(this IServiceCollection services, string contentRootPath)
        {
            BlogConfig.ContentRootPath = contentRootPath;
            return services;
        }
    }

    /// <summary>
    /// Helper class that initializes the Security static services from DI.
    /// Resolve this singleton after all services are registered to wire up static references.
    /// </summary>
    public class SecurityInitializer
    {
        /// <summary>
        /// Creates a SecurityInitializer that wires up the static Security and Right classes.
        /// </summary>
        public SecurityInitializer(
            IRoleService roleService,
            IMembershipService membershipService,
            IHttpContextAccessor httpContextAccessor)
        {
            Security.Initialize(roleService, membershipService, httpContextAccessor);
            Right.Initialize(roleService, membershipService);
        }
    }
}
