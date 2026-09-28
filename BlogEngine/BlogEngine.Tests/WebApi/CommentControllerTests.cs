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
    public class CommentControllerTests
    {
        private CommentsController _ctrl;

        [TestInitialize]
        public void Init()
        {
            _ctrl = new CommentsController(new FakeCommentRepository());
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData()
            };
        }

        [TestMethod]
        public void CommentControllerGet()
        {
            var results = _ctrl.Get();
            Assert.IsNotNull(results);
        }

        [TestMethod]
        public void CommentControllerGetById()
        {
            var item = _ctrl.Get("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void CommentControllerPost()
        {
            var result = _ctrl.Post(new CommentDetail { Id = Guid.NewGuid() });
            Assert.IsInstanceOfType(result, typeof(CreatedResult));
        }

        [TestMethod]
        public void CommentControllerPut()
        {
            var result = _ctrl.Put(new CommentItem { Id = Guid.NewGuid() });
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void CommentControllerDelete()
        {
            var result = _ctrl.Delete("96d5b379-7e1d-4dac-a6ba-1e50db561b04");
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }

        [TestMethod]
        public void CommentControllerProcessChecked()
        {
            _ctrl.ControllerContext.RouteData.Values["id"] = "delete";

            var items = new List<CommentItem>();
            items.Add(new CommentItem()
            {
                IsChecked = true,
                Id = Guid.NewGuid()
            });

            var result = _ctrl.ProcessChecked(items);
            Assert.IsInstanceOfType(result, typeof(OkResult));
        }
    }
}
