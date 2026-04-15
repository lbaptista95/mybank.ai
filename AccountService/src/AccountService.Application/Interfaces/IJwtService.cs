using AccountService.Domain.Entities;

namespace AccountService.Application.Interfaces;

public interface IJwtService
{
    (string token, DateTime expiresAt) GenerateToken(User user);
}
