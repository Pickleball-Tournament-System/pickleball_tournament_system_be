using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly MongoDBService _db;

    public PlayersController(MongoDBService db)
    {
        _db = db;
    }

    // ===== GET: Lấy tất cả người chơi =====
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var players = await _db.Users.Find(u => u.Role == "Player").ToListAsync();
        return Ok(players);
    }

    // ===== GET: Lấy người chơi theo ID =====
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var player = await _db.Users.Find(u => u.Id == id && u.Role == "Player").FirstOrDefaultAsync();
        if (player == null)
            return NotFound("Player not found");
        return Ok(player);
    }

    // ===== PUT: Cập nhật thông tin player =====
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] User updated)
    {
        var existing = await _db.Users.Find(u => u.Id == id && u.Role == "Player").FirstOrDefaultAsync();
        if (existing == null)
            return NotFound("Player not found.");

        updated.Id = id;
        updated.Role = "Player"; // đảm bảo không bị đổi role

        await _db.Users.ReplaceOneAsync(u => u.Id == id, updated);
        return Ok("Player updated successfully.");
    }

    // ===== DELETE: Xóa player =====
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Users.DeleteOneAsync(u => u.Id == id && u.Role == "Player");
        if (result.DeletedCount == 0)
            return NotFound("Player not found.");
        return Ok("Player deleted successfully.");
    }
}
