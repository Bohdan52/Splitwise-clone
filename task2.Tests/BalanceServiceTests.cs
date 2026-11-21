using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Exceptions;
using task2.Services.Implementations;
using Xunit;

namespace task2.Tests;

public class BalanceServiceTests
{
    private SplitwiseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SplitwiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SplitwiseDbContext(options);
    }
    private static (Guid groupId, Guid user1, Guid user2) SeedPizzaGroup(SplitwiseDbContext context)
    {
        var groupId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        context.GroupMembers.AddRange(
            new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = user1, Role = MemberRole.Admin },
            new GroupMember { Id = Guid.NewGuid(), GroupId = groupId, UserId = user2, Role = MemberRole.Member }
        );

        var expenseId = Guid.NewGuid();
        context.Expenses.Add(new Expense
        {
            Id = expenseId,
            GroupId = groupId,
            PaidById = user1,
            CreatedById = user1,
            Amount = 100,
            Description = "Pizza",
            Currency = "USD",
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Splits = new List<ExpenseSplit>
            {
                new ExpenseSplit { Id = Guid.NewGuid(), ExpenseId = expenseId, UserId = user1, AmountOwed = 50 },
                new ExpenseSplit { Id = Guid.NewGuid(), ExpenseId = expenseId, UserId = user2, AmountOwed = 50 }
            }
        });

        context.SaveChanges();
        return (groupId, user1, user2);
    }

    [Fact]
    public async Task GetGroupSettlementsAsync_ShouldCalculateCorrectDebts()
    {
        var context = GetInMemoryDbContext();
        var service = new BalanceService(context);
        var (groupId, user1, user2) = SeedPizzaGroup(context);

        var settlements = await service.GetGroupSettlementsAsync(groupId, user1);

        Assert.NotNull(settlements);
        var settlementList = settlements.ToList();

        Assert.Single(settlementList);
        Assert.Equal(user2, settlementList[0].FromUserId);
        Assert.Equal(user1, settlementList[0].ToUserId);
        Assert.Equal(50, settlementList[0].Amount);
    }

    [Fact]
    public async Task GetGroupBalancesAsync_ShouldThrow_WhenUserIsNotMember()
    {
        var context = GetInMemoryDbContext();
        var service = new BalanceService(context);
        var (groupId, _, _) = SeedPizzaGroup(context);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetGroupBalancesAsync(groupId, Guid.NewGuid()));
    }
}