using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using task2.Domain.Entities;
using task2.Dtos;
using task2.Services.Implementations;
using Xunit;

namespace task2.Tests;

public class UserServiceTests
{
    private Mock<UserManager<User>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenUserNotFound()
    {
        var mockUserManager = GetMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

        var service = new UserService(mockUserManager.Object);
        await Assert.ThrowsAsync<Exception>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser_WhenUserExists()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "old", Email = "old@test.com", FullName = "Old Name", DefaultCurrency = "USD" };
        var mockUserManager = GetMockUserManager();

        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = new UserService(mockUserManager.Object);
        var request = new UpdateUserRequest("new@test.com", "New Name", "EUR");

        var result = await service.UpdateAsync(user.Id, request);

        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("EUR", result.DefaultCurrency);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteUser_WhenUserExists()
    {
        var user = new User { Id = Guid.NewGuid() };
        var mockUserManager = GetMockUserManager();

        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = new UserService(mockUserManager.Object);
        await service.DeleteAsync(user.Id);
    }
    [Fact]
    public async Task GetAllAsync_ShouldReturnUsers()
    {
        var mockUserManager = GetMockUserManager();
        var users = new List<User> { new User { Id = Guid.NewGuid(), Email = "test@test.com", FullName = "Test", DefaultCurrency = "USD" } };

        mockUserManager.Setup(x => x.Users).Returns(new Mock<IQueryable<User>>().Object); 
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenUpdateFails()
    {
        var user = new User { Id = Guid.NewGuid() };
        var mockUserManager = GetMockUserManager();

        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

        var service = new UserService(mockUserManager.Object);
        var request = new UpdateUserRequest("test@test.com", "Name", "USD");

        await Assert.ThrowsAsync<Exception>(() => service.UpdateAsync(user.Id, request));
    }
}