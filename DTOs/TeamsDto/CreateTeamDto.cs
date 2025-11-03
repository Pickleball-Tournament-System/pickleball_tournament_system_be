namespace PickleballTournamentAPI.DTOs.TeamsDto
{
    public class CreateTeamDto
    {
        public string Name { get; set; }

        // Sử dụng List để chấp nhận 1 hoặc 2 player
        public List<string> PlayerIds { get; set; } = new List<string>();
    }
}