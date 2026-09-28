using BlogEngine.Core;
using BlogEngine.Core.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public LogsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IEnumerable<SelectOption> GetLog()
    {
        if (!Security.IsAuthorizedTo(BlogEngine.Core.Rights.AccessAdminPages))
            throw new System.UnauthorizedAccessException();

        var action = HttpContext.Request.RouteValues["id"]?.ToString();

        switch (action)
        {
            case "file":
                return GetLogFile();
            default:
                break;
        }
        return new List<SelectOption>();
    }

    [HttpPut]
    public IActionResult PurgeLog()
    {
        if (!Security.IsAuthorizedTo(BlogEngine.Core.Rights.AccessAdminPages))
            throw new System.UnauthorizedAccessException();

        string fileLocation = Path.Combine(_env.ContentRootPath, BlogConfig.StorageLocation.TrimStart('~', '/'), "logger.txt");
        try
        {
            System.IO.File.Delete(fileLocation);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            Utils.Log("Error purging log file", ex);
            return StatusCode(500);
        }
    }

    IEnumerable<SelectOption> GetLogFile()
    {
        string fileLocation = Path.Combine(_env.ContentRootPath, BlogConfig.StorageLocation.TrimStart('~', '/'), "logger.txt");
        var items = new List<SelectOption>();
    
        if (System.IO.File.Exists(fileLocation))
        {
            using (var sw = new StreamReader(fileLocation))
            {
                string line;
                string logItem = "";
                int count = 1;
                while ((line = sw.ReadLine()) != null)
                {
                    if (line.Contains("*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*"))
                    {
                        // new log item
                        if (!string.IsNullOrEmpty(logItem))
                        {
                            var item = new SelectOption();
                            item.OptionName = "Line" + count.ToString();
                            item.OptionValue = logItem;
                            items.Add(item);
                            logItem = "";
                            count++;
                        }
                    }
                    else
                    {
                        // append line to log item
                        logItem = logItem + line + "<br/>";
                    }
                    
                }
                sw.Close();
                return items;
            }
        }
        else
        {
            return new List<SelectOption>();
        }
    }
}
