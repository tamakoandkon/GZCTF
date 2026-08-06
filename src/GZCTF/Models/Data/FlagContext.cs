using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId))]
public class FlagContext
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Flag content
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string Flag { get; set; } = string.Empty;

    /// <summary>
    /// Whether it is occupied
    /// </summary>
    public bool IsOccupied { get; set; }

    #region Db Relationship

    /// <summary>
    /// Attachment ID
    /// </summary>
    public int? AttachmentId { get; set; }

    /// <summary>
    /// Attachment
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// Challenge ID
    /// </summary>
    public int? ChallengeId { get; set; }

    /// <summary>
    /// Challenge
    /// </summary>
    public GameChallenge? Challenge { get; set; }

    /// <summary>
    /// Pool challenge ID
    /// </summary>
    public int? PoolChallengeId { get; set; }

    /// <summary>
    /// Pool challenge
    /// </summary>
    public PoolChallenge? PoolChallenge { get; set; }

    #endregion Db Relationship
}
