using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Services.Interfaces;

namespace task2.Services.Implementations;

public class GroupService : IGroupService
{
    private readonly SplitwiseDbContext _context;

    public GroupService(SplitwiseDbContext context)
    {
        _context = context;
    }

    public async Task<Group> CreateGroupAsync(Guid userId, string name, string currency)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Currency = currency,
            CreatedById = userId,
            Type = GroupType.Home
        };

        _context.Groups.Add(group);

        _context.GroupMembers.Add(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = userId,
            Role = MemberRole.Admin
        });

        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(Guid userId)
    {
        return await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.Group)
            .ToListAsync();
    }

    public async Task AddMemberAsync(Guid groupId, Guid userId, Guid currentUserId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) throw new Exception("Group not found");

        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.Role == MemberRole.Admin);

        if (!isAdmin) throw new Exception("Only group admins can add members.");

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) throw new Exception("User not found");

        var alreadyMember = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (alreadyMember) throw new Exception("User is already a member of this group.");

        _context.GroupMembers.Add(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = MemberRole.Member
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId, Guid currentUserId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) throw new Exception("Group not found");

        var isAdmin = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.Role == MemberRole.Admin);

        if (!isAdmin) throw new Exception("Only group admins can remove members.");

        if (userId == currentUserId) throw new Exception("Admin cannot remove themselves.");

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (member == null) throw new Exception("User is not a member of this group.");

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
    }
}