using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;
using System.Collections.Generic;
using System.Linq; // Cần cho .Any() và .Distinct()
using System.Security.Claims;
using PickleballTournamentAPI.DTOs.TeamsDto; // Đảm bảo using đúng DTOs

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

    // Lấy ID của người dùng hiện tại từ token (JWT)
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    // Hàm private helper để "join" thông tin Player
    private async Task<TeamDetailsDto> MapToTeamDetailsDto(Team team)
    {
        if (team.Players == null || !team.Players.Any())
        {
            return new TeamDetailsDto
            {
                Id = team.Id,
                Name = team.Name,
                CreatedAt = team.CreatedAt,
                CreatedById = team.CreatedById,
                Players = new List<PlayerInfoSmallDto>() // Trả về danh sách rỗng
            };
        }

        var playerFilter = Builders<User>.Filter.In(u => u.Id, team.Players);
        var players = await _db.Users.Find(playerFilter)
            .Project(u => new PlayerInfoSmallDto
            {
                Id = u.Id,
                FullName = u.FullName
            })
            .ToListAsync();

        return new TeamDetailsDto
        {
            Id = team.Id,
            Name = team.Name,
            CreatedAt = team.CreatedAt,
            CreatedById = team.CreatedById,
            Players = players
        };
    }

    // ===== GET: Lấy tất cả team =====
    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _db.Teams.Find(_ => true).ToListAsync();
        var responseList = new List<TeamDetailsDto>();
        foreach (var team in teams)
        {
            responseList.Add(await MapToTeamDetailsDto(team));
        }
        return Ok(responseList);
    }

    // ===== GET: Lấy team theo ID =====
    // (Route này phải phân biệt với GetTeamByPlayerId)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeamById(string id)
    {
        var team = await _db.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (team == null)
            return NotFound("Team not found.");
        return Ok(await MapToTeamDetailsDto(team));
    }

    // ===== GET: Lấy team theo Player ID (ĐÃ SỬA LỖI ROUTE) =====
    [HttpGet("ByPlayer/{playerId}")]
    public async Task<IActionResult> GetTeamByPlayerId(string playerId)
    {
        var filter = Builders<Team>.Filter.AnyEq(t => t.Players, playerId);
        var team = await _db.Teams.Find(filter).FirstOrDefaultAsync();

        if (team == null)
            return NotFound("This player is not on any team.");

        return Ok(await MapToTeamDetailsDto(team));
    }


    // ===== POST: Tạo team mới (ĐÃ SỬA) =====
    // (Cho phép tạo với 1 hoặc 2 người)
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        // --- Validation ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Team name is required.");

        if (dto.PlayerIds == null || !dto.PlayerIds.Any())
            return BadRequest("At least one player ID is required to create a team.");

        if (dto.PlayerIds.Count > 2)
            return BadRequest("A team can have a maximum of 2 players.");

        if (dto.PlayerIds.Count != dto.PlayerIds.Distinct().Count())
            return BadRequest("Duplicate player IDs are not allowed.");

        // Kiểm tra tất cả player IDs có hợp lệ không
        var playerFilter = Builders<User>.Filter.In(u => u.Id, dto.PlayerIds) & Builders<User>.Filter.Eq(u => u.Role, "Player");
        var validPlayerCount = await _db.Users.Find(playerFilter).CountDocumentsAsync();

        if (validPlayerCount != dto.PlayerIds.Count)
            return NotFound("One or more player IDs are not valid players.");

        // Kiểm tra xem các player này đã ở team khác chưa
        var existingTeamFilter = Builders<Team>.Filter.AnyIn(t => t.Players, dto.PlayerIds);
        if (await _db.Teams.Find(existingTeamFilter).AnyAsync())
            return BadRequest("One or more players are already on another team.");
        // --- Kết thúc Validation ---

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized("User ID could not be determined from token.");

        var newTeam = new Team
        {
            Name = dto.Name,
            Players = dto.PlayerIds, // Gán danh sách 1 hoặc 2 player
            CreatedAt = DateTime.UtcNow,
            CreatedById = currentUserId
        };

        await _db.Teams.InsertOneAsync(newTeam);

        var responseDto = await MapToTeamDetailsDto(newTeam);
        return CreatedAtAction(nameof(GetTeamById), new { id = newTeam.Id }, responseDto);
    }

    // ===== PUT: Cập nhật tên Team =====
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam(string id, [FromBody] TeamUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Team name is required.");

        var team = await _db.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (team == null)
            return NotFound("Team not found.");

        var currentUserId = GetCurrentUserId();
        if (team.CreatedById != currentUserId)
        {
            return Forbid("Only the team creator can update the team name.");
        }

        var update = Builders<Team>.Update.Set(t => t.Name, dto.Name);
        await _db.Teams.UpdateOneAsync(t => t.Id == id, update);

        return Ok("Team name updated successfully.");
    }

    // ===== PUT: Thêm Player vào Team (API MỚI) =====
    // (Chỉ người tạo mới được thêm)
    [HttpPut("{id}/addplayer")]
    public async Task<IActionResult> AddPlayer(string id, [FromBody] AddPlayerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlayerId))
            return BadRequest("PlayerId is required.");

        var team = await _db.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (team == null)
            return NotFound("Team not found.");

        // KIỂM TRA QUYỀN: Chỉ người tạo team được thêm
        var currentUserId = GetCurrentUserId();
        if (team.CreatedById != currentUserId)
        {
            return Forbid("Only the team creator can add players.");
        }

        // KIỂM TRA LOGIC:
        // 1. Kiểm tra team đã đầy chưa (giới hạn 2 người)
        if (team.Players.Count >= 2)
            return BadRequest("Team is full. Cannot add more than 2 players.");

        // 2. Kiểm tra player có tồn tại không
        var player = await _db.Users.Find(u => u.Id == dto.PlayerId && u.Role == "Player").FirstOrDefaultAsync();
        if (player == null)
            return NotFound("Player not found or is not a valid player.");

        // 3. Kiểm tra player đã ở trong team này chưa
        if (team.Players.Contains(dto.PlayerId))
            return BadRequest("Player is already in this team.");

        // 4. Kiểm tra player đã ở team khác chưa
        var playerTeamFilter = Builders<Team>.Filter.AnyEq(t => t.Players, dto.PlayerId);
        if (await _db.Teams.Find(playerTeamFilter).AnyAsync())
            return BadRequest("This player is already on another team.");

        // Thêm player vào mảng 'Players'
        var update = Builders<Team>.Update.Push(t => t.Players, dto.PlayerId);
        await _db.Teams.UpdateOneAsync(t => t.Id == id, update);

        return Ok("Player added successfully.");
    }


    // ===== PUT: Kick Player =====
    [HttpPut("{id}/kick")]
    public async Task<IActionResult> KickPlayer(string id, [FromBody] KickPlayerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlayerId))
            return BadRequest("PlayerId is required.");

        var team = await _db.Teams.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (team == null)
            return NotFound("Team not found.");

        var currentUserId = GetCurrentUserId();
        if (team.CreatedById != currentUserId)
        {
            return Forbid("Only the team creator can kick players.");
        }

        if (!team.Players.Contains(dto.PlayerId))
            return NotFound("Player not found in this team.");

        var update = Builders<Team>.Update.Pull(t => t.Players, dto.PlayerId);
        await _db.Teams.UpdateOneAsync(t => t.Id == id, update);

        return Ok("Player kicked from team successfully.");
    }
}