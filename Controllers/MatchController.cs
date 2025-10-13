using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;
using PickleballTournamentAPI.DTOs;

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

    // === Trận đơn (đã có ở trước) ===
    [HttpPost]
    public async Task<IActionResult> CreateSingle([FromBody] Match match)
    {
        match.MatchType = "single";
        match.WinnerTeam = match.ScoreA > match.ScoreB ? "A" : "B";
        await _db.Matches.InsertOneAsync(match);

        // Cập nhật win/loss
        var winnerId = match.WinnerTeam == "A" ? match.PlayerAId : match.PlayerBId;
        var loserId = match.WinnerTeam == "A" ? match.PlayerBId : match.PlayerAId;

        if (winnerId != null)
        {
            var winner = await _db.Players.Find(p => p.Id == winnerId).FirstOrDefaultAsync();
            if (winner != null)
            {
                winner.Wins++;
                await _db.Players.ReplaceOneAsync(p => p.Id == winner.Id, winner);
            }
        }

        if (loserId != null)
        {
            var loser = await _db.Players.Find(p => p.Id == loserId).FirstOrDefaultAsync();
            if (loser != null)
            {
                loser.Losses++;
                await _db.Players.ReplaceOneAsync(p => p.Id == loser.Id, loser);
            }
        }

        return Ok(match);
    }

    // === Trận đôi mới ===
    [HttpPost("doubles")]
    public async Task<IActionResult> CreateDouble([FromBody] DoubleMatchRequest req)
    {
        var match = new Match
        {
            MatchType = "double",
            TeamAPlayer1Id = req.TeamAPlayer1Id,
            TeamAPlayer2Id = req.TeamAPlayer2Id,
            TeamBPlayer1Id = req.TeamBPlayer1Id,
            TeamBPlayer2Id = req.TeamBPlayer2Id,
            ScoreA = req.ScoreA,
            ScoreB = req.ScoreB,
            WinnerTeam = req.ScoreA > req.ScoreB ? "A" : "B"
        };

        await _db.Matches.InsertOneAsync(match);

        var teamAWon = req.ScoreA > req.ScoreB;

        // Cập nhật kết quả cho cả 4 người
        var playerIds = new List<string>
        {
            req.TeamAPlayer1Id,
            req.TeamAPlayer2Id,
            req.TeamBPlayer1Id,
            req.TeamBPlayer2Id
        };

        var players = await _db.Players.Find(p => playerIds.Contains(p.Id!)).ToListAsync();

        foreach (var player in players)
        {
            bool isTeamA = player.Id == req.TeamAPlayer1Id || player.Id == req.TeamAPlayer2Id;

            if ((isTeamA && teamAWon) || (!isTeamA && !teamAWon))
                player.Wins++;
            else
                player.Losses++;

            await _db.Players.ReplaceOneAsync(p => p.Id == player.Id, player);
        }

        return Ok(match);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var matches = await _db.Matches.Find(_ => true).ToListAsync();
        return Ok(matches);
    }
}
