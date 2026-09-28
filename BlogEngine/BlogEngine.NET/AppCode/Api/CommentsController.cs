using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using BlogEngine.Core.Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    readonly ICommentsRepository repository;

    public CommentsController(ICommentsRepository repository)
    {
        this.repository = repository;
    }

    public CommentsVM Get()
    {
        return repository.Get();
    }

    public IActionResult Get(string id)
    {
        var result = repository.FindById(new Guid(id));
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    public IActionResult Post([FromBody]CommentDetail item)
    {
        var result = repository.Add(item);
        if (result == null)
            return StatusCode(304);

        return Created("", result);
    }

    public IActionResult Put([FromBody]CommentItem item)
    {
        repository.Update(item, "update");
        return Ok();
    }

    public IActionResult Delete(string id)
    {
        Guid gId;
        if (Guid.TryParse(id, out gId))
            repository.Remove(gId);
        return Ok();
    }
	
	[HttpPut]
    public IActionResult ProcessChecked([FromBody]List<CommentItem> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var action = HttpContext.Request.RouteValues["id"]?.ToString().ToLowerInvariant();

        foreach (var item in items)
        {
            if (item.IsChecked)
            {
                if (action == "delete")
                {
                    repository.Remove(item.Id);
                }
                else
                {
                    repository.Update(item, action);
                }
            }
        }

        return Ok();
    }

    [HttpPut]
    public IActionResult DeleteAll([FromBody]CommentItem item)
    {
        var action = HttpContext.Request.RouteValues["id"]?.ToString();

        if (action.ToLower() == "pending" || action.ToLower() == "spam")
        {
            repository.DeleteAll(action);
        }
        return Ok();
    }
}
