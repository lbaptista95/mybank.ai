using AccountService.Application.DTOs;
using AccountService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly RegisterUserUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;

    public UsersController(RegisterUserUseCase registerUseCase, LoginUseCase loginUseCase)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
    }

    /// <summary>Register a new user and receive a JWT token.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _registerUseCase.ExecuteAsync(request, ct);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Authenticate and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _loginUseCase.ExecuteAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }
}
