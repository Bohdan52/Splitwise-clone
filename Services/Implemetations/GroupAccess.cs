using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Exceptions;

namespace task2.Services.Implemetations;

internal static class GroupAccess
{
    public static async Task EnsureMemberAsync(this SplitwiseDbContext context, Guid groupId, Guid userId)
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (!isMember)
        {
            throw new ForbiddenException("User is not a member of the group.");
        }

    }

}