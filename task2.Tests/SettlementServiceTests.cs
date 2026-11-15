using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Dtos;
using task2.Services.Implementations;
using Xunit;

namespace task2.Tests;

public class SettlementServiceTests
{
    private SplitwiseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SplitwiseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SplitwiseDbContext(options);
    }

    [Fact]
    public async Task CreateSettlementAsync_ShouldThrow_WhenAmountIsNegative()
    {
        var context = GetInMemoryDbContext();
        var service = new SettlementService(context);

        var request = new CreateSettlementRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -50, "USD", "Test", DateOnly.FromDateTime(DateTime.Now));

        await Assert.ThrowsAsync<Exception>(() => service.CreateSettlementAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateSettlementAsync_ShouldThrow_WhenSettlingWithSelf()
    {
        var context = GetInMemoryDbContext();
        var service = new SettlementService(context);
        var userId = Guid.NewGuid();

        var request = new CreateSettlementRequest(Guid.NewGuid(), userId, userId, 50, "USD", "Test", DateOnly.FromDateTime(DateTime.Now));

        await Assert.ThrowsAsync<Exception>(() => service.CreateSettlementAsync(request, userId));
    }
    [Fact]
    public async Task CreateSettlementAsync_ShouldCreateSettlement_WhenValid()
    {
        var context = GetInMemoryDbContext();
        var service = new SettlementService(context);
        var currentUserId = Guid.NewGuid();

        var request = new CreateSettlementRequest(Guid.NewGuid(), currentUserId, Guid.NewGuid(), 100, "USD", "Lunch", DateOnly.FromDateTime(DateTime.Now));

        var result = await service.CreateSettlementAsync(request, currentUserId);

        Assert.NotNull(result);
        Assert.Equal(100, result.Amount);

        var settlementInDb = await context.Settlements.FirstOrDefaultAsync();
        Assert.NotNull(settlementInDb);
        Assert.Equal(currentUserId, settlementInDb.FromUserId);
    }
}