using System.ComponentModel.DataAnnotations;

namespace AccountService.Application.DTOs;

public record RegisterUserRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    DateTime CreatedAt
);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    UserResponse User
);
