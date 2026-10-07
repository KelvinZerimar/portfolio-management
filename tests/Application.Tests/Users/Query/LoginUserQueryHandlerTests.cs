using Application.Common.Security;
using Application.Users.Interfaces;
using Application.Users.Query;
using Contracts.Users;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Users.Query;

public class LoginUserQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private LoginUserQueryHandler CreateHandler() => new(
        Substitute.For<ILogger<LoginUserQueryHandler>>(),
        _userRepository,
        _passwordHasher,
        _jwtTokenGenerator);

    private static LoginUserRequest CreateRequest(string email = "user@test.com", string password = "P@ssw0rd!") => new()
    {
        Email = email,
        Password = password
    };

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenResponse()
    {
        var request = CreateRequest();
        var user = User.Create(request.Email, "Ada", "Lovelace", "stored-hash");
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerateToken(user).Returns(new AuthToken("the-access-token", expiresAt));

        var result = await CreateHandler().Handle(new LoginUserQuery(request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.AccessToken.Should().Be("the-access-token");
        result.Value.ExpiresAtUtc.Should().Be(expiresAt);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmail_ReturnsUnauthorizedWithoutGeneratingToken()
    {
        var request = CreateRequest();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new LoginUserQuery(request), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User.InvalidCredentials");
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsUnauthorizedWithoutGeneratingToken()
    {
        var request = CreateRequest();
        var user = User.Create(request.Email, "Ada", "Lovelace", "stored-hash");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(false);

        var result = await CreateHandler().Handle(new LoginUserQuery(request), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User.InvalidCredentials");
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }
}
