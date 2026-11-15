using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using task2.Dtos;
using task2.Services.Interfaces;

namespace task2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _expenseService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _expenseService.GetByIdAsync(id)); }
        catch (Exception ex) { return NotFound(ex.Message); }
    }

    [HttpGet("group/{groupId:guid}")]
    public async Task<IActionResult> GetByGroupId(Guid groupId)
    {
        return Ok(await _expenseService.GetByGroupIdAsync(groupId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var result = await _expenseService.CreateAsync(request, GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _expenseService.DeleteAsync(id, GetUserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}