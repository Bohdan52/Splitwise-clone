using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using task2.Dtos;
using task2.Services.Interfaces;

namespace task2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _userService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _userService.GetByIdAsync(id)); }
        catch (Exception ex) { return NotFound(ex.Message); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (GetUserId() != id)
            return Forbid();

        try { return Ok(await _userService.UpdateAsync(id, request)); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (GetUserId() != id)
            return Forbid();

        try
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}