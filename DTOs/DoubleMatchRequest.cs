namespace PickleballTournamentAPI.DTOs;

public class DoubleMatchRequest
{
    public string TeamAPlayer1Id { get; set; } = string.Empty;
    public string TeamAPlayer2Id { get; set; } = string.Empty;
    public string TeamBPlayer1Id { get; set; } = string.Empty;
    public string TeamBPlayer2Id { get; set; } = string.Empty;
    public int ScoreA { get; set; }
    public int ScoreB { get; set; }
}
