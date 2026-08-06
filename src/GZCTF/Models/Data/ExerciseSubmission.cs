using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(ExerciseId))]
[Index(nameof(UserId), nameof(ExerciseId))]
public class ExerciseSubmission
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Submitted answer string
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Status of the submitted answer
    /// </summary>
    public AnswerResult Status { get; set; } = AnswerResult.FlagSubmitted;

    /// <summary>
    /// Time the answer was submitted
    /// </summary>
    public DateTimeOffset SubmitTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User who submitted
    /// </summary>
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// Pool challenge ID
    /// </summary>
    public int ExerciseId { get; set; }

    /// <summary>
    /// Pool challenge
    /// </summary>
    public PoolChallenge Exercise { get; set; } = null!;

    #endregion
}
