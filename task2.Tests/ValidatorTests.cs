using FluentValidation.TestHelper;
using task2.Dtos;
using task2.Dtos.Auth;
using task2.Validators;
using Xunit;

namespace task2.Tests;

public class AuthValidatorTests
{
    [Fact]
    public void RegisterDto_Should_Have_Error_When_Email_Is_Empty()
    {
        var validator = new RegisterDtoValidator();
        var model = new RegisterDto { Email = "", UserName = "testuser", Password = "Password123!" };

        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RegisterDto_Should_Have_Error_When_UserName_Is_Too_Short()
    {
        var validator = new RegisterDtoValidator();
        // Перевіряємо MinLength(3) для UserName
        var model = new RegisterDto { Email = "test@test.com", UserName = "ab", Password = "Password123!" };

        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void RegisterDto_Should_Have_Error_When_Password_Is_Too_Short()
    {
        var validator = new RegisterDtoValidator();
        // Перевіряємо MinLength(6) для Password
        var model = new RegisterDto { Email = "test@test.com", UserName = "testuser", Password = "123" };

        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterDto_Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var validator = new RegisterDtoValidator();
        var model = new RegisterDto
        {
            Email = "test@test.com",
            UserName = "testuser",
            Password = "ValidPassword123!",
            FullName = "John Doe"
        };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginDto_Should_Have_Error_When_UserNameOrEmail_Is_Empty()
    {
        var validator = new LoginDtoValidator();
        var model = new LoginDto { UserNameOrEmail = "", Password = "Password123!" };

        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UserNameOrEmail);
    }

    [Fact]
    public void LoginDto_Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var validator = new LoginDtoValidator();
        var model = new LoginDto { UserNameOrEmail = "testuser", Password = "Password123!" };

        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}