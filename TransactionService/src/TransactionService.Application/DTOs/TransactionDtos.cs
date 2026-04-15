using System.ComponentModel.DataAnnotations;

namespace TransactionService.Application.DTOs;

public record CreateTransactionRequest(
    [Required] Guid FromAccountId,
    [Required] Guid ToAccountId,
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive.")] decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [MaxLength(500)] string? Description
);

public record TransactionResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string Status,
    string? Description,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record StatementResponse(
    IReadOnlyList<TransactionResponse> Items,
    string? NextCursor,
    bool HasMore
);
