using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PickleballTournamentAPI.Models;
using MongoDB.Driver;
using BCrypt.Net;

namespace PickleballTournamentAPI.Services;

public class AuthService
{
    private readonly MongoDBService _db;
    private readonly IConfiguration _config;

    public AuthService(MongoDBService db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
        var existing = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existing != null) return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Email = email, PasswordHash = hash };
        await _db.Users.InsertOneAsync(user);
        return true;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
