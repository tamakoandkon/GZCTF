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

    /// <summary>
    /// A decoy flag was handed out on this team's instance for this challenge
    /// </summary>
    public bool MarkedFlagIssued { get; set; }

    /// <summary>
    /// One of those decoy values was submitted to the platform
    /// </summary>
    public bool MarkedFlagSubmitted { get; set; }

    /// <summary>
    /// When the decoy flag was issued, if any
    /// </summary>
    public DateTimeOffset? MarkedFlagIssuedAt { get; set; }

    /// <summary>
    /// When the decoy value was submitted, if it was
    /// </summary>
    public DateTimeOffset? MarkedFlagSubmittedAt { get; set; }
}
