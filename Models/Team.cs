using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PickleballTournamentAPI.Models;

public class Team
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = null!;
    public List<string> Players { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedById { get; internal set; }
}
