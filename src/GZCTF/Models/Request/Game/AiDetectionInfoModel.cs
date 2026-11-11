namespace GZCTF.Models.Request.Game;

/// <summary>
/// AI identification signal shown to game monitors for manual review.
/// </summary>
public class AiDetectionInfoModel
{
    public Guid Id { get; set; }
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int ParticipationId { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public DateTimeOffset SeenAt { get; set; }
}
