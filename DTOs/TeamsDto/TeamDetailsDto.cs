namespace PickleballTournamentAPI.DTOs.TeamsDto
{
    public class TeamDetailsDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedById { get; set; } // Hiển thị ai đã tạo

        // Trả về danh sách thông tin player, không phải ID
        public List<PlayerInfoSmallDto> Players { get; set; }
    }
}