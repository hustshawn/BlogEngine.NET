# BlogEngine.NET — finish ATX .NET 10 migration

Goal: ATX output builds but the app is not usable. Make it fully working on .NET 10.

## Success criteria
- [ ] `dotnet build` 0 errors / 0 warnings; `dotnet test` 61/61
- [ ] Front end: home post list, post, page, category, tag, archive, search, contact, login render with theme assets
- [ ] API: every URL used by the admin SPA routes to the same action as Web API 2 did
- [ ] Admin: /admin loads, login works, post CRUD works end-to-end
- [ ] Works in Production environment (not only Development)
- [ ] ATX nextsteps items: /setup protection, System.Linq.Dynamic.Core conflict, ProcessChecked null guard

## Plan
1. [x] Startup: call AddBlogEngineCore, init static providers, drop duplicate cookie scheme
2. [ ] Static assets: serve legacy root folders (Custom, Scripts, Content, fonts, admin assets); fix relative URLs; MapStaticAssets for Production
3. [ ] API: Web API 2 compat routing convention (verb-by-name, {action}/{id}, parameter-based selection), global exception filter, ProcessChecked guard, fix 3 tests
4. [ ] Front end parity: home post list, widgets, category/tag/search listing
5. [ ] Admin: port admin/*.cshtml (layout, index, editors, about) to ASP.NET Core Razor
6. [ ] Security: protect /setup, admin + API auth parity
7. [ ] Packages: resolve MSB3277
8. [ ] E2E verification (Development + Production): login, create/edit/delete post, comment

## Review
