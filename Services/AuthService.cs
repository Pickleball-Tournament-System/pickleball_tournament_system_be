using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using PickleballTournamentAPI.Controllers;
using PickleballTournamentAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

    public async Task<bool> RegisterAsync(UserRegisterDto dto)
    {
        var existingUser = await _db.Users.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
        if (existingUser != null) return false;

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber,
            Role = "Player" 
        };

        await _db.Users.InsertOneAsync(user);
        return true;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role), 
            new Claim("FullName", user.FullName ?? "")
        }),
            Expires = DateTime.UtcNow.AddHours(3),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

}
