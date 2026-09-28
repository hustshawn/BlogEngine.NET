using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    readonly ICategoryRepository repository;

    public CategoriesController(ICategoryRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<CategoryItem> Get(int take = 10, int skip = 0, string filter = "", string order = "")
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

    public IActionResult Post([FromBody]CategoryItem item)
    {
        var result = repository.Add(item);
        if (result == null)
            return NotFound();

        return Created("", result);
    }

    [HttpPut]
    public IActionResult Update([FromBody]CategoryItem item)
    {
        repository.Update(item);
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
    public IActionResult ProcessChecked([FromBody]List<CategoryItem> items)
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
