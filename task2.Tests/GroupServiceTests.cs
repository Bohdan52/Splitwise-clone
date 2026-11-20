using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Services.Implementations;
using Xunit;
using task2.Exceptions;

namespace task2.Tests;

public class GroupServiceTests
{
    private SplitwiseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SplitwiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SplitwiseDbContext(options);
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldCreateGroupAndAddAdmin()
    {
        var context = GetInMemoryDbContext();
        var service = new GroupService(context);
        var userId = Guid.NewGuid();

        var result = await service.CreateGroupAsync(userId, "Vacation", "USD");

        Assert.NotNull(result);
        Assert.Equal("Vacation", result.Name);

        var member = await context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == result.Id && gm.UserId == userId);
        Assert.NotNull(member);
        Assert.Equal(MemberRole.Admin, member.Role);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddUser_WhenAdminRequests()
    {
        var context = GetInMemoryDbContext();
        var groupId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();

        context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Currency = "USD", CreatedById = adminId });
        context.Users.Add(new User { Id = newUserId, UserName = "test", Email = "test@test.com", FullName = "Test" });
        context.GroupMembers.Add(new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = adminId, Role = MemberRole.Admin });
        await context.SaveChangesAsync();

        var service = new GroupService(context);
        await service.AddMemberAsync(groupId, newUserId, adminId);

        var addedMember = await context.GroupMembers.FirstOrDefaultAsync(gm => gm.UserId == newUserId);
        Assert.NotNull(addedMember);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldRemoveUser_WhenAdminRequests()
    {
        var context = GetInMemoryDbContext();
        var service = new GroupService(context);

        var groupId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var regularId = Guid.NewGuid();

        context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Currency = "USD", CreatedById = adminId });
        context.GroupMembers.AddRange(
            new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = adminId, Role = MemberRole.Admin },
            new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = regularId, Role = MemberRole.Member }
        );
        await context.SaveChangesAsync();

        await service.RemoveMemberAsync(groupId, regularId, adminId);

        var removed = await context.GroupMembers.FirstOrDefaultAsync(gm => gm.UserId == regularId);
        Assert.Null(removed);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrow_WhenNotAdmin()
    {
        var context = GetInMemoryDbContext();
        var service = new GroupService(context);

        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        context.Groups.Add(new Group { Id = groupId, Name = "Test Group", Currency = "USD", CreatedById = userId });
        context.GroupMembers.Add(new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = userId, Role = MemberRole.Member });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.RemoveMemberAsync(groupId, otherId, userId));
    }

    [Fact]
    public async Task AddMemberAsync_ShouldThrow_WhenGroupNotFound()
    {
        var context = GetInMemoryDbContext();
        var service = new GroupService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddMemberAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }
}