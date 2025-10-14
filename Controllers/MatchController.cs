using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly MongoDBService _db;

    public MatchesController(MongoDBService db)
    {
        _db = db;
    }

    // ===== GET: Lấy tất cả trận đấu =====
    [HttpGet]
    public async Task<IActionResult> GetAllMatches()
    {
        var matches = await _db.Matches.Find(_ => true).ToListAsync();
        return Ok(matches);
    }

    // ===== GET: Lấy 1 trận đấu theo ID =====
    [HttpGet("{id:length(24)}")]
    public async Task<IActionResult> GetMatchById(string id)
    {
        var match = await _db.Matches.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (match == null)
            return NotFound(new { message = "Match not found" });

        // Gắn thông tin người chơi / đội
        var playerIds = new List<string?>();

        if (match.MatchType == "single")
        {
            playerIds.Add(match.PlayerAId);
            playerIds.Add(match.PlayerBId);
        }
        else // double
        {
            playerIds.AddRange(new[] { match.TeamAPlayer1Id, match.TeamAPlayer2Id, match.TeamBPlayer1Id, match.TeamBPlayer2Id });
        }

        var players = await _db.Users
            .Find(u => playerIds.Contains(u.Id))
            .ToListAsync();

        return Ok(new
        {
            match,
            players
        });
    }

    // ===== POST: Tạo trận đấu mới =====
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMatch([FromBody] Match match)
    {
        if (match.MatchType != "single" && match.MatchType != "double")
            return BadRequest(new { message = "MatchType must be 'single' or 'double'." });

        match.Id = null;
        match.MatchDate = DateTime.UtcNow;
        match.ScoreA = 0;
        match.ScoreB = 0;
        match.WinnerTeam = null;

        await _db.Matches.InsertOneAsync(match);
        return Ok(new { message = "Match created successfully.", match });
    }

    // ===== PUT: Cập nhật kết quả trận đấu =====
    [HttpPut("{id:length(24)}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMatch(string id, [FromBody] Match updated)
    {
        var existing = await _db.Matches.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (existing == null)
            return NotFound(new { message = "Match not found" });

        updated.Id = id;
        updated.MatchDate = existing.MatchDate; // giữ nguyên ngày tạo

        await _db.Matches.ReplaceOneAsync(m => m.Id == id, updated);
        return Ok(new { message = "Match updated successfully.", match = updated });
    }

    // ===== PATCH: Cập nhật điểm số và xác định đội thắng =====
    [HttpPatch("{id:length(24)}/result")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateResult(string id, [FromBody] Match result)
    {
        var match = await _db.Matches.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (match == null)
            return NotFound(new { message = "Match not found" });

        match.ScoreA = result.ScoreA;
        match.ScoreB = result.ScoreB;
        match.WinnerTeam = result.ScoreA > result.ScoreB ? "A" : result.ScoreA < result.ScoreB ? "B" : "Draw";

        await _db.Matches.ReplaceOneAsync(m => m.Id == id, match);
        return Ok(new { message = "Match result updated.", match });
    }

    // ===== DELETE: Xóa trận đấu =====
    [HttpDelete("{id:length(24)}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMatch(string id)
    {
        var result = await _db.Matches.DeleteOneAsync(m => m.Id == id);
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Match not found" });

        return Ok(new { message = "Match deleted successfully." });
    }
}
