using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Dtos;
using task2.Services.Implementations;
using Xunit;
using task2.Exceptions;

namespace task2.Tests;

public class ExpenseServiceTests
{
    private SplitwiseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SplitwiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SplitwiseDbContext(options);
    }

    private static void SeedMembers(SplitwiseDbContext context, Guid groupId, params Guid[] userIds)
    {
        foreach (var id in userIds)
            context.GroupMembers.Add(new GroupMember
            { Id = Guid.NewGuid(), GroupId = groupId, UserId = id, Role = MemberRole.Member });
        context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_EqualSplit_ShouldDivideAmountEqually()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);
        var groupId = Guid.NewGuid();
        var payer = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        SeedMembers(context, groupId, payer, user2Id);

        var request = new CreateExpenseRequest(
            GroupId: groupId,
            Description: "Pizza", 
            Amount: 100, Currency: "USD", Category: "Food",
            Date: DateOnly.FromDateTime(DateTime.UtcNow), 
            PaidById: payer, SplitType: SplitType.Equal,
            Participants: new List<SplitInputDto> { new SplitInputDto(payer), new SplitInputDto(user2Id) }
        );

        var result = await service.CreateAsync(request, payer);

        Assert.NotNull(result);
        Assert.Equal(2, result.Splits.Count);
        Assert.Equal(50, result.Splits[0].AmountOwed);
    }

    [Fact]
    public async Task CreateAsync_PercentageSplit_ShouldCalculateCorrectly()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);
        var currentUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var payer = Guid.NewGuid();
        SeedMembers(context, groupId, payer, user2Id);

        var request = new CreateExpenseRequest(
            GroupId: groupId, Description: "Party", Amount: 200, Currency: "USD", Category: "Fun",
            Date: DateOnly.FromDateTime(DateTime.UtcNow), PaidById: payer, SplitType: SplitType.Percentage,
            Participants: new List<SplitInputDto>
            {
                new SplitInputDto(payer, Percent: 20),
                new SplitInputDto(user2Id, Percent: 80)
            }
        );

        var result = await service.CreateAsync(request, payer);

        Assert.Equal(40, result.Splits[0].AmountOwed);
        Assert.Equal(160, result.Splits[1].AmountOwed);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenExpenseNotFound()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDelete_WhenUserIsCreator()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);
        var userId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();

        context.Expenses.Add(new Expense
        {
            Id = expenseId,
            CreatedById = userId,
            PaidById = userId,
            Amount = 10,
            Description = "Test",
            Currency = "USD",
            Date = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now,
            SplitType = SplitType.Equal
        });
        await context.SaveChangesAsync();

        await service.DeleteAsync(expenseId, userId);

        var deleted = await context.Expenses.FindAsync(expenseId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenUserIsNotCreator()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);

        context.Expenses.Add(new Expense
        {
            Id = Guid.NewGuid(),
            CreatedById = Guid.NewGuid(),
            PaidById = Guid.NewGuid(),
            Amount = 10,
            Description = "Test",
            Currency = "USD",
            Date = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now,
            SplitType = SplitType.Equal
        });
        await context.SaveChangesAsync();

        var randomUserId = Guid.NewGuid();
        var expenseId = context.Expenses.First().Id;

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(expenseId, randomUserId));
    }
    [Fact]
    public async Task GetByGroupIdAsync_ShouldThrow_WhenUserIsNotMember()
    {
        var service = new ExpenseService(GetInMemoryDbContext());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByGroupIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenParticipantIsNotInGroup()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);
        var groupId = Guid.NewGuid();
        var payer = Guid.NewGuid();
        SeedMembers(context, groupId, payer);

        var request = new CreateExpenseRequest(
            groupId, "Pizza", 100, "USD", null, DateOnly.FromDateTime(DateTime.UtcNow),
            payer, SplitType.Equal,
            new List<SplitInputDto> { new(payer), new(Guid.NewGuid()) });

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(request, payer));
    }
}