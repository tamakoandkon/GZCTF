using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// A decoy flag that was handed to a response carrying a self-reported AI identifier.
/// The value is random on every issuance, so a submission of this exact string can be
/// traced back to the response that produced it.
/// </summary>
/// <remarks>
/// This is a review signal, not proof: a person can copy the value, and a player can
/// set the identifier header manually. The real flag is never modified.
/// </remarks>
[Index(nameof(GameId), nameof(Flag), IsUnique = true)]
[Index(nameof(ContainerId))]
public class MarkedFlag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int GameId { get; set; }

    public int ChallengeId { get; set; }

    public int ParticipationId { get; set; }

    public int TeamId { get; set; }

    public Guid ContainerId { get; set; }

    [MaxLength(128)]
    public string TeamName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ChallengeTitle { get; set; } = string.Empty;

    /// <summary>
    /// Self-reported identifier that triggered this issuance
    /// </summary>
    [MaxLength(128)]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Decoy value, same length and shape as the real flag
    /// </summary>
    [MaxLength(Limits.MaxFlagLength)]
    public string Flag { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public int? SubmissionId { get; set; }

    public Guid? SubmittedByUserId { get; set; }
}
