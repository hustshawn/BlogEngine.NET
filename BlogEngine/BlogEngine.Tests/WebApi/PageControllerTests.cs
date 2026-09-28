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
    public class PageControllerTests
    {
        private PagesController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new PagesController(new FakePageRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void PageControllerGet()
        {
            var results = _ctrl.Get(0, 0);
            Assert.IsTrue(results.Any());
        }

        [TestMethod]
        public void PageControllerGetById()
        {
            var item = _ctrl.Get("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void PageControllerPost()
        {
            var result = _ctrl.Post(new PageDetail { Id = Guid.NewGuid() });
            Assert.IsInstanceOfType(result, typeof(CreatedResult));

            var createdResult = (CreatedResult)result;
            var item = createdResult.Value as PageDetail;

            Assert.IsNotNull(item);
            Assert.AreNotEqual(Guid.Empty, item.Id);
        }

        [TestMethod]
        public void PageControllerUpdate()
        {
            var result = _ctrl.Update(new PageDetail()
            {
                Id = Guid.NewGuid()
            });
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void PageControllerDelete()
        {
            var result = _ctrl.Delete("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void PageControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<PageDetail>();
            items.Add(new PageDetail()
            {
                IsChecked = true,
                Id = Guid.NewGuid()
            });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
