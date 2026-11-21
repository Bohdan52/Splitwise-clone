using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using task2.Services.Interfaces;

namespace task2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Balance")]
public class BalanceController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalanceController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpGet("group/{groupId:guid}")]
    public async Task<IActionResult> GetGroupBalances(Guid groupId)
    {
        var result = await _balanceService.GetGroupBalancesAsync(groupId, GetUserId());
        return Ok(result);
    }

    [HttpGet("group/{groupId:guid}/settlements")]
    public async Task<IActionResult> GetGroupSettlements(Guid groupId)
    {
        var result = await _balanceService.GetGroupSettlementsAsync(groupId, GetUserId());
        return Ok(result);
    }
}