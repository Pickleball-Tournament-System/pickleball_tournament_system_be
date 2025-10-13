using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly MongoDBService _db;

    public PlayersController(MongoDBService db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var players = await _db.Players.Find(_ => true).ToListAsync();
        return Ok(players);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var player = await _db.Players.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (player == null) return NotFound();
        return Ok(player);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Player player)
    {
        await _db.Players.InsertOneAsync(player);
        return Ok(player);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Player updated)
    {
        var result = await _db.Players.ReplaceOneAsync(p => p.Id == id, updated);
        if (result.MatchedCount == 0) return NotFound();
        return Ok("Updated successfully");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Players.DeleteOneAsync(p => p.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok("Deleted successfully");
    }
}
