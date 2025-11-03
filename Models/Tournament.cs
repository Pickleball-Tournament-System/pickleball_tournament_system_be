using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace PickleballTournamentAPI.Models
{
    public enum TournamentType
    {
        Single,
        Double
    }

    public class Tournament
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; }
        public TournamentType Type { get; set; } // Đơn hoặc Đôi
        public decimal EntryFee { get; set; } // Lệ phí tham gia
        public bool IsPublic { get; set; } = true;

        // Nếu 'PasswordHash' không null, giải đấu yêu cầu mật khẩu
        public string? PasswordHash { get; set; }

        // Danh sách ID của người tham gia
        // Nếu là giải "Single" -> List<UserId>
        // Nếu là giải "Double" -> List<TeamId>
        public List<string> ParticipantIds { get; set; } = new List<string>();

        public int MaxParticipants { get; set; }
        public string CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}