using task2.Dtos;

namespace task2.Services.Interfaces;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseResponse>> GetAllAsync();
    Task<ExpenseResponse> GetByIdAsync(Guid id);
    Task<IEnumerable<ExpenseResponse>> GetByGroupIdAsync(Guid groupId);
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId);
    Task DeleteAsync(Guid id, Guid currentUserId);
}