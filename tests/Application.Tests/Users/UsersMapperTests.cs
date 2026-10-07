using Application.Users;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.Users;

public class UsersMapperTests
{
    [Fact]
    public void ToRegisterUserResponse_MapsIdAndEmail()
    {
        var user = User.Create("ada@test.com", "Ada", "Lovelace", "hashed-password");

        var response = user.ToRegisterUserResponse();

        response.Id.Should().Be(user.Id);
        response.Email.Should().Be(user.Email);
    }
}
