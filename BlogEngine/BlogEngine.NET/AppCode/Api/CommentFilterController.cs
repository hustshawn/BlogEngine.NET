using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CommentFilterController : ControllerBase
{
    readonly ICommentFilterRepository repository;

    public CommentFilterController(ICommentFilterRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<CommentFilterItem> Get(int take = 10, int skip = 0, string filter = "", string order = "Filter")
    {
        return repository.Find(take, skip, filter, order);
    }

    public IActionResult Post([FromBody]CommentFilterItem item)
    {
        var result = repository.Add(item);
        if (result == null)
            return NotFound();

        return Created("", result);
    }

    [HttpPut]
    public IActionResult DeleteAll()
    {
        repository.RemoveAll();
        return Ok();
    }

    [HttpPut]
    public IActionResult ProcessChecked([FromBody]List<BlogEngine.Core.Data.Models.Blog> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var action = RouteData.Values["id"]?.ToString();
        if (string.IsNullOrEmpty(action))
            return BadRequest();

        if (action.ToLower() == "delete")
        {
            foreach (var item in items)
            {
                if (item.IsChecked)
                {
                    repository.Remove(item.Id);
                }
            }
        }
        return Ok();
    }
}
