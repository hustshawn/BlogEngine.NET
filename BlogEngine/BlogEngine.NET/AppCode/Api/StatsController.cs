using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    readonly IStatsRepository repository;

    public StatsController(IStatsRepository repository)
    {
        this.repository = repository;
    }

    public Stats Get()
    {
        return repository.Get();
    }
}
