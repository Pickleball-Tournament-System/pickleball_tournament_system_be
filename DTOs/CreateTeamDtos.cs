namespace PickleballTournamentAPI.DTOs;

public class CreateTeamDto
{
    public string Name { get; set; } = null!;
    public List<string> Players { get; set; } = new();
}
