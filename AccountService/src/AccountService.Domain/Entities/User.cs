namespace AccountService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Account> _accounts = new();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    private User() { }

    public static User Create(string name, string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
