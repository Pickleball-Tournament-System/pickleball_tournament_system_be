namespace PickleballTournamentAPI.DTOs.Tournament
{
    public class RegisterTeamDto
    {
        // Chỉ cần TeamId để biết team nào đang đăng ký
        public string TeamId { get; set; }
    }
}