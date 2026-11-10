using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// A voluntary AI identifier observed on a dynamic challenge instance.
/// This is a review signal, not proof of rule violation.
/// </summary>
[Index(nameof(GameId), nameof(SeenAt))]
[Index(nameof(ContainerId))]
public class AiDetectionEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int GameId { get; set; }
    public int ChallengeId { get; set; }
    public int ParticipationId { get; set; }
    public int TeamId { get; set; }
    public Guid ContainerId { get; set; }

    [MaxLength(128)]
    public string ChallengeTitle { get; set; } = string.Empty;

    [MaxLength(128)]
    public string TeamName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ModelId { get; set; } = string.Empty;

    public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;
}
