using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace BlogEngine.Tests.Integration
{
    /// <summary>
    /// Runs the whole application in-process against a copy of the sample XML data and checks the
    /// behaviour the .NET 10 migration had to restore: Web API 2 routing/binding/JSON, SSR sign-in,
    /// front-end pages and access rules.
    /// </summary>
    [TestClass]
    public class WebAppTests
    {
        static BlogEngineFactory _factory;
        static HttpClient _anonymous;
        static HttpClient _admin;

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            _factory = new BlogEngineFactory();
            _anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await SignIn(_admin, "admin", "admin");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _anonymous?.Dispose();
            _admin?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task HomePageListsSamplePost()
        {
            var html = await _anonymous.GetStringAsync("/");
            StringAssert.Contains(html, "Welcome to BlogEngine.NET");
        }

        [TestMethod]
        public async Task PostPageRenders()
        {
            var response = await _anonymous.GetAsync("/post/welcome-to-blogengine-net");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), "the hard part of creating your own blog is done");
        }

        [TestMethod]
        public async Task UnknownUrlReturns404Page()
        {
            var response = await _anonymous.GetAsync("/no-such-page");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task ApiRequiresAuthentication()
        {
            var response = await _anonymous.GetAsync("/api/settings");
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task SourceAndSetupFilesAreNotServed()
        {
            foreach (var url in new[] { "/Custom/Themes/Standard/site.cshtml", "/App_Data/users.xml", "/setup/SQLServer/Setup.sql" })
            {
                var response = await _anonymous.GetAsync(url);
                Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, url);
            }
        }

        [TestMethod]
        public async Task LegacyAutoUpgradeEndpointIsGone()
        {
            var response = await _admin.PostAsync("/api/updater/delete", new StringContent(""));
            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task JsonUsesPascalCaseLikeWebApi2()
        {
            var json = JArray.Parse(await _admin.GetStringAsync("/api/posts?take=1"));
            Assert.IsNotNull(json[0]["Title"]);
            Assert.IsNull(json[0]["title"]);
        }

        [TestMethod]
        public async Task ActionRouteSelectsMethodByName()
        {
            // api/{controller}/{action}/{id}
            var role = JObject.Parse(await _admin.GetStringAsync("/api/roles/get/Administrators"));
            Assert.AreEqual("Administrators", (string)role["RoleName"]);

            var rights = JToken.Parse(await _admin.GetStringAsync("/api/roles/getrights/Administrators"));
            Assert.IsInstanceOfType(rights, typeof(JArray));
        }

        [TestMethod]
        public async Task QueryStringIdSelectsSingleItemOverload()
        {
            // Web API 2 picked Get(string id) when "id" came from the query string
            var single = JToken.Parse(await _admin.GetStringAsync("/api/users?id=admin"));
            Assert.IsInstanceOfType(single, typeof(JObject));

            var list = JToken.Parse(await _admin.GetStringAsync("/api/users"));
            Assert.IsInstanceOfType(list, typeof(JArray));
        }

        [TestMethod]
        public async Task StringResultsAreJson()
        {
            var response = await _admin.GetAsync("/api/setup?version=3.3.8");
            Assert.AreEqual("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.AreEqual("\"\"", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task CreatePostToleratesEmptyIdLikeWebApi2()
        {
            // the admin editor posts "Id": "" and "Comments": "" for new posts
            var body = "{\"Id\":\"\",\"Title\":\"Integration test post\",\"Content\":\"<p>x</p>\",\"DateCreated\":\"2026-01-02 03:04\","
                + "\"Slug\":\"integration-test-post\",\"Categories\":[],\"Tags\":[],\"Comments\":\"\",\"HasCommentsEnabled\":true,\"IsPublished\":true}";
            var response = await _admin.PostAsync("/api/posts", new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());

            var created = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreNotEqual(Guid.Empty, Guid.Parse((string)created["Id"]));

            var delete = await _admin.DeleteAsync("/api/posts/" + created["Id"]);
            Assert.AreEqual(HttpStatusCode.OK, delete.StatusCode);
        }

        [TestMethod]
        public async Task ActionQueryParameterIsNotTheRouteAction()
        {
            // PUT api/settings?action=clearCache must reach Put(settings, action) with action=clearCache
            var settings = await _admin.GetStringAsync("/api/settings");
            var response = await _admin.PutAsync("/api/settings?action=clearCache", new StringContent(settings, Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task SignOutEndsSession()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            await SignIn(client, "admin", "admin");
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/users")).StatusCode);

            await client.GetAsync("/Account/logout");
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);
        }

        [TestMethod]
        public async Task WrongPasswordIsRejected()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var response = await PostLogin(client, "admin", "wrong");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode); // form shown again
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);
        }

        static async Task SignIn(HttpClient client, string user, string password)
        {
            var response = await PostLogin(client, user, password);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "login should redirect");
        }

        static async Task<HttpResponseMessage> PostLogin(HttpClient client, string user, string password)
        {
            var form = await client.GetStringAsync("/Account/login");
            var token = Regex.Match(form, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;
            return await client.PostAsync("/Account/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["_handler"] = "login",
                ["Input.UserName"] = user,
                ["Input.Password"] = password
            }));
        }

        /// <summary>Hosts the app on a throwaway copy of the content root so tests never touch the sample data.</summary>
        sealed class BlogEngineFactory : WebApplicationFactory<Program>
        {
            readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "blogengine-tests-" + Guid.NewGuid().ToString("N"));

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                var source = FindWebProject();
                foreach (var dir in new[] { "App_Data", "Custom", "setup", "wwwroot" })
                    CopyDirectory(Path.Combine(source, dir), Path.Combine(_contentRoot, dir));
                foreach (var file in new[] { "appsettings.json", "robots.txt" })
                    File.Copy(Path.Combine(source, file), Path.Combine(_contentRoot, file));

                builder.UseContentRoot(_contentRoot);
                builder.UseEnvironment("Production");
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                try { Directory.Delete(_contentRoot, true); } catch (IOException) { }
            }

            static string FindWebProject()
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BlogEngine.NET", "BlogEngine.NET.csproj")))
                    dir = dir.Parent;
                return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("BlogEngine.NET project"), "BlogEngine.NET");
            }

            static void CopyDirectory(string from, string to)
            {
                foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
                {
                    var target = Path.Combine(to, Path.GetRelativePath(from, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(file, target);
                }
            }
        }
    }
}
