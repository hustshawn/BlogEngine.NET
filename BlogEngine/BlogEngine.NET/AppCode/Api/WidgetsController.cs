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
public class WidgetsController : ControllerBase
{
    readonly IWidgetsRepository repository;

    public WidgetsController(IWidgetsRepository repository)
    {
        this.repository = repository;
    }

    public WidgetsVM Get()
    {
        return new WidgetsVM();
    }

    [HttpPut]
    public IActionResult Update([FromBody]List<WidgetZone> items)
    {
        if (repository.Update(items))
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }    
    }

}