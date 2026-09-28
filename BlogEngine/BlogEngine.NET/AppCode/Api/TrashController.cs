using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TrashController : ControllerBase
{
    readonly ITrashRepository repository;

    public TrashController(ITrashRepository repository)
    {
        this.repository = repository;
    }

    public TrashVM Get(int type = 1, int take = 10, int skip = 0, string filter = "1==1", string order = "DateCreated descending")
    {
        return repository.GetTrash((TrashType)type, take, skip, filter, order);
    }

    [HttpPut]
    public IActionResult Purge(TrashItem item)
    {
        repository.Purge(item.ObjectType, item.Id);
        return Ok();
    }

    [HttpPut]
    public IActionResult PurgeAll()
    {
        repository.PurgeAll();
        return Ok();
    }

    [HttpPut]
    public IActionResult Restore(TrashItem item)
    {
        repository.Restore(item.ObjectType, item.Id);
        return Ok();
    }
}
