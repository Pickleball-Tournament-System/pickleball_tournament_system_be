using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PickleballTournamentAPI.Models;

public class Player
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("FullName")]
    public string FullName { get; set; } = "";

    [BsonElement("Gender")]
    public string Gender { get; set; } = "";

    [BsonElement("DateOfBirth")]
    public DateTime DateOfBirth { get; set; }

    [BsonElement("Phone")]
    public string Phone { get; set; } = "";

    [BsonElement("Email")]
    public string Email { get; set; } = "";

    [BsonElement("Role")]
    public string Role { get; set; } = "Player";

    [BsonElement("wins")]
    public int Wins { get; set; } = 0;

    [BsonElement("losses")]
    public int Losses { get; set; } = 0;
}
