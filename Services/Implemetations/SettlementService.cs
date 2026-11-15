using task2.Data;
using task2.Domain.Entities;
using task2.Dtos;
using task2.Services.Interfaces;

namespace task2.Services.Implementations;

public class SettlementService : ISettlementService
{
    private readonly SplitwiseDbContext _context;

    public SettlementService(SplitwiseDbContext context)
    {
        _context = context;
    }

    public async Task<SettlementResponse> CreateSettlementAsync(CreateSettlementRequest request, Guid currentUserId)
    {
        if (request.Amount <= 0)
            throw new Exception("Amount must be greater than zero");

        if (request.FromUserId == request.ToUserId)
            throw new Exception("You cannot settle a debt with yourself");

        if (request.FromUserId != currentUserId)
            throw new Exception("You can only create settlements from your own account");

        var settlement = new Settlement
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Note = request.Note,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.Settlements.Add(settlement);
        await _context.SaveChangesAsync();

        return new SettlementResponse(
            settlement.Id,
            settlement.GroupId,
            settlement.FromUserId,
            settlement.ToUserId,
            settlement.Amount,
            settlement.Currency,
            settlement.Note,
            settlement.Date,
            settlement.CreatedAt
        );
    }
}