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
    public class RoleControllerTests
    {
        private RolesController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new RolesController(new FakeRolesRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void RoleControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void RoleControllerGetById()
        {
            var item = _ctrl.Get("test");
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void RoleControllerPost()
        {
            var result = _ctrl.Post(new RoleItem { RoleName = "test" });
            Assert.IsInstanceOfType(result, typeof(CreatedResult));

            var createdResult = (CreatedResult)result;
            var item = createdResult.Value as RoleItem;

            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void RoleControllerUpdate()
        {
            var items = new List<RoleItem>();
            items.Add(new RoleItem());
            var result = _ctrl.Put(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void RoleControllerDelete()
        {
            var result = _ctrl.Delete("test");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void RoleControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<RoleItem>();
            items.Add(new RoleItem());

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void RoleControllerGetRights()
        {
            var result = _ctrl.GetRights("test");
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void RoleControllerGetUserRiles()
        {
            var result = _ctrl.GetUserRoles("test");
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void RoleControllerSaveRights()
        {
            var rights = new List<Group>();
            var result = _ctrl.SaveRights(rights, "test");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
