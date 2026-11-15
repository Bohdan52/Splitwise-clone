using task2.Dtos;

namespace task2.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
}