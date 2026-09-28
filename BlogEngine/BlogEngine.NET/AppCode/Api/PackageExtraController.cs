using BlogEngine.Core.Packaging;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlogEngine.NET.AppCode.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackageExtraController : ControllerBase
    {
        public IEnumerable<PackageExtra> Get()
        {
            return Gallery.GetPackageExtras();
        }

        public IActionResult Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return StatusCode((int)HttpStatusCode.ExpectationFailed);

            var result = Gallery.GetPackageExtra(id);
            if (result != null)
                return Ok(result);

            return Ok();
        }
    }
}
