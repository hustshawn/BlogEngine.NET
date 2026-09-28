# Assessment Report: BlogEngine.NET

## Solution Overview

| Attribute | Value |
|-----------|-------|
| **Solution Name** | BlogEngine.NET |
| **Total Projects** | 3 |
| **Target Framework** | net10.0 |
| **Total Lines of Code** | 82549 |
| **Overall Complexity** | Critical |
| **Total NuGet Packages** | 34 (across all projects) |
| **Incompatible Packages** | 16 |
| **.NET Core Readiness** | Not Ready |
| **Linux Readiness** | Not Ready |

## Executive Summary

**Solution Migration Mode: COMPLEX**

BlogEngine.NET is a .NET Framework 4.5 blogging platform with a layered architecture: **BlogEngine.NET** (web application) → **BlogEngine.Core** (shared library) → **BlogEngine.Tests** (unit tests). All three projects use legacy old-style `.csproj` format with `packages.config` for NuGet dependency management. The web project is a hybrid ASP.NET WebForms + Web API 2 application, with extensive use of `.aspx`, `.ascx`, `.master` pages, custom `IHttpHandler`/`IHttpModule` implementations in the Core library, and a SimpleInjector DI container.

The solution is deeply tied to `System.Web`, `System.Drawing`, `System.ServiceModel`, and ASP.NET WebForms infrastructure including custom `WebControl` derivatives, `MembershipProvider`/`RoleProvider` subclasses, `VirtualPathProvider`-based theming, and `App_GlobalResources` localization. The Core library implements 14+ `IHttpHandler` endpoints and 4 `IHttpModule` pipeline modules, all requiring conversion to ASP.NET Core middleware and minimal API endpoints.

- **1 Critical-complexity project** (BlogEngine.NET) — WebForms + Web API 2 hybrid, 16 .aspx pages, 26 .ascx controls, 6 .master pages, 25 NuGet packages (10 incompatible), SimpleInjector DI, theme engine, App_GlobalResources localization
- **1 High-complexity project** (BlogEngine.Core) — 56,599 LOC class library with deep System.Web/System.Drawing/System.ServiceModel dependencies, 14 IHttpHandlers, 4 IHttpModules, custom MembershipProvider/RoleProvider, WCF service reference, local DLL dependencies (AjaxMin, BlogML, ICSharpCode.SharpZipLib)
- **1 Medium-complexity project** (BlogEngine.Tests) — MSTest-based test project referencing Web API 2 controllers, requires test framework modernization

The primary transformation challenge is the complete rewrite of the WebForms UI layer (50 WebForms files) to Blazor Server, combined with the wholesale replacement of `System.Web` infrastructure in BlogEngine.Core (HttpHandlers → endpoints, HttpModules → middleware, MembershipProvider → ASP.NET Core Identity, ConfigurationManager → IConfiguration/IOptions).

### Key Statistics

| Metric | Count |
|--------|-------|
| Projects requiring format conversion | 3 (legacy to SDK-style) |
| Blocking issues | 0 |
| WebForms pages to migrate | 16 |
| WebForms user controls to migrate | 26 |
| Master pages to migrate | 6 |
| Web API controllers to migrate | 27 |
| HttpHandlers to migrate | 14 |
| HttpModules to migrate | 4 |
| Total estimated changes | 141 |

## Project Analysis Table

| Project | Current Framework | Target | LOC | Packages | Incompatible | Complexity |
|---------|-------------------|--------|-----|----------|--------------|------------|
| BlogEngine.Core | net45 | net10.0 | 56599 | 4 | 2 | High |
| BlogEngine.NET | net45 | net10.0 | 24412 | 25 | 10 | Critical |
| BlogEngine.Tests | net45 | net10.0 | 1538 | 5 | 4 | Medium |

## Cross-Project Package Summary

| Package | Used By | Version(s) | Compatible | Notes |
|---------|---------|------------|------------|-------|
| Newtonsoft.Json | BlogEngine.Core, BlogEngine.NET, BlogEngine.Tests | 6.0.8, 7.0.1 | Yes | UpgradePackage to latest 13.x; supports netstandard2.0 |
| DynamicQuery | BlogEngine.Core, BlogEngine.NET | 1.0 | No | INCOMPATIBLE — net35 only; replace with System.Linq.Dynamic.Core |
| Microsoft.AspNet.WebApi | BlogEngine.NET, BlogEngine.Tests | 5.2.3 | No | INCOMPATIBLE — .NET Framework only; built into ASP.NET Core |
| Microsoft.AspNet.WebApi.Client | BlogEngine.NET, BlogEngine.Tests | 5.2.3 | No | INCOMPATIBLE — replace with built-in ASP.NET Core |
| Microsoft.AspNet.WebApi.Core | BlogEngine.NET, BlogEngine.Tests | 5.2.3 | No | INCOMPATIBLE — replace with built-in ASP.NET Core |
| Microsoft.AspNet.WebApi.WebHost | BlogEngine.NET, BlogEngine.Tests | 5.2.3 | No | INCOMPATIBLE — replace with built-in ASP.NET Core |

## Cross-Project Dependencies

BlogEngine.Tests (Medium)
  - BlogEngine.NET (Critical)
    - BlogEngine.Core (High)
  - BlogEngine.Core (High)

### Recommended Transformation Order (Dependency-First)

1. **BlogEngine.Core** — Leaf library with zero project dependencies; must be migrated first as both other projects depend on it
2. **BlogEngine.NET** — Main web application; depends only on BlogEngine.Core; WebForms→Blazor Server + Web API 2→ASP.NET Core
3. **BlogEngine.Tests** — Depends on both BlogEngine.Core and BlogEngine.NET; migrate last after the code it tests is on net10.0

## Key Findings

1. **WebForms-to-Blazor Server rewrite required**: The web project contains 16 `.aspx` pages, 26 `.ascx` user controls, and 6 `.master` pages — all deeply tied to the WebForms lifecycle (`Page_Load`, `IsPostBack`, `<asp:>` server controls, `ViewState`). These must be converted to Blazor Server components with `@page` routes and `@code` blocks.
2. **Massive System.Web dependency in Core library**: BlogEngine.Core implements 14 `IHttpHandler` classes (SyndicationHandler, FileHandler, ImageHandler, etc.) and 4 `IHttpModule` classes (CompressionModule, UrlRewrite, ReferrerModule, WwwSubDomainModule) directly against `System.Web`. These must be rewritten as ASP.NET Core middleware and minimal API endpoints.
3. **Custom authentication providers need replacement**: The solution uses custom `MembershipProvider` (XmlMembershipProvider, DbMembershipProvider) and `RoleProvider` (XmlRoleProvider, DbRoleProvider) subclasses from `System.Web.Security`. These must be replaced with ASP.NET Core Identity or custom `IAuthenticationHandler` implementations.
4. **System.Drawing usage blocks Linux deployment**: BlogEngine.Core uses `System.Drawing` for image processing. This must be replaced with a cross-platform alternative like `SixLabors.ImageSharp` for Linux readiness.
5. **SimpleInjector DI container**: The web project uses SimpleInjector 3.1.0 with `SimpleInjector.Integration.WebApi`. This should be replaced with the built-in ASP.NET Core DI container, migrating all registrations to `IServiceCollection`.
6. **Local DLL dependencies (non-NuGet)**: Three libraries are referenced as local DLLs from a `lib/` folder — AjaxMin, BlogML, and ICSharpCode.SharpZipLib. AjaxMin and BlogML need modern replacements; SharpZipLib has a NuGet package (SharpZipLib) targeting netstandard2.0+.
7. **All 3 projects require legacy-to-SDK project format conversion**: Every `.csproj` uses the old-style MSBuild format with explicit `<Compile>` items and `packages.config`. All must be converted to SDK-style with `<PackageReference>`.
8. **App_GlobalResources localization**: 37 `.resx` files in `App_GlobalResources/` use the legacy `GlobalResourceProxyGenerator` pattern. These must be migrated to `IStringLocalizer<T>` / `IViewLocalizer` with standard resource file embedding.
9. **WCF Service Reference in Core**: BlogEngine.Core has a WCF Data Service reference (`GalleryServer`) for the package gallery. This must be replaced with `HttpClient`-based API calls.
10. **Theme engine uses WebForms infrastructure**: The theming system relies on `site.master` files, `VirtualPathProvider`, and `ViewEngine` customization — all WebForms/MVC 5 constructs that have no direct equivalent in modern .NET and require an `IFileProvider`/`IViewLocationExpander` replacement.

## External Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| SQL Server / MySQL / SQLite / SQL CE | Database | Multiple database providers via custom `DbBlogProvider`; SQL CE not supported on .NET 10 — must drop or replace with SQLite |
| BlogEngine Gallery Server | Cloud Service | WCF OData service reference for package gallery; replace with HttpClient |
| SMTP (email sending) | Infrastructure | Used for newsletters, comment notifications, contact form; migrate from `System.Net.Mail` to `MailKit` or `SmtpClient` in Core |

## Actionable Next Steps

1. **Phase 1 — Core library modernization** (High risk): Convert BlogEngine.Core to SDK-style net10.0, replace System.Web HttpHandlers/HttpModules with middleware/endpoints, replace System.Drawing with ImageSharp, replace MembershipProvider/RoleProvider with Core Identity abstractions, replace local DLLs with NuGet equivalents.
2. **Phase 2 — Web project migration** (Critical risk): Convert BlogEngine.NET to SDK-style net10.0 Blazor Server app, convert all WebForms pages/controls/masters to Blazor components, convert Web API 2 controllers to ASP.NET Core controllers, replace SimpleInjector with built-in DI, migrate Web.config to appsettings.json, migrate App_GlobalResources to IStringLocalizer.
3. **Phase 3 — Test project migration** (Medium risk): Convert BlogEngine.Tests to SDK-style net10.0 with modern test framework (MSTest SDK or migrate to xUnit/NUnit), update controller test references to ASP.NET Core types.
4. **Phase 4 — Integration validation** (Medium risk): End-to-end testing of blog post creation, commenting, theming, multi-database provider support, and admin SPA functionality.

---

## Per-Project Assessment Details

### BlogEngine.Core

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net45 |
| **Lines of Code** | 56599 |
| **NuGet Packages** | 4 |
| **Project References** | 0 |
| **Complexity** | High |
| **Estimated Changes** | 6 |

#### Package Compatibility (4 packages, 2 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| DynamicQuery | 1.0 | INCOMPATIBLE | ReplacePackage — replace with System.Linq.Dynamic.Core (netstandard2.0+) |
| Microsoft.Web.Xdt | 2.1.1 | COMPATIBLE | UpgradePackage — latest supports netstandard2.0 |
| Newtonsoft.Json | 6.0.8 | COMPATIBLE | UpgradePackage — upgrade to 13.x (netstandard2.0) |
| NuGet.Core | 2.8.2 | INCOMPATIBLE | ReplacePackage — replace with NuGet.Protocol (netstandard2.0+) |

#### Project Dependencies (0)

N/A

#### Legacy Files Inventory (1 files across 1 kinds)

| Kind | Files |
|------|------:|
| `.config` | 1 |

#### Migration Analysis

##### Migration Strategy

1. Convert `BlogEngine.Core.csproj` from old-style MSBuild format to SDK-style `<Project Sdk="Microsoft.NET.Sdk">` targeting `net10.0`. Remove explicit `<Compile>` items, migrate `packages.config` to `<PackageReference>`, delete `Properties/AssemblyInfo.cs` and enable `GenerateAssemblyInfo`.
2. Replace `DynamicQuery` 1.0 (net35 LINQ extension) with `System.Linq.Dynamic.Core` NuGet package. Update `using` statements and any API differences (method signatures are largely compatible).
3. Replace `NuGet.Core` 2.8.2 with `NuGet.Protocol` and `NuGet.Packaging`. The `Services/Packaging/` classes that use NuGet APIs for theme/extension installation must be rewritten against the modern NuGet client SDK.
4. Replace all 14 `IHttpHandler` implementations (in `Web/HttpHandlers/`) with ASP.NET Core minimal API endpoints or middleware. Each handler's `ProcessRequest(HttpContext)` method maps to an endpoint delegate or `IMiddleware.InvokeAsync`.
5. Replace all 4 `IHttpModule` implementations (CompressionModule, UrlRewrite, ReferrerModule, WwwSubDomainModule) with ASP.NET Core middleware registered in `Program.cs`.
6. Replace `System.Drawing` usage in `Services/FileSystem/Image.cs` and `Data/Services/Avatar.cs` with `SixLabors.ImageSharp` for cross-platform image processing.
7. Replace `XmlMembershipProvider`, `DbMembershipProvider`, `XmlRoleProvider`, `DbRoleProvider` (in `Providers/`) with ASP.NET Core Identity or custom `IUserStore<T>`/`IRoleStore<T>` implementations.
8. Replace the `GalleryServer` WCF Data Service reference with typed `HttpClient` calls. Remove the `Service References/` folder.
9. Replace local DLL references: `AjaxMin.dll` → `NUglify` (netstandard2.0), `BlogML.dll` → inline or port the BlogML import/export logic, `ICSharpCode.SharpZipLib.dll` → `SharpZipLib` NuGet package.
10. Replace `ConfigurationManager` / custom `ConfigurationSection` usage in `BlogConfig.cs`, `BlogSettings.cs`, and provider configuration with `IConfiguration` / `IOptions<T>`.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| System.Web pervasion | High | Nearly every namespace in the Core library references System.Web (HttpContext.Current, HttpRuntime, VirtualPathUtility); requires systematic replacement |
| System.Drawing on Linux | High | Image processing will fail on Linux without replacement; ImageSharp is the standard cross-platform alternative |
| Custom provider model | High | MembershipProvider/RoleProvider/BlogProvider abstract classes form the backbone; all concrete implementations and callers must be updated |
| WCF service reference | Medium | GalleryServer OData client is auto-generated; must be manually replaced with HttpClient |
| Thread-static / HttpContext.Current | High | Extensive use of HttpContext.Current throughout the library for user identity, request data, and caching; must be replaced with IHttpContextAccessor injection |

##### Recommendations

1. Begin by creating interface abstractions for the current `HttpContext.Current` usage pattern — introduce `IHttpContextAccessor` wrappers early so dependent code can be migrated incrementally.
2. Replace local DLL references with NuGet packages first, as this is a prerequisite for the SDK-style project conversion.
3. Consider splitting the monolithic `BlogEngine.Core` into smaller focused libraries post-migration (e.g., BlogEngine.Data, BlogEngine.Web.Middleware) to reduce coupling.

##### Cross-Project Impact

BlogEngine.Core is the foundational library — **both** BlogEngine.NET and BlogEngine.Tests depend on it. It must be fully migrated and building on net10.0 before either consumer project can be converted. Changes to public API surfaces (e.g., replacing `HttpContext` parameters with ASP.NET Core equivalents, changing provider base classes) will cascade to all callers in the web and test projects.

---

### BlogEngine.NET

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net45 |
| **Lines of Code** | 24412 |
| **NuGet Packages** | 25 |
| **Project References** | 1 |
| **Complexity** | Critical |
| **Estimated Changes** | 126 |

#### Package Compatibility (25 packages, 10 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| angularjs | 1.3.0 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN instead of NuGet |
| Antlr | 3.5.0.2 | COMPATIBLE | ReplacePackage — build-time dependency of WebGrease/Web.Optimization; remove with Web.Optimization |
| bootstrap | 3.4.1 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/LibMan/CDN |
| DynamicQuery | 1.0 | INCOMPATIBLE | ReplacePackage — replace with System.Linq.Dynamic.Core |
| FontAwesome | 4.4.0 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| jQuery | 2.1.4 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| jQuery.Cookie | 1.4.0 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| jQuery.Validation | 1.14.0 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| json2 | 1.0.2 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot (or remove — modern browsers have native JSON) |
| Microsoft.AspNet.Razor | 3.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core SDK; remove package |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | INCOMPATIBLE | ReplacePackage — replace with WebOptimizer or static file bundling |
| Microsoft.AspNet.WebApi | 5.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core; remove meta-package |
| Microsoft.AspNet.WebApi.Client | 5.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core |
| Microsoft.AspNet.WebApi.Core | 5.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core |
| Microsoft.AspNet.WebApi.WebHost | 5.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core |
| Microsoft.AspNet.WebPages | 3.2.3 | INCOMPATIBLE | ReplacePackage — built into ASP.NET Core |
| Microsoft.Web.Infrastructure | 1.0.0.0 | COMPATIBLE | ReplacePackage — build-time infrastructure; not needed in SDK-style |
| Moment.js | 2.10.6 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| Newtonsoft.Json | 7.0.1 | COMPATIBLE | UpgradePackage — upgrade to 13.x (netstandard2.0) |
| SimpleInjector | 3.1.0 | COMPATIBLE | ReplacePackage — replace with built-in ASP.NET Core DI |
| SimpleInjector.Extensions.ExecutionContextScoping | 3.1.0 | INCOMPATIBLE | ReplacePackage — merged into SimpleInjector; remove when replacing with Core DI |
| SimpleInjector.Integration.WebApi | 3.1.0 | INCOMPATIBLE | ReplacePackage — remove when replacing with Core DI |
| toastr | 2.1.1 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| Twitter.Bootstrap | 3.0.1.1 | COMPATIBLE | ReplacePackage — client-side asset; deliver via wwwroot/CDN |
| WebGrease | 1.6.0 | COMPATIBLE | ReplacePackage — build-time CSS/JS minification tool; remove with Web.Optimization |

#### Project Dependencies (1)

- BlogEngine.Core

#### Legacy Files Inventory (137 files across 7 kinds)

| Kind | Files |
|------|------:|
| `.aspx` | 16 |
| `.ascx` | 26 |
| `.master` | 6 |
| `.asax` | 1 |
| `.asmx` | 1 |
| `.cshtml` | 37 |
| `.config` | 13 |
| `.resx` | 37 |

#### Migration Analysis

##### Migration Strategy

1. Convert `BlogEngine.NET.csproj` from old-style MSBuild format to SDK-style `<Project Sdk="Microsoft.NET.Sdk.Web">` targeting `net10.0`. Remove all explicit `<Compile>` and `<Content>` items; migrate `packages.config` to `<PackageReference>`; delete `Properties/AssemblyInfo.cs`.
2. Convert the 16 `.aspx` WebForms pages to Blazor Server components with `@page` routes. Map `Page_Load` → `OnInitializedAsync`, eliminate `ViewState` and `IsPostBack` patterns, replace `<asp:>` server controls with HTML + Blazor components. Key pages: `default.aspx`, `post.aspx`, `page.aspx`, `archive.aspx`, `search.aspx`, `contact.aspx`, `error.aspx`, `error404.aspx`, `login.aspx`, `register.aspx`, etc.
3. Convert the 26 `.ascx` user controls to Blazor components or partial Razor views. Theme controls (PostView, CommentView, CommentForm, etc.) become Blazor components; admin controls (Settings.ascx, menu.ascx) become Blazor admin components.
4. Convert the 6 `.master` pages to Blazor `_Layout.cshtml` / `MainLayout.razor` files. `ContentPlaceHolder` → `@Body` / `@RenderSection`. Nested masters (account.master, admin.master, site.master) → layout hierarchy.
5. Convert the 27 Web API 2 controllers (in `AppCode/Api/`) from `System.Web.Http.ApiController` to `Microsoft.AspNetCore.Mvc.ControllerBase` with `[ApiController]`. Replace `IHttpActionResult` with `IActionResult`, update `HttpResponseMessage` patterns, and move route configuration from `BlogEngineConfig.cs` to attribute routing / `Program.cs`.
6. Replace SimpleInjector DI container with built-in `IServiceCollection` / `IServiceProvider`. Migrate all `container.Register` calls to `services.AddScoped/Transient/Singleton` in `Program.cs`.
7. Convert `Global.asax` application lifecycle events to `Program.cs` middleware pipeline configuration. Map `Application_Start` → builder configuration, `Application_Error` → exception middleware.
8. Replace `Updater.asmx` SOAP service with a minimal API endpoint.
9. Migrate `Web.config` (`<appSettings>`, `<connectionStrings>`, `<system.web>`) to `appsettings.json` and `IConfiguration`/`IOptions<T>`. Migrate transform files (`Web.Debug.config`, `Web.Release.config`) to environment-specific `appsettings.{env}.json`.
10. Migrate 37 `.resx` files from `App_GlobalResources/` to embedded resources with `IStringLocalizer<T>`. Remove the `GlobalResourceProxyGenerator` auto-generated `.designer.cs` files.
11. Move static assets (Scripts/, Content/, fonts/, Custom/Themes/*/src/) to `wwwroot/`. Remove NuGet-delivered client-side packages (jQuery, bootstrap, AngularJS, etc.) and deliver via `wwwroot/lib/` or CDN.
12. Convert the 37 `.cshtml` Razor views (admin templates, widget views, theme pages) from WebPages Razor syntax to ASP.NET Core Razor / Blazor component syntax. Replace `@Html.Raw`, `WebPageBase`, `@Url.Content("~/...")` patterns.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| WebForms-to-Blazor conversion | Critical | 50 WebForms files with deep lifecycle dependencies (Page_Load, IsPostBack, ViewState, FindControl); requires complete rewrite |
| Admin SPA (AngularJS 1.3) | High | The admin area is an AngularJS SPA loaded via cshtml pages; AngularJS is end-of-life — needs a decision: keep as static JS in wwwroot or rewrite in Blazor |
| Theme engine architecture | High | Themes use .master + .ascx files loaded dynamically by VirtualPathProvider; requires IFileProvider/IViewLocationExpander replacement or Blazor component-based theming |
| SimpleInjector → Core DI | Medium | All service registrations and Web API dependency resolution must be migrated; verify no SimpleInjector-specific features (decorators, open generics) are in use |
| Custom WebControls | High | `AppCode/Controls/` contains server controls (WidgetBase, PostCalendar, SearchBox, etc.) inheriting from `System.Web.UI.WebControl`; must be rewritten as Blazor components or Tag Helpers |
| Multi-database provider support | Medium | The application supports SQL Server, MySQL, SQLite, and SQL CE via custom providers; SQL CE is not available on .NET 10 |

##### Recommendations

1. Keep the AngularJS admin SPA as-is in `wwwroot/admin/` initially — it communicates with the backend via Web API JSON endpoints. Convert the API controllers first; the SPA JavaScript will work unchanged once the API endpoints are equivalent.
2. Prioritize converting the public-facing pages (default, post, page, archive, search, contact) to Blazor Server first, then tackle admin/account pages.
3. Create a Blazor `MainLayout.razor` that replicates the Standard theme's `site.master` structure; wire up `@Body` and `@RenderSection` equivalents.

##### Cross-Project Impact

BlogEngine.NET depends on BlogEngine.Core; it cannot be converted until Core is on net10.0. Once BlogEngine.NET's Web API controllers are migrated from `System.Web.Http.ApiController` to `ControllerBase`, the test project's controller instantiation and assertion code will need corresponding updates.

---

### BlogEngine.Tests

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net45 |
| **Lines of Code** | 1538 |
| **NuGet Packages** | 5 |
| **Project References** | 2 |
| **Complexity** | Medium |
| **Estimated Changes** | 9 |

#### Package Compatibility (5 packages, 4 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| Microsoft.AspNet.WebApi | 5.2.3 | INCOMPATIBLE | ReplacePackage — remove; built into ASP.NET Core |
| Microsoft.AspNet.WebApi.Client | 5.2.3 | INCOMPATIBLE | ReplacePackage — remove; built into ASP.NET Core |
| Microsoft.AspNet.WebApi.Core | 5.2.3 | INCOMPATIBLE | ReplacePackage — remove; built into ASP.NET Core |
| Microsoft.AspNet.WebApi.WebHost | 5.2.3 | INCOMPATIBLE | ReplacePackage — remove; built into ASP.NET Core |
| Newtonsoft.Json | 6.0.8 | COMPATIBLE | UpgradePackage — upgrade to 13.x |

#### Project Dependencies (2)

- BlogEngine.Core
- BlogEngine.NET

#### Legacy Files Inventory (2 files across 1 kinds)

| Kind | Files |
|------|------:|
| `.config` | 2 |

#### Migration Analysis

##### Migration Strategy

1. Convert `BlogEngine.Tests.csproj` from old-style MSBuild format to SDK-style `<Project Sdk="Microsoft.NET.Sdk">` targeting `net10.0`. Remove the `ProjectTypeGuids` for MSTest, migrate `packages.config` to `<PackageReference>`, delete `Properties/AssemblyInfo.cs`.
2. Replace MSTest (`Microsoft.VisualStudio.QualityTools.UnitTestFramework`) with MSTest SDK packages (`Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework`) or migrate to xUnit/NUnit. The project uses `[TestClass]` / `[TestMethod]` attributes throughout the 13 test files.
3. Remove all four `Microsoft.AspNet.WebApi.*` packages. Update the 13 controller test classes (in `WebApi/`) to instantiate ASP.NET Core `ControllerBase`-derived controllers instead of `ApiController`-derived ones. Update `System.Web.Http` using statements to `Microsoft.AspNetCore.Mvc`.
4. Upgrade `Newtonsoft.Json` from 6.0.8 to latest 13.x.
5. Update fake repository classes (13 files in `Fakes/`) if the Core library's repository interfaces changed during migration.
6. Remove `app.config` (binding redirects no longer needed in SDK-style projects).

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| Controller API surface changes | Medium | All 13 test classes directly instantiate Web API 2 controllers; when these become ASP.NET Core controllers, constructor signatures and return types will change |
| Test framework modernization | Low | MSTest migration to SDK-style is straightforward; attribute names remain the same |
| Fake repositories | Low | The 13 fake repository classes implement Core interfaces; if interface signatures change during Core migration, fakes must be updated accordingly |

##### Recommendations

1. Migrate the test project last, after both BlogEngine.Core and BlogEngine.NET compile successfully on net10.0.
2. Consider introducing `WebApplicationFactory<T>` integration tests to validate the full ASP.NET Core pipeline, supplementing the existing unit tests.
3. Add a test that verifies the DI container resolves all registered services, catching missing registrations early.

##### Cross-Project Impact

BlogEngine.Tests depends on both BlogEngine.Core and BlogEngine.NET. It is the terminal node in the dependency graph and must be migrated last. It has no downstream dependents, so its migration does not block any other project.
