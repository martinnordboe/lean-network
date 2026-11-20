using game_match_api.Models;

namespace game_match_api.dto
{
    /// <summary>
    /// HTTP response DTO for activity objects.
    /// </summary>
    public class ActivityDto
    {
        public int Id { get; init; }
        public int OwnerId { get; init; }
        public string ActivityTitle { get; init; }
        public string? ActivityInfo { get; init; }
        public string? Address { get; init; } // Kunne laves til et adresse objekt
        public DateTime? StartTime { get; init; }
        public int MaxParticipants { get; init; }
        public List<User?> Participants { get; init; }

        public ActivityDto(int id, int ownerId, string activityTitle, string? activityInfo, string? address, DateTime? startTime, int maxParticipants, List<User?> participants)
        {
            Id = id;
            OwnerId = ownerId;
            ActivityTitle = activityTitle;
            ActivityInfo = activityInfo;
            Address = address;
            StartTime = startTime;
            MaxParticipants = maxParticipants;
            Participants = participants;
        }
    }
}
