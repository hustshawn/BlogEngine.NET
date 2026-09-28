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
    public class CustomFieldsControllerTests
    {
        private CustomFieldsController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new CustomFieldsController(new FakeCustomFieldsRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void CustomFieldsControllerGet()
        {
            var results = _ctrl.Get();
            Assert.IsTrue(results.Any());
        }

        //[TestMethod]
        //public void CustomFieldsControllerPost()
        //{
        //    var result = _ctrl.Post(new CustomField { CustomType = "test", BlogId = new Guid() });
        //    Assert.IsInstanceOfType(result, typeof(CreatedResult));
        //}

        [TestMethod]
        public void CustomFieldsControllerUpdate()
        {
            var items = new List<CustomField>();
            items.Add(new CustomField { CustomType = "test", BlogId = new System.Guid() });
            var result = _ctrl.Put(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
