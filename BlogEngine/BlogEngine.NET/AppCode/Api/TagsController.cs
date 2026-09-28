using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    readonly ITagRepository repository;

    public TagsController(ITagRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<TagItem> Get(int take = 10, int skip = 0, string postId = "", string order = "")
    {
        return repository.Find(take, skip, postId, order);
    }

    [HttpPut]
    public IActionResult Update([FromBody]TagToUpdate tag)
    {
        repository.Save(tag.OldTag, tag.NewTag);
        return Ok();
    }

    public IActionResult Delete(string id)
    {
        repository.Delete(id);
        return Ok();
    }
	
	[HttpPut]
    public IActionResult ProcessChecked([FromBody]List<TagItem> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var action = RouteData.Values["id"]?.ToString().ToLowerInvariant();
        if (string.IsNullOrEmpty(action))
            return BadRequest();

        if (action == "delete")
        {
            foreach (var item in items)
            {
                if (item.IsChecked)
                {
                    repository.Delete(item.TagName);
                }
            }
        }
        return Ok();
    }
}

public class TagToUpdate
{
    public string OldTag { get; set; }
    public string NewTag { get; set; }
}
