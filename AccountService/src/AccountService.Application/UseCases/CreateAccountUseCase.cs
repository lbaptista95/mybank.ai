using AccountService.Application.DTOs;
using AccountService.Domain.Entities;
using AccountService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccountService.Application.UseCases;

public class CreateAccountUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateAccountUseCase> _logger;

    public CreateAccountUseCase(
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        ILogger<CreateAccountUseCase> logger)
    {
        _accountRepository = accountRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AccountResponse> ExecuteAsync(Guid userId, CreateAccountRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var account = Account.Create(userId, request.Currency);

        await _accountRepository.AddAsync(account, ct);
        await _accountRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Account created: {AccountId} for user {UserId}", account.Id, userId);

        return MapToResponse(account);
    }

    public async Task<AccountResponse> GetByIdAsync(Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new KeyNotFoundException($"Account '{accountId}' not found.");

        if (account.UserId != userId)
            throw new UnauthorizedAccessException("Access denied to this account.");

        return MapToResponse(account);
    }

    public async Task<IEnumerable<AccountResponse>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetByUserIdAsync(userId, ct);
        return accounts.Select(MapToResponse);
    }

    private static AccountResponse MapToResponse(Domain.Entities.Account a) =>
        new(a.Id, a.UserId, a.Balance, a.Currency, a.Status.ToString(), a.CreatedAt);
}
