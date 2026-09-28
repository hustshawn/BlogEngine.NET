using BlogEngine.Core.Data.Models;
using BlogEngine.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BlogEngine.Tests.WebApi
{
    [TestClass]
    public class TrashControllerTests
    {
        private TrashController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new TrashController(new FakeTrashRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void TrashControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void TrashControllerPurge()
        {
            var result = _ctrl.Purge(new TrashItem());
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void TrashControllerPurgeAll()
        {
            var result = _ctrl.PurgeAll();
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void TrashControllerRestore()
        {
            var result = _ctrl.Restore(new TrashItem());
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
