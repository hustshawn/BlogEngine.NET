using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    readonly ILookupsRepository  repository;

    public LookupsController(ILookupsRepository repository)
    {
        this.repository = repository;
    }

    public Lookups Get()
    {
        return repository.GetLookups();
    }

    [HttpPut]
    public IActionResult Update([FromBody]EditorOptions item)
    {
        try
        {
            repository.SaveEditorOptions(item);
            return Ok(true);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode((int)HttpStatusCode.Unauthorized);
        }
        catch (Exception ex)
        {
            BlogEngine.Core.Utils.Log("Error updating", ex);
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
