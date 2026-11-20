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

    [Fact]
    public async Task CreateAsync_EqualSplit_ShouldDivideAmountEqually()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);

        var request = new CreateExpenseRequest(
            GroupId: Guid.NewGuid(), Description: "Pizza", Amount: 100, Currency: "USD", Category: "Food",
            Date: DateOnly.FromDateTime(DateTime.UtcNow), PaidById: Guid.NewGuid(), SplitType: SplitType.Equal,
            Participants: new List<SplitInputDto> { new SplitInputDto(Guid.NewGuid()), new SplitInputDto(Guid.NewGuid()) }
        );

        var result = await service.CreateAsync(request, Guid.NewGuid());

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

        var request = new CreateExpenseRequest(
            GroupId: Guid.NewGuid(), Description: "Party", Amount: 200, Currency: "USD", Category: "Fun",
            Date: DateOnly.FromDateTime(DateTime.UtcNow), PaidById: currentUserId, SplitType: SplitType.Percentage,
            Participants: new List<SplitInputDto>
            {
                new SplitInputDto(Guid.NewGuid(), Percent: 20),
                new SplitInputDto(Guid.NewGuid(), Percent: 80)
            }
        );

        var result = await service.CreateAsync(request, currentUserId);

        Assert.Equal(40, result.Splits[0].AmountOwed);
        Assert.Equal(160, result.Splits[1].AmountOwed);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenExpenseNotFound()
    {
        var context = GetInMemoryDbContext();
        var service = new ExpenseService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
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

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.DeleteAsync(expenseId, randomUserId));
    }
}