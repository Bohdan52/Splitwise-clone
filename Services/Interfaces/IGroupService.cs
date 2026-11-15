using task2.Domain.Entities;

namespace task2.Services.Interfaces;

public interface IGroupService
{
    Task<Group> CreateGroupAsync(Guid userId, string name, string currency);
    Task<IEnumerable<Group>> GetUserGroupsAsync(Guid userId);

    Task AddMemberAsync(Guid groupId, Guid userId, Guid currentUserId);
    Task RemoveMemberAsync(Guid groupId, Guid userId, Guid currentUserId);
}