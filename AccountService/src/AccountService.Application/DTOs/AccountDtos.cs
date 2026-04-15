using System.ComponentModel.DataAnnotations;
using AccountService.Domain.Enums;

namespace AccountService.Application.DTOs;

public record CreateAccountRequest(
    [Required, StringLength(3, MinimumLength = 3)] string Currency = "BRL"
);

public record AccountResponse(
    Guid Id,
    Guid UserId,
    decimal Balance,
    string Currency,
    string Status,
    DateTime CreatedAt
);
