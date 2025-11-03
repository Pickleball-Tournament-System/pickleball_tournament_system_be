using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;
using PickleballTournamentAPI.Services;
using PickleballTournamentAPI.Services.VnPayHelper;

namespace PickleballTournamentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly MongoDBService _db;
    private readonly IConfiguration _config;

    public PaymentController(MongoDBService db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // API NÀY DO SERVER VNPAY GỌI (SERVER-TO-SERVER)
    // Đây là nơi DUY NHẤT để xác nhận thanh toán và ghi danh
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnpayIpn()
    {
        string vnp_HashSecret = _config["VnPay:HashSecret"];
        var vnpayData = new VnPayLibrary();
        var requestData = HttpContext.Request.Query;

        foreach (var (key, value) in requestData)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpayData.AddResponseData(key, value.ToString());
            }
        }

        string vnp_TxnRef = vnpayData.GetResponseData("vnp_TxnRef"); // Mã giao dịch của BẠN
        string vnp_ResponseCode = vnpayData.GetResponseData("vnp_ResponseCode"); // "00" = OK
        string vnp_TransactionStatus = vnpayData.GetResponseData("vnp_TransactionStatus"); // "00" = OK
        string vnp_SecureHash = vnpayData.GetResponseData("vnp_SecureHash");

        bool checkSignature = vnpayData.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

        if (!checkSignature)
        {
            // Chữ ký không hợp lệ -> Giao dịch đáng ngờ
            return Ok(new { RspCode = "97", Message = "Invalid Signature" });
        }

        // Tìm giao dịch trong DB của bạn
        var transaction = await _db.Transactions.Find(t => t.Id == vnp_TxnRef).FirstOrDefaultAsync();

        if (transaction == null)
        {
            return Ok(new { RspCode = "01", Message = "Order not found" });
        }

        // Nếu transaction đã "Success" -> VNPAY gọi lại lần 2 -> Chỉ trả về OK
        if (transaction.Status == PaymentStatus.Success)
        {
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }

        // Thanh toán thành công (Mã 00)
        if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
        {
            // 1. CẬP NHẬT GIAO DỊCH
            var transUpdate = Builders<PaymentTransaction>.Update
                .Set(t => t.Status, PaymentStatus.Success)
                .Set(t => t.VnpayTransactionCode, vnpayData.GetResponseData("vnp_TransactionNo"));

            await _db.Transactions.UpdateOneAsync(t => t.Id == transaction.Id, transUpdate);

            // 2. GHI DANH VÀO GIẢI ĐẤU
            // (ParticipantId đã được lưu khi tạo transaction)
            var tournamentUpdate = Builders<Tournament>.Update
                .Push(t => t.ParticipantIds, transaction.ParticipantId);

            await _db.Tournaments.UpdateOneAsync(t => t.Id == transaction.TournamentId, tournamentUpdate);

            // (Gửi email xác nhận, bắn event, v.v... ở đây)

            // 3. Trả về cho VNPAY biết đã xử lý thành công
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }
        else
        {
            // Thanh toán thất bại
            var transUpdate = Builders<PaymentTransaction>.Update.Set(t => t.Status, PaymentStatus.Failed);
            await _db.Transactions.UpdateOneAsync(t => t.Id == transaction.Id, transUpdate);

            return Ok(new { RspCode = "02", Message = "Confirm Failed" });
        }
    }
}