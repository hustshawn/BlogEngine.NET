using App_Code;
using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    readonly IPostRepository repository;

    public PostsController(IPostRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<PostItem> Get(int take = 10, int skip = 0, string filter = "", string order = "")
    {
        return repository.Find(take, skip, filter, order);
    }

    public IActionResult Get(string id)
    {
        var result = repository.FindById(new Guid(id));
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    public IActionResult Post([FromBody]PostDetail item)
    {
        var result = repository.Add(item);
        if (result == null)
            return StatusCode(304);

        return Created("", result);
    }

    [HttpPut]
    public IActionResult Update([FromBody]PostDetail item)
    {
        repository.Update(item, "update");
        return Ok();
    }
	
	[HttpPut]
    public IActionResult ProcessChecked([FromBody]List<PostDetail> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var action = RouteData.Values["id"]?.ToString().ToLowerInvariant();
        if (string.IsNullOrEmpty(action))
            return BadRequest();
      
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

    public IActionResult Delete(string id)
    {
        Guid gId;
        if (Guid.TryParse(id, out gId))
            repository.Remove(gId);

        return Ok();
    }
}