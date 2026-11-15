using Microsoft.AspNetCore.Mvc;
using task2.Dtos.Auth;
using task2.Services.Interfaces;

namespace task2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
       => Ok(await _authService.RegisterAsync(dto));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
        => Ok(await _authService.LoginAsync(dto));
}