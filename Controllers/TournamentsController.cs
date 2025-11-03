using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PickleballTournamentAPI.DTOs; // Thêm DTO
using PickleballTournamentAPI.DTOs.Tournament;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;
using System.Security.Claims;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TournamentsController : ControllerBase
{
    private readonly MongoDBService _db;
    private readonly VnPayService _vnPayService;

    // Tiêm VnPayService vào
    public TournamentsController(MongoDBService db, VnPayService vnPayService)
    {
        _db = db;
        _vnPayService = vnPayService;
    }

    // Sửa 1: Trả về string? (nullable) để an toàn
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    // ===== GET: Lấy tất cả giải đấu (công khai) =====
    [HttpGet]
    [AllowAnonymous] // Cho phép người chưa đăng nhập xem
    public async Task<IActionResult> GetPublicTournaments()
    {
        var tournaments = await _db.Tournaments.Find(t => t.IsPublic).ToListAsync();
        return Ok(tournaments);
    }

    // ===== GET: Lấy chi tiết 1 giải đấu =====
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTournamentById(string id)
    {
        var tournament = await _db.Tournaments.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (tournament == null) return NotFound();
        return Ok(tournament);
    }

    // ===== POST: Tạo giải đấu (Ví dụ) =====
    [HttpPost]
    [Authorize(Roles = "Admin")] // Chỉ Admin được tạo
    public async Task<IActionResult> CreateTournament([FromBody] Tournament newTournament)
    {
        // Sửa 2: Kiểm tra null từ GetCurrentUserId()
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User ID not found in token.");
        }

        newTournament.CreatedById = currentUserId; // An toàn
        await _db.Tournaments.InsertOneAsync(newTournament);
        return CreatedAtAction(nameof(GetTournamentById), new { id = newTournament.Id }, newTournament);
    }

    // ===== POST: Đăng ký (Giải Đôi) =====
    [HttpPost("{id}/register-team")]
    public async Task<IActionResult> RegisterTeam(string id, [FromBody] RegisterTeamDto dto)
    {
        var tournament = await _db.Tournaments.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (tournament == null) return NotFound("Tournament not found.");

        // 1. Kiểm tra logic
        if (tournament.Type != TournamentType.Double)
            return BadRequest("This is not a doubles tournament.");

        if (tournament.ParticipantIds.Count >= tournament.MaxParticipants)
            return BadRequest("Tournament is full.");

        var team = await _db.Teams.Find(t => t.Id == dto.TeamId).FirstOrDefaultAsync();
        if (team == null) return NotFound("Team not found.");
        if (team.Players.Count != 2) return BadRequest("Team must have 2 players.");

        // ParticipantIds đã được khởi tạo trong Model, không cần kiểm tra null
        if (tournament.ParticipantIds.Contains(team.Id))
            return BadRequest("Team is already registered.");

        // 2. Tạo giao dịch (Transaction) ở trạng thái "Pending"
        var transaction = new PaymentTransaction
        {
            TournamentId = tournament.Id,
            ParticipantId = team.Id, // Lưu TeamId
            Amount = tournament.EntryFee
        };

        // Sửa 3 (QUAN TRỌNG): Phải Insert TRƯỚC
        // Để MongoDB gán giá trị cho 'transaction.Id'
        await _db.Transactions.InsertOneAsync(transaction);

        // 3. Tạo URL VNPAY
        // 'transaction.Id' (string?) bây giờ đã có giá trị
        string paymentUrl = _vnPayService.CreatePaymentUrl(
            transaction.Id,
            transaction.Amount,
            $"Dang ky giai {tournament.Name} cho team {team.Name}"
        );

        // 4. Trả về URL cho frontend
        return Ok(new { PaymentUrl = paymentUrl });
    }

    // ===== POST: Đăng ký (Giải Đơn) =====
    [HttpPost("{id}/register-individual")]
    public async Task<IActionResult> RegisterIndividual(string id)
    {
        var tournament = await _db.Tournaments.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (tournament == null) return NotFound("Tournament not found.");

        // Sửa 2: Kiểm tra null từ GetCurrentUserId()
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User ID not found in token.");
        }

        // 1. Kiểm tra logic
        if (tournament.Type != TournamentType.Single)
            return BadRequest("This is not a singles tournament.");

        if (tournament.ParticipantIds.Count >= tournament.MaxParticipants)
            return BadRequest("Tournament is full.");

        if (tournament.ParticipantIds.Contains(currentUserId))
            return BadRequest("You are already registered.");

        // 2. Tạo giao dịch (Transaction) "Pending"
        var transaction = new PaymentTransaction
        {
            TournamentId = tournament.Id,
            ParticipantId = currentUserId, // Lưu UserId (an toàn)
            Amount = tournament.EntryFee
        };

        // Sửa 3 (QUAN TRỌNG): Phải Insert TRƯỚC
        await _db.Transactions.InsertOneAsync(transaction);

        // 3. Tạo URL VNPAY
        // 'transaction.Id' (string?) bây giờ đã có giá trị
        string paymentUrl = _vnPayService.CreatePaymentUrl(
            transaction.Id,
            transaction.Amount,
            $"Dang ky giai {tournament.Name}"
        );

        // 4. Trả về URL
        return Ok(new { PaymentUrl = paymentUrl });
    }
}