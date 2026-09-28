using BlogEngine.Core.Data.Models;
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
    public class CategoryControllerTests
    {
        private CategoriesController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new CategoriesController(new FakeCategoryRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void CategoryControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void CategoryControllerGetById()
        {
            var blog = _ctrl.Get("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsNotNull(blog);
        }

        [TestMethod]
        public void CategoryControllerPost()
        {
            var result = _ctrl.Post(new CategoryItem { Id = Guid.NewGuid() });
            Assert.IsInstanceOfType(result, typeof(CreatedResult));

            var createdResult = (CreatedResult)result;
            var item = createdResult.Value as CategoryItem;

            Assert.IsNotNull(item);
            Assert.AreNotEqual(Guid.Empty, item.Id);
        }

        [TestMethod]
        public void CategoryControllerUpdate()
        {
            var result = _ctrl.Update(new CategoryItem()
            {
                Id = Guid.NewGuid()
            });
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void CategoryControllerDelete()
        {
            var result = _ctrl.Delete("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void CategoryControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<CategoryItem>();
            items.Add(new CategoryItem()
            {
                IsChecked = true,
                Id = Guid.NewGuid()
            });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
