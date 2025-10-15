using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.DTOs;
using PickleballTournamentAPI.Services;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly MongoDBService _db;

    public TeamsController(MongoDBService db)
    {
        _db = db;
    }

    // GET all teams
    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _db.Teams.Find(_ => true).ToListAsync();
        return Ok(teams);
    }

    // POST create a new team
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Team name is required.");

        var newTeam = new Team
        {
            Name = dto.Name,
            Players = dto.Players,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Teams.InsertOneAsync(newTeam);
        return Ok(newTeam);
    }

}
