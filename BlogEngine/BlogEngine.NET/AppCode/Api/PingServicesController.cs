using BlogEngine.Core.Providers;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PingServicesController : ControllerBase
{
    public IEnumerable<PingItem> Get()
    {
        var pings = new List<PingItem>();

        foreach (var s in BlogService.LoadPingServices())
        {
            pings.Add(new PingItem { IsChecked = false, ServiceUrl = s });
        }
        return pings;
    }

    public IActionResult Post([FromBody]PingItem item)
    {
        var sc = BlogService.LoadPingServices();
        if (!sc.Contains(item.ServiceUrl))
        {
            sc.Add(item.ServiceUrl);
        }
        BlogService.SavePingServices(sc);

        var pings = new List<PingItem>();
        foreach (var s in sc)
        {
            pings.Add(new PingItem { IsChecked = false, ServiceUrl = s });
        }
        return Created("", pings);
    }

    [HttpPut]
    public IActionResult ProcessChecked([FromBody]List<PingItem> items)
    {
        if (items == null || items.Count == 0)
            return StatusCode((int)HttpStatusCode.ExpectationFailed);

        var sc = BlogService.LoadPingServices();

        foreach (var item in items)
        {
            if (item.IsChecked)
            {
                sc.Remove(item.ServiceUrl);
            }
        }
        BlogService.SavePingServices(sc);

        return Ok();
    }
}

public class PingItem
{
    public bool IsChecked { get; set; }
    public string ServiceUrl { get; set; }
}