using DeckDuel2.Data;
using DeckDuel2.Models;
using DeckDuel2.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DeckDuel2.Extensions;

public static class AuthExtensions
{
    /// <summary>
    /// Returns the User entity for the authenticated principal or null if not found / not authenticated.
    /// </summary>
    public static async Task<User?> GetAuthenticatedUserAsync(this ClaimsPrincipal? user, IUserRepository userRepo)
    {
        if (user == null) return null;

        var username = user.Identity?.Name ?? user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(username)) return null;

        return await userRepo.GetUserByUsernameAsync(username);
    }
}