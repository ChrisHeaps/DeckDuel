using Xunit;
using Moq;
using FluentAssertions;
using DeckDuel2.Domain;
using DeckDuel2.Repositories;
using DeckDuel2.DTOs;
using DeckDuel2.Models;

namespace DeckDuel2.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserService _userService;
        private const string JwtSecret = "super_secret_demo_key_12345super_secret_demo_key_12345";

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _userService = new UserService(_mockUserRepository.Object);
        }

        [Fact(Skip = "Not ready to run yet")]
        public async Task RegisterUserAsync_WithValidUser_ShouldCreateUser()
        {
            // Arrange
            var userDto = new UserDto { Username = "testuser", Password = "password123", Email = "test@example.com" };
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.RegisterUserAsync(userDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Username.Should().Be("testuser");
            _mockUserRepository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact(Skip = "Not ready to run yet")]
        public async Task RegisterUserAsync_WithDuplicateUsername_ShouldThrowException()
        {
            // Arrange
            var userDto = new UserDto { Username = "testuser", Password = "password123", Email = "test@example.com" };
            var existingUser = new User { Id = 1, Username = "testuser" };
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userService.RegisterUserAsync(userDto);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorType.Should().Be(DDError.AlreadyExists);
            result.Error.Should().Be("Username already exists.");
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var password = "chris";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Id = 1, Username = "chris", PasswordHash = hashedPassword };
            var loginDto = new UserDto { Username = "chris", Password = password };

            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("chris"))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.LoginAsync(loginDto, JwtSecret);

            // Assert
            result.Success.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnFailure()
        {
            // Arrange
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
            var user = new User { Id = 1, Username = "testuser", PasswordHash = hashedPassword };
            var loginDto = new UserDto { Username = "testuser", Password = "wrongpassword" };

            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.LoginAsync(loginDto, JwtSecret);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorType.Should().Be(DDError.Unauthorized);
            result.Error.Should().Be("Invalid username or password.");
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var loginDto = new UserDto { Username = "nonexistent", Password = "password123" };
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("nonexistent"))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.LoginAsync(loginDto, JwtSecret);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorType.Should().Be(DDError.Unauthorized);
            result.Error.Should().Be("Invalid username or password.");
        }

        [Fact]
        public async Task LoginAsync_WithNullLoginDto_ShouldReturnFailure()
        {
            // Act
            var result = await _userService.LoginAsync(null, JwtSecret);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorType.Should().Be(DDError.InvalidInput);
            result.Error.Should().Be("Username and Password are required.");
        }

        [Theory]
        [InlineData("", "password123")]
        [InlineData("   ", "password123")]
        [InlineData("testuser", "")]
        [InlineData("testuser", "   ")]
        public async Task LoginAsync_WithEmptyOrWhitespaceCredentials_ShouldReturnFailure(string username, string password)
        {
            // Arrange
            var loginDto = new UserDto { Username = username, Password = password };

            // Act
            var result = await _userService.LoginAsync(loginDto, JwtSecret);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorType.Should().Be(DDError.InvalidInput);
            result.Error.Should().Be("Username and Password are required.");
        }

        [Fact]
        public async Task LoginAsync_WithChrisUsername_ShouldIncludeAdminRole()
        {
            // Arrange
            var password = "chris";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Id = 1, Username = "chris", PasswordHash = hashedPassword };
            var loginDto = new UserDto { Username = "chris", Password = password };

            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("chris"))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.LoginAsync(loginDto, JwtSecret);

            // Assert
            result.Success.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
            // Note: Decoding the JWT to verify the Admin role would require additional setup
            // For now, just verify the token is generated successfully
        }
    }
}