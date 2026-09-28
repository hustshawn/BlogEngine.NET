using BlogEngine.Core.Data.Models;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using Microsoft.AspNetCore.Mvc;
using System.Xml;

[ApiController]
[Route("api/[controller]")]
public class NewsFeedController : ControllerBase
{
    public List<SelectOption> Get()
    {
        var items = new List<SelectOption>();
        string url = "https://blogengine.io/news.xml";
        try
        {
            var cnt = 0;
            var reader = XmlReader.Create(url);
            var feed = SyndicationFeed.Load(reader);
            reader.Close();

            foreach (SyndicationItem item in feed.Items)
            {
                var option = new SelectOption();
                option.OptionName = item.Title.Text;
                option.OptionValue = item.Id;
                items.Add(option);
                cnt++;
                if (cnt > 5) break;
            }
        }
        catch (Exception ex)
        {
            BlogEngine.Core.Utils.Log("Dashboard news feed", ex);
        }
        return items;
    }
}
