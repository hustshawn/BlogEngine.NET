using BlogEngine.Core;
using BlogEngine.Core.Data;
using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Web.HttpHandlers;
using BlogEngine.NET.AppCode.Api;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Components (Blazor Server) with interactive server-side rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MVC controllers with Web API 2 compatible routing, filters and JSON (PascalCase, as the admin SPA expects)
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<UnauthorizedAccessExceptionFilterAttribute>();
        options.Filters.Add<UnhandledExceptionLogger>();
    })
    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver())
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });
builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApplicationModelProvider, WebApiRoutingProvider>());

// Admin SPA shell pages (admin/*.cshtml) keep their legacy URLs via explicit @page routes
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/admin";
    options.Conventions.AddPageRoute("/index", "admin/index.cshtml");
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Register BlogEngine.Core providers (blog, file system, membership, roles)
builder.Services.SetBlogEngineContentRoot(builder.Environment.ContentRootPath);
builder.Services.AddBlogEngineCore(builder.Configuration);

// Register DI services (replaces SimpleInjector RegisterDiContainer)
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<ILookupsRepository, LookupsRepository>();
builder.Services.AddScoped<ICommentsRepository, CommentsRepository>();
builder.Services.AddScoped<ITrashRepository, TrashRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICustomFieldRepository, CustomFieldRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IRolesRepository, RolesRepository>();
builder.Services.AddScoped<IFileManagerRepository, FileManagerRepository>();
builder.Services.AddScoped<ICommentFilterRepository, CommentFilterRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IWidgetsRepository, WidgetsRepository>();

// Configure cookie authentication (replaces FormsAuthentication from Web.Config)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(129600);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".AUXBLOGENGINE";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

builder.Services.AddAuthorization();

// Configure cookie policy (replaces Application_PreSendRequestHeaders SameSite logic)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

// Add localization support
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

// Localized labels (former App_GlobalResources/labels.resx) used by Utils.Translate and the admin.res.axd script
Utils.LabelResources = new System.Resources.ResourceManager("BlogEngine.NET.Resources.labels", typeof(Program).Assembly);

// Resolve provider singletons eagerly so the static BlogService/Security accessors are initialized
app.Services.GetRequiredService<BlogEngine.Core.Providers.IBlogProvider>();
app.Services.GetRequiredService<BlogEngine.Core.Providers.IBlogFileSystemProvider>();
app.Services.GetRequiredService<SecurityInitializer>();

// Initialize BlogEngine extensions (ported from Global.asax Application_BeginRequest)
var initialized = false;
var initLock = new object();

app.Use(async (context, next) =>
{
    // Port of BlogEngineConfig.Initialize - one-time initialization
    if (!initialized)
    {
        lock (initLock)
        {
            if (!initialized)
            {
                Utils.LoadExtensions();
                initialized = true;
            }
        }
    }

    // Port of BlogEngineConfig.SetCulture (Application_PreRequestHandlerExecute)
    var culture = BlogSettings.Instance.Culture;
    if (!string.IsNullOrEmpty(culture) && !culture.Equals("Auto"))
    {
        CultureInfo defaultCulture = Utils.GetDefaultCulture();
        Thread.CurrentThread.CurrentUICulture = defaultCulture;
        Thread.CurrentThread.CurrentCulture = defaultCulture;
    }

    await next();
});

app.UseCookiePolicy();

// Themes, widgets and extensions keep their legacy /Custom URLs; the folder stays under the
// content root because BlogEngine.Core reads and installs themes there at runtime
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Custom")),
    RequestPath = "/Custom"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages();
app.MapResourceHandler();
app.MapUpdaterEndpoints();
app.MapRazorComponents<BlogEngine.NET.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
