using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using AccountService.Application.UseCases;
using AccountService.Domain.Entities;
using AccountService.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AccountService.UnitTests.UseCases;

public class RegisterUserUseCaseTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<ILogger<RegisterUserUseCase>> _loggerMock = new();

    private RegisterUserUseCase CreateSut() =>
        new(_userRepositoryMock.Object, _jwtServiceMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithNewEmail_ShouldRegisterAndReturnToken()
    {
        // Arrange
        var request = new RegisterUserRequest("Test User", "test@example.com", "StrongPass123!");
        var expectedToken = "jwt.token.here";
        var expectedExpiry = DateTime.UtcNow.AddHours(1);

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns((expectedToken, expectedExpiry));

        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(request);

        // Assert
        result.Token.Should().Be(expectedToken);
        result.User.Email.Should().Be(request.Email.ToLowerInvariant());
        result.User.Name.Should().Be(request.Name);

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterUserRequest("Test User", "existing@example.com", "StrongPass123!");

        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var act = () => sut.ExecuteAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
