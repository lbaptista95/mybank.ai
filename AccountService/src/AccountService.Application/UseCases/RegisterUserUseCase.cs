using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using AccountService.Domain.Entities;
using AccountService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccountService.Application.UseCases;

public class RegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RegisterUserUseCase> _logger;

    public RegisterUserUseCase(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<RegisterUserUseCase> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Name, request.Email, passwordHash);

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);

        var (token, expiresAt) = _jwtService.GenerateToken(user);

        return new LoginResponse(
            token,
            expiresAt,
            new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt)
        );
    }
}
