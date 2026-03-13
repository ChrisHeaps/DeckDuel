using BCrypt.Net;
using DeckDuel2.DTOs;
using DeckDuel2.Models;
using DeckDuel2.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DeckDuel2.Domain
{
    public interface IUserService
    {
        Task<DDResult<User>> RegisterUserAsync(UserDto userDto);
        Task<DDResult<string>> LoginAsync(UserDto loginDto, string jwtSecret);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<DDResult<User>> RegisterUserAsync(UserDto userDto)
        {
            //TODO: Validate userDto fields (username length, email format, password strength)
            
            var existingUser = await _userRepository.GetUserByUsernameAsync(userDto.Username);
            if (existingUser != null)
                return DDResult<User>.Fail(DDError.AlreadyExists, "Username already exists.");

            var existingNickName = await _userRepository.GetUserByInGameNameAsync(userDto.InGameName);
            if (existingUser != null)
                return DDResult<User>.Fail(DDError.AlreadyExists, "Nickname already exists.");

            var user = new User
            {
                Username = userDto.Username,
                InGameName = userDto.InGameName,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password)
            };

            await _userRepository.AddUserAsync(user);
            return DDResult<User>.Ok(user);
        }

        public async Task<DDResult<string>> LoginAsync(UserDto loginDto, string jwtSecret)
        {
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || 
                string.IsNullOrWhiteSpace(loginDto.Password))
                return DDResult<string>.Fail(DDError.InvalidInput, "Username and Password are required.");

            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return DDResult<string>.Fail(DDError.Unauthorized, "Invalid username or password.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, loginDto.Username),
                new Claim(ClaimTypes.Role, loginDto.Username == "chris" ? "Admin" : "Standard")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds
            );

            return DDResult<string>.Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}