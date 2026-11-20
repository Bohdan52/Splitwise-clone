using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using task2.Domain.Entities;
using task2.Dtos;
using task2.Services.Interfaces;
using task2.Exceptions;

namespace task2.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();

        return users.Select(u => new UserResponse(
            u.Id,
            u.Email ?? string.Empty,
            u.FullName ?? string.Empty,
            u.DefaultCurrency ?? string.Empty
        ));
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null) throw new NotFoundException("User not found");

        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName ?? string.Empty,
            user.DefaultCurrency ?? string.Empty
        );
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null) throw new NotFoundException("User not found");

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.DefaultCurrency = request.DefaultCurrency;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new BadRequestException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName ?? string.Empty,
            user.DefaultCurrency ?? string.Empty
        );
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null) throw new NotFoundException("User not found");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
            throw new BadRequestException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}