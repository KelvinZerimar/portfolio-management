using Application.Common.Security;
using Application.Users.Command;
using Application.Users.Interfaces;
using Contracts.Users;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Users.Command;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterUserCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<RegisterUserCommandHandler>>(),
        _userRepository,
        _passwordHasher);

    private static RegisterUserRequest CreateRequest(string email = "new@user.com") => new()
    {
        Email = email,
        FirstName = "Ada",
        LastName = "Lovelace",
        Password = "P@ssw0rd!"
    };

    [Fact]
    public async Task Handle_WithNewEmail_CreatesUserAndReturnsResponse()
    {
        var request = CreateRequest();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Hash(request.Password).Returns("hashed-password");

        var result = await CreateHandler().Handle(new RegisterUserCommand(request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be(request.Email);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == request.Email
                && u.FirstName == request.FirstName
                && u.LastName == request.LastName
                && u.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsConflictWithoutCreatingUser()
    {
        var request = CreateRequest();
        var existingUser = User.Create(request.Email, "Existing", null, "existing-hash");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(existingUser);

        var result = await CreateHandler().Handle(new RegisterUserCommand(request), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User.AlreadyExists");
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
