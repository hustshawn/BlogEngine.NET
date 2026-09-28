using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PagerController : ControllerBase
{
    public string Get()
    {
        return BlogEngine.Core.Pager.Render();
    }
}
