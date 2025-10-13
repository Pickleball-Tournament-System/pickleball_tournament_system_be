using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PickleballTournamentAPI.Models;

namespace PickleballTournamentAPI.Services;

public class MongoDBService
{
    private readonly IMongoDatabase _database;

    public MongoDBService(IOptions<MongoDBSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Player> Players => _database.GetCollection<Player>("Players");
    public IMongoCollection<Match> Matches => _database.GetCollection<Match>("Matches");

}
