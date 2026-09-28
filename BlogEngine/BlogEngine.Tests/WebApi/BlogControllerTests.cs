using BlogEngine.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlogEngine.Tests.WebApi
{
    [TestClass]
    public class BlogControllerTests
    {
        private BlogsController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new BlogsController(new FakeBlogRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void BlogControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void BlogControllerGetById()
        {
            var blog = _ctrl.Get("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsNotNull(blog);
        }

        [TestMethod]
        public void BlogControllerPost()
        {
            var newBlog = new Core.Data.Models.BlogItem();

            var result = _ctrl.Post(newBlog);
            Assert.IsInstanceOfType(result, typeof(CreatedResult));

            var createdResult = (CreatedResult)result;
            var blog = createdResult.Value as BlogEngine.Core.Data.Models.Blog;

            Assert.IsNotNull(blog);
            Assert.IsTrue(blog.Id != Guid.Empty && blog.Id != null);
        }

        [TestMethod]
        public void BlogControllerUpdate()
        {
            var result = _ctrl.Update(new BlogEngine.Core.Data.Models.Blog()
            {
                Id = Guid.NewGuid()
            });
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void BlogControllerDelete()
        {
            var result = _ctrl.Delete("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void BlogControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<BlogEngine.Core.Data.Models.Blog>();
            items.Add(new BlogEngine.Core.Data.Models.Blog()
            {
                IsChecked = true,
                Id = Guid.NewGuid()
            });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
