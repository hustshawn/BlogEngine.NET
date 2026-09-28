using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    /// <summary>
    /// Dashboard "new version available" check. Upstream releases are .NET Framework builds, so the
    /// in-place auto-upgrade (setup/upgrade) was removed with the .NET 10 migration; never offer one.
    /// </summary>
    public string Get(string version)
    {
        return string.Empty;
    }
}
