using task2.Dtos;

namespace task2.Services.Interfaces;

public interface IBalanceService
{
    Task<IEnumerable<BalanceResponse>> GetGroupBalancesAsync(Guid groupId, Guid currentUserId);
    Task<IEnumerable<SettlementSuggestionResponse>> GetGroupSettlementsAsync(Guid groupId, Guid currentUserId);
}