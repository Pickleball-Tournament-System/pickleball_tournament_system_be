using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PickleballTournamentAPI.Models
{
    public enum PaymentStatus
    {
        Pending, // Đang chờ thanh toán
        Success, // Thành công
        Failed   // Thất bại
    }

    public class PaymentTransaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Đây sẽ là OrderId (vnp_TxnRef) gửi cho VNPAY

        public string TournamentId { get; set; }

        // Có thể là UserId hoặc TeamId, tùy loại giải
        public string ParticipantId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? VnpayTransactionCode { get; set; } // Mã giao dịch của VNPAY
    }
}