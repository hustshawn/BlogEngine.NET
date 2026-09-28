using BlogEngine.Core;
using BlogEngine.Core.Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BlogRollController : ControllerBase
{
    public BlogRollController()
    {
    }

    public IEnumerable<BlogRollItem> Get()
    {
        var vm = new BlogRollVM();
        return vm.BlogRolls;
    }

    public IActionResult Get(string id)
    {
        Guid gId;
        if(Guid.TryParse(id, out gId))
        {
            return NotFound();
        }
        else
        {
            var vm = new BlogRollVM();
            var result = vm.BlogRolls.Find(b => b.Id == gId);
            return Ok(result);
        }
    }

    public IActionResult Post(BlogRollItem item)
    {
        if (!Security.IsAdministrator)
            return Unauthorized();

        BlogEngine.Core.Providers.BlogService.InsertBlogRoll(item);
        return Created("", item);
    }

    [HttpPut]
    public IActionResult Update([FromBody]BlogRollItem item)
    {
        if (!Security.IsAdministrator)
            return Unauthorized();

        BlogEngine.Core.Providers.BlogService.UpdateBlogRoll(item);
        return Ok();
    }

    private string getUrl(string url)
    {
        if (!string.IsNullOrEmpty(url) && !url.Contains("://"))
            url = "http://" + url;
        return url;
    }

    //private void Resort()
    //{
    //    int sortIndex = -1;
    //    // Re-sort remaining items starting from zero to eliminate any possible gaps.
    //    // Need to cast BlogRollItem.BlogRolls to an array to
    //    // prevent errors with modifying a collection while enumerating it.
    //    foreach (BlogRollItem brItem in BlogRollItem.BlogRolls.ToArray())
    //    {
    //        brItem.SortIndex = ++sortIndex;
    //        brItem.Save();
    //    }
    //}
}