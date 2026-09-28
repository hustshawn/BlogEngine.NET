using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using BlogEngine.Core.Packaging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using Microsoft.AspNetCore.Http;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    readonly IPackageRepository repository;

    public PackagesController(IPackageRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<Package> Get(int take = 10, int skip = 0, string filter = "", string order = "")
    {
        return repository.Find(take, skip, filter, order);
    }

    public IActionResult Get(string id)
    {
        var result = repository.FindById(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPut]
    public IActionResult ProcessChecked([FromBody]List<Package> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var action = HttpContext.Request.RouteValues["id"]?.ToString();

        try
        {
            if (action == "install")
            {
                foreach (var item in items)
                {
                    if (!repository.Install(item.Id))
                        return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
            if (action == "uninstall")
            {
                foreach (var item in items)
                {
                    if (!repository.Uninstall(item.Id))
                        return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }

            if (action == "enable")
            {
                foreach (var item in items)
                {
                    item.Enabled = true;
                    repository.Update(item);
                }
            }
            if (action == "disable")
            {
                foreach (var item in items)
                {
                    item.Enabled = false;
                    repository.Update(item);
                }
            }
            return Ok();
        }
        catch (System.Exception ex)
        {
            BlogEngine.Core.Utils.Log("Error processing package", ex);
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPut]
    public IActionResult Update([FromBody]Package item)
    {
        try
        {
            repository.Update(item);
            return Ok(true);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode((int)HttpStatusCode.Unauthorized);
        }
        catch (Exception ex)
        {
            BlogEngine.Core.Utils.Log("Error updating package", ex);
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPut]
    public IActionResult Install(string id)
    {
        if (string.IsNullOrEmpty(id))
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        repository.Install(id);
        return Ok();
    }

    [HttpPut]
    public IActionResult Uninstall(string id)
    {
        if (string.IsNullOrEmpty(id))
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        repository.Uninstall(id);
        return Ok();
    }

    [HttpPut]
    public IActionResult Rate(string id, [FromBody]Review review)
    {
        if (review == null) return StatusCode(417, "Review can not be null");

        if (review.Body.Length > 450) review.Body = review.Body.Substring(0, 450);

        var result = Gallery.RatePackage(id, review);

        return Ok(result);
    }

    [HttpPut]
    public IActionResult Refresh()
    {
        try
        {
            BlogEngine.Core.Blog.CurrentInstance.Cache.Remove(Constants.CacheKey);
            return Ok(true);
        }
        catch (Exception ex)
        {
            BlogEngine.Core.Utils.Log("Error refreshing gallery cache", ex);
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPut]
    public IActionResult SetTheme(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var bs = BlogEngine.Core.BlogSettings.Instance;
            bs.Theme = id;
            bs.Save();
        }
        return Ok();
    }
}
