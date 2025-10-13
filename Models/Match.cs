using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PickleballTournamentAPI.Models;

public class Match
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Trận đơn
    [BsonElement("playerAId")]
    public string? PlayerAId { get; set; }

    [BsonElement("playerBId")]
    public string? PlayerBId { get; set; }

    // Trận đôi
    [BsonElement("teamAPlayer1Id")]
    public string? TeamAPlayer1Id { get; set; }

    [BsonElement("teamAPlayer2Id")]
    public string? TeamAPlayer2Id { get; set; }

    [BsonElement("teamBPlayer1Id")]
    public string? TeamBPlayer1Id { get; set; }

    [BsonElement("teamBPlayer2Id")]
    public string? TeamBPlayer2Id { get; set; }

    [BsonElement("scoreA")]
    public int ScoreA { get; set; }

    [BsonElement("scoreB")]
    public int ScoreB { get; set; }

    [BsonElement("winnerTeam")]
    public string? WinnerTeam { get; set; } // "A" hoặc "B"

    [BsonElement("matchType")]
    public string MatchType { get; set; } = "single"; // "single" hoặc "double"

    [BsonElement("matchDate")]
    public DateTime MatchDate { get; set; } = DateTime.UtcNow;
}
