using BlogEngine.Core;
using BlogEngine.Core.Data;
using BlogEngine.Core.Data.Contracts;
using BlogEngine.NET.AppCode.Api;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add Razor Components (Blazor Server) with interactive server-side rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MVC controllers with Newtonsoft.Json for API compatibility
builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapUpdaterEndpoints();
app.MapRazorComponents<BlogEngine.NET.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
