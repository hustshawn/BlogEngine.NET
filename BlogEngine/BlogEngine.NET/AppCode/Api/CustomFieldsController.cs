using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CustomFieldsController : ControllerBase
{
    readonly ICustomFieldRepository repository;

    public CustomFieldsController(ICustomFieldRepository repository)
    {
        this.repository = repository;
    }

    public IEnumerable<CustomField> Get(string filter = "")
    {
        // filter example: 'CustomType == "THEME" && ObjectId == "standard"'
        return repository.Find(filter);
    }

    public IActionResult Post([FromBody]CustomField item)
    {
        if (item == null) throw new ApplicationException("Custom field is required");

        item.BlogId = BlogEngine.Core.Blog.CurrentInstance.Id;

        if (item.CustomType == "PROFILE")
        {
            item.ObjectId = BlogEngine.Core.Security.CurrentUser.Identity.Name;
        }

        var result = repository.Add(item);
        if (result == null)
            return NotFound();

        return Created("", result);
    }

    public IActionResult Put([FromBody]List<CustomField> items)
    {
        if (items != null && items.Count > 0)
        {
            repository.ClearCustomFields(items[0].CustomType, items[0].ObjectId);

            foreach (var item in items)
            {
                repository.Add(item);
            }
        }
        return Ok();
    }

    public IActionResult Delete([FromBody]CustomField item)
    {
        repository.Remove(item.CustomType, item.ObjectId, item.Key);
        return Ok();
    }
}