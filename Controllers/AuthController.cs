using Microsoft.AspNetCore.Mvc;
using PickleballTournamentAPI.Services;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
    {
        var success = await _auth.RegisterAsync(dto.Email, dto.Password);
        if (!success) return BadRequest("User already exists.");
        return Ok("Registered successfully!");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        var token = await _auth.LoginAsync(dto.Email, dto.Password);
        if (token == null) return Unauthorized("Invalid credentials.");
        return Ok(new { token });
    }
}

public class UserRegisterDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UserLoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
