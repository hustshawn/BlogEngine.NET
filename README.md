<br>
<p>
    <strong>BlogEngine</strong> is an open source blogging platform since 2007.<br>Easily customizable. Many free built-in Themes, Widgets, and Plugins.
</p>
<br>

- **[Website](https://blogengine.io/)**
- **[Docs](https://blogengine.io/support/get-started/)**
- **[Themes](https://blogengine.io/themes/)**
- **[Custom Design Theme](https://blogengine.io/themes/custom/)**
- **[Contact us](https://blogengine.io/support/)**
  <br>
  <br>

## Get Started

BlogEngine runs on **.NET 10** (ASP.NET Core) on Linux, macOS or Windows.

1. Requirements
   The [ASP.NET Core 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or the SDK to build from source).

2. Build and publish

   ```bash
   dotnet publish BlogEngine/BlogEngine.NET/BlogEngine.NET.csproj -c Release -o ./publish
   ```

3. Write Permissions
   The process needs write access to the `App_Data` and `Custom` folders of the published output
   (blog data, uploads, themes and extension settings). Keep `App_Data` on persistent storage.

4. Run

   ```bash
   cd publish
   dotnet BlogEngine.NET.dll --urls http://0.0.0.0:8080
   ```

   On IIS, install the ASP.NET Core Hosting Bundle; the publish output contains the `web.config` for it.

5. Done
   Navigate to the admin panel by adding /admin/ to your website's URL,
   for example https://yourwebsite.com/admin/<br>
   Username: admin<br>
   Password: admin (change it right away)
   <br><br>

## Configuration

Settings live in `appsettings.json` (or environment variables such as `BlogEngine__DefaultProvider`):

- `BlogEngine:DefaultProvider`: `XmlBlogProvider` (default, files in App_Data) or `DbBlogProvider` (SQL Server);
  see `setup/ReadMe.txt` and `setup/SQLServer/ReadMe.txt`.
- `BlogEngine:FileSystemProvider`: storage for uploaded files, `XmlBlogProvider` or `DbBlogProvider`.
- `ConnectionStrings:BlogEngine`: database connection string for `DbBlogProvider`.
- Other former `web.config` app settings keep their names without the `BlogEngine.` prefix,
  e.g. `BlogEngine:UsageScenario`, `BlogEngine:SingleSignOn`.

Child blogs are served under their virtual path (e.g. `/blog2/`) like before.

## Development

Environment:

- .NET 10 SDK
- Any editor (Visual Studio 2026, VS Code, Rider)

Steps:

```bash
git clone <this repository>
cd BlogEngine.NET/BlogEngine
dotnet build BlogEngine.sln
dotnet test BlogEngine.sln                  # unit + in-process integration tests
cd BlogEngine.NET && dotnet run             # http://localhost:5000, admin at /admin/
```

## Security

The auth and antiforgery cookies are protected by the ASP.NET Core Data Protection key ring, stored in
`App_Data/DataProtection-Keys` (this replaces `machineKey` in `Web.config`). It is created on first start,
is excluded from source control and from publish output, and must be kept private. When running several
instances behind a load balancer, share that folder (or configure another Data Protection key store).
<br><br>

## Migration notes

This version was migrated from .NET Framework 4.5 (WebForms + Web API 2) to .NET 10 with AWS Transform
and manual completion. The public site renders with Blazor static SSR, account pages are SSR forms, the
admin SPA and its JSON API behave as before. The in-place auto-upgrade (`setup/upgrade`) was removed:
upstream releases are .NET Framework builds. SQL CE is no longer supported. Reports from the migration are
in `artifacts/`.
<br><br>

## Copyright and License

Code released under the MS-RL License. Docs released under Creative Commons.<br>
Copyright 2007–2023 BlogEngine
