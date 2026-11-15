using task2.Dtos;

namespace task2.Services.Interfaces;

public interface ISettlementService
{
    Task<SettlementResponse> CreateSettlementAsync(CreateSettlementRequest request, Guid currentUserId);
}