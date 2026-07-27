using DigitalWalletApi.Extensions;
using DigitalWalletApi.Filter;
using DigitalWalletCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWalletApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("student/dashboard")]
    [Authorize(Roles = "Student")]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> GetStudentDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetStudentDashboardAsync(User.GetUserId(), cancellationToken);
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    [HttpGet("merchant/dashboard")]
    [Authorize(Roles = "Merchant")]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> GetMerchantDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetMerchantDashboardAsync(User.GetUserId(), cancellationToken);
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    [HttpGet("school/dashboard")]
    [Authorize(Roles = "SchoolAdmin")]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> GetSchoolDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSchoolDashboardAsync(User.GetUserId(), cancellationToken);
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    [HttpGet("system/dashboard")]
    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(LogActionFilter))]
    public async Task<IActionResult> GetSystemDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSystemDashboardAsync(cancellationToken);
        return Ok(result);
    }
}
