using TransactionService.Application.DTOs;
using TransactionService.Application.Interfaces;
using TransactionService.Domain.Entities;
using TransactionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace TransactionService.Application.UseCases;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<CreateTransactionUseCase> _logger;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        IKafkaProducer kafkaProducer,
        ILogger<CreateTransactionUseCase> logger)
    {
        _transactionRepository = transactionRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<TransactionResponse> ExecuteAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        var transaction = Transaction.Create(
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.Currency,
            request.Description);

        await _transactionRepository.AddAsync(transaction, ct);
        await _transactionRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Transaction created: {TransactionId}", transaction.Id);

        // Publish to Kafka for fraud analysis
        var kafkaMessage = new
        {
            transactionId = transaction.Id,
            fromAccountId = transaction.FromAccountId,
            toAccountId = transaction.ToAccountId,
            amount = transaction.Amount,
            currency = transaction.Currency,
            description = transaction.Description,
            createdAt = transaction.CreatedAt
        };

        await _kafkaProducer.PublishAsync("transaction.created", transaction.Id.ToString(), kafkaMessage, ct);
        _logger.LogInformation("Event published: transaction.created for {TransactionId}", transaction.Id);

        return MapToResponse(transaction);
    }

    public async Task<TransactionResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, ct);
        return transaction is null ? null : MapToResponse(transaction);
    }

    public async Task<StatementResponse> GetStatementAsync(
        Guid accountId,
        string? cursor,
        int pageSize,
        CancellationToken ct = default)
    {
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);
        var (items, nextCursor) = await _transactionRepository.GetStatementAsync(accountId, cursor, clampedPageSize, ct);

        return new StatementResponse(
            items.Select(MapToResponse).ToList().AsReadOnly(),
            nextCursor,
            nextCursor is not null
        );
    }

    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.FromAccountId, t.ToAccountId, t.Amount, t.Currency,
            t.Status.ToString(), t.Description, t.CreatedAt, t.ProcessedAt);
}
