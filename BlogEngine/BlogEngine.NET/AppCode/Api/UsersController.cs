using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    readonly IUsersRepository repository;

    public UsersController(IUsersRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<BlogUser> Get(int take = 10, int skip = 0, string filter = "1 == 1", string order = "UserName")
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

    public IActionResult Post([FromBody]BlogUser item)
    {
        var result = repository.Add(item);
        if (result == null)
            return StatusCode(304);

        return Created("", result);
    }

    [HttpPut]
    public IActionResult Update([FromBody]BlogUser item)
    {
        repository.Update(item);
        return Ok();
    }

    [HttpPut]
    public IActionResult SaveProfile([FromBody]BlogUser item)
    {
        repository.SaveProfile(item);
        return Ok();
    }

    [HttpPut]
    public IActionResult ProcessChecked([FromBody]List<BlogUser> items)
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
                    repository.Remove(item.UserName);
                }
            }
        }
        return Ok();
    }

    public IActionResult Delete(string id)
    {
        repository.Remove(id);
        return Ok();
    }
}