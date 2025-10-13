using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PickleballTournamentAPI.Models;

public class Player
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("wins")]
    public int Wins { get; set; } = 0;

    [BsonElement("losses")]
    public int Losses { get; set; } = 0;
}
