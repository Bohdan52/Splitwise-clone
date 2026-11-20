using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using task2.Dtos;
using task2.Services.Interfaces;

namespace task2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Group")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        var group = await _groupService.CreateGroupAsync(GetUserId(), request.Name, request.Currency);
        return Ok(group);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var groups = await _groupService.GetUserGroupsAsync(GetUserId());
        return Ok(groups);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        await _groupService.AddMemberAsync(id, request.UserId, GetUserId());
        return Ok();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _groupService.RemoveMemberAsync(id, userId, GetUserId());
        return NoContent();
    }
}