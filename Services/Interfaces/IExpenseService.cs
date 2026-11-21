using task2.Dtos;

namespace task2.Services.Interfaces;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseResponse>> GetAllAsync(Guid currentUserId);
    Task<ExpenseResponse> GetByIdAsync(Guid id, Guid currentUserId);
    Task<IEnumerable<ExpenseResponse>> GetByGroupIdAsync(Guid groupId, Guid currentUserId);
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId);
    Task DeleteAsync(Guid id, Guid currentUserId);
}