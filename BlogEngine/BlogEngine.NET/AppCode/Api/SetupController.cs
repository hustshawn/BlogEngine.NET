using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public SetupController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string Get(string version)
    {
        var updater = new BlogEngine.Core.Data.Services.Updater(_env.ContentRootPath);
        return updater.Check(version);
    }
}
