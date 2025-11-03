namespace PickleballTournamentAPI.DTOs.TeamsDto
{
    public class CreateDoubleTeamDto
    {
        public string? Name { get; set; }
        public string Player1Id { get; set; }
        public string Player2Id { get; set; }
    }
}