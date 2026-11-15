using task2.Dtos;

namespace task2.Services.Interfaces;

public interface IBalanceService
{
    Task<IEnumerable<BalanceResponse>> GetGroupBalancesAsync(Guid groupId);
    Task<IEnumerable<SettlementSuggestionResponse>> GetGroupSettlementsAsync(Guid groupId);
}