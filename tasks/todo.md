# BlogEngine.NET — finish ATX .NET 10 migration

Goal: ATX output builds but the app is not usable. Make it fully working on .NET 10.

## Success criteria
- [x] `dotnet build` 0 errors / 0 warnings; `dotnet test` all green (76: 61 unit + 15 integration)
- [x] Front end: home post list, post, page, category, tag, archive, search, contact, login render with theme assets
- [x] API: every URL used by the admin SPA routes to the same action as Web API 2 did
- [x] Admin: /admin loads, login works, post CRUD works end-to-end (browser)
- [x] Works in Production environment (Release publish output, browser E2E 36/36)
- [x] ATX nextsteps items: /setup protection, System.Linq.Dynamic.Core conflict, ProcessChecked null guard

## Plan
1. [x] Startup: call AddBlogEngineCore, init static providers, drop duplicate cookie scheme
2. [x] Static assets: legacy URL layout in wwwroot, /Custom from content root, MapStaticAssets
3. [x] API: Web API 2 compat routing, JSON input/output parity, global filters, audit fixes
4. [x] Front end parity (subagent): static SSR, widgets, themes, comments, feeds/handlers
5. [x] Admin (subagent): admin cshtml -> Razor Pages; extension settings as SSR forms
6. [x] Security (subagent): single cookie sign-in, per-request roles, /setup, per-blog cookies
7. [x] Packages: pinned versions, MSB3277 resolved, no vulnerable packages
8. [x] E2E verification: XML + SQL Server providers, Development + Production publish
9. [x] Child blogs under virtual paths, former HttpModules wired
10. [x] Integration tests for the compat layer (mutation-checked)
11. [ ] Independent code review, address findings

## Review
- ATX left: unregistered Core services, camelCase JSON, no Web API routing, interactive Blazor that cannot
  use HttpContext, no extensions loaded, config keys ignored, admin not ported, MetaWeblog broken,
  unauthenticated updater endpoints, path traversal, version 1.0.0.0.
- Known upstream (3.3.8) quirks kept for parity: widgets page iframe requests /admin/{}; posts without an
  image reference a missing Custom/Themes/<theme>/images/defaultImg.jpg; SQL Server sample data has no
  "sidebar Post" widget zone for the Standard theme.
- Not verified: MySQL / SQLite providers (need provider factory registration), reCAPTCHA (needs keys).
