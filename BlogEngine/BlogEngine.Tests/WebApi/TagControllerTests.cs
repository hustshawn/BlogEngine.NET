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
    public class TagControllerTests
    {
        private TagsController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new TagsController(new FakeTagsRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void TagsControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void TagsControllerUpdate()
        {
            var result = _ctrl.Update(new TagToUpdate());
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void TagsControllerDelete()
        {
            var result = _ctrl.Delete("test");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void TagsControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<TagItem>();
            items.Add(new TagItem()
            {
                IsChecked = true,
                TagName = "test"
            });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
