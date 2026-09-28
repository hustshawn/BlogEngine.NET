using BlogEngine.Core.Data.Models;
using BlogEngine.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;

namespace BlogEngine.Tests.WebApi
{
    [TestClass]
    public class PackagesControllerTests
    {
        private PackagesController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new PackagesController(new FakePackagesRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void PackagesControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void PackagesControllerGetById()
        {
            var item = _ctrl.Get("test");
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void PackageControllerUpdate()
        {
            var result = _ctrl.Update(new Package() { Id = "test" });
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result;
            Assert.AreEqual(true, okResult.Value);
        }

        [TestMethod]
        public void PackagesControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "install";

            var items = new List<Package>();
            items.Add(new Package() { Id = "test" });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
