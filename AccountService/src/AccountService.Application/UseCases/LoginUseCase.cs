using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using AccountService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccountService.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<LoginUseCase> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        _logger.LogInformation("User logged in: {UserId}", user.Id);

        var (token, expiresAt) = _jwtService.GenerateToken(user);

        return new LoginResponse(
            token,
            expiresAt,
            new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt)
        );
    }
}
