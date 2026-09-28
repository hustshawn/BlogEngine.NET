using BlogEngine.Core.Data.Models;
using BlogEngine.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace BlogEngine.Tests.WebApi
{
    [TestClass]
    public class UserControllerTests
    {
        private UsersController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new UsersController(new FakeUsersRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void UsersControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void UsersControllerGetById()
        {
            var item = _ctrl.Get("test");
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void UsersControllerPost()
        {
            var result = _ctrl.Post(new BlogUser());
            Assert.IsInstanceOfType(result, typeof(CreatedResult));

            var createdResult = (CreatedResult)result;
            var item = createdResult.Value as BlogUser;
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void UsersControllerUpdate()
        {
            var result = _ctrl.Update(new BlogUser());
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void UsersControllerDelete()
        {
            var result = _ctrl.Delete("test");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void UsersControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<BlogUser>();
            items.Add(new BlogUser());

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
