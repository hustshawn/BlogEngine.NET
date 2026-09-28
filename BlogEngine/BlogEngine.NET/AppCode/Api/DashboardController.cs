using BlogEngine.Core.Data.Contracts;
using BlogEngine.Core.Data.ViewModels;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    readonly IDashboardRepository repository;

    public DashboardController(IDashboardRepository repository)
    {
        this.repository = repository;
    }

    public DashboardVM Get()
    {
        return repository.Get();
    }
}