namespace GZCTF.Models.Request.Game;

/// <summary>
/// Ledger entry for a decoy flag handed to an instance that reported a model identifier.
/// Shows the exact issued value next to the answer the team submitted, so a monitor can
/// verify the match by eye instead of trusting a badge.
/// </summary>
public class AiMarkedFlagInfoModel
{
    public Guid Id { get; set; }
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int ParticipationId { get; set; }
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The decoy value that was written into that one response
    /// </summary>
    public string Flag { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>
    /// When the exact value was submitted, if it was
    /// </summary>
    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public int? SubmissionId { get; set; }

    /// <summary>
    /// The answer stored in that submission (verbatim)
    /// </summary>
    public string? SubmittedAnswer { get; set; }

    public DateTimeOffset? SubmissionTime { get; set; }

    public Guid? SubmittedByUserId { get; set; }
}
