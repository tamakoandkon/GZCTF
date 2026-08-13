using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    [JsonPropertyName("time")]
    public DateTimeOffset SubmitTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Related username (serialized as "user")
    /// </summary>
    [JsonPropertyName("user")]
    public string UserName => User?.UserName ?? string.Empty;

    /// <summary>
    /// Related challenge title (serialized as "challenge")
    /// </summary>
    [JsonPropertyName("challenge")]
    public string ChallengeName => Exercise?.Title ?? string.Empty;

    #region Db Relationship

    /// <summary>
    /// User ID
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// User who submitted
    /// </summary>
    [JsonIgnore]
    public UserInfo User { get; set; } = null!;

    /// <summary>
    /// Pool challenge ID
    /// </summary>
    [JsonIgnore]
    public int ExerciseId { get; set; }

    /// <summary>
    /// Pool challenge
    /// </summary>
    [JsonIgnore]
    public PoolChallenge Exercise { get; set; } = null!;

    #endregion
}
