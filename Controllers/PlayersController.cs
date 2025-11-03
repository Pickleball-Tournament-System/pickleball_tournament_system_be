using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.DTOs.TeamsDto;
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
    public async Task<IActionResult> Update(string id, [FromBody] PlayerUpdateDto updatedDto)
    {
        // 1. Kiểm tra xem player có tồn tại không (logic này giữ nguyên)
        var existing = await _db.Users.Find(u => u.Id == id && u.Role == "Player").FirstOrDefaultAsync();
        if (existing == null)
            return NotFound("Player not found.");

        // 2. Tạo danh sách các định nghĩa cập nhật
        var updateDefinitionList = new List<UpdateDefinition<User>>();

        // 3. Xây dựng danh sách cập nhật từ DTO
        // Logic này gần như cũ, nhưng giờ ta đọc từ 'updatedDto'

        if (updatedDto.FullName != null)
        {
            updateDefinitionList.Add(Builders<User>.Update.Set(u => u.FullName, updatedDto.FullName));
        }

        if (updatedDto.Email != null)
        {
            updateDefinitionList.Add(Builders<User>.Update.Set(u => u.Email, updatedDto.Email));
        }

        if (updatedDto.Gender != null)
        {
            updateDefinitionList.Add(Builders<User>.Update.Set(u => u.Gender, updatedDto.Gender));
        }

        if (updatedDto.DateOfBirth.HasValue)
        {
            updateDefinitionList.Add(Builders<User>.Update.Set(u => u.DateOfBirth, updatedDto.DateOfBirth.Value));
        }

        if (updatedDto.PhoneNumber != null)
        {
            updateDefinitionList.Add(Builders<User>.Update.Set(u => u.PhoneNumber, updatedDto.PhoneNumber));
        }

        // 4. Kiểm tra xem có gì để cập nhật không
        if (updateDefinitionList.Count == 0)
        {
            return BadRequest("No valid fields provided for update.");
        }

        // 5. Kết hợp và thực hiện cập nhật (logic này giữ nguyên)
        var combinedUpdate = Builders<User>.Update.Combine(updateDefinitionList);

        try
        {
            await _db.Users.UpdateOneAsync(
                u => u.Id == id, // Điều kiện lọc
                combinedUpdate   // Các trường cần cập nhật
            );

            return Ok("Player updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
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
