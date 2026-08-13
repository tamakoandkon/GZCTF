using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Exercise (training range) event, recorded for the admin monitor.
/// Information includes flag submission, container start/stop, and cheating.
/// </summary>
[Index(nameof(ExerciseId))]
[Index(nameof(PublishTimeUtc))]
public class ExerciseEvent : FormattableData<EventType>
{
    [Key]
    [JsonIgnore]
    public int Id { get; set; }

    /// <summary>
    /// Publish time
    /// </summary>
    [Required]
    [JsonPropertyName("time")]
    public DateTimeOffset PublishTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Related username
    /// </summary>
    [JsonPropertyName("user")]
    public string UserName => User?.UserName ?? string.Empty;

    [JsonIgnore]
    public Guid? UserId { get; set; }

    [JsonIgnore]
    public UserInfo? User { get; set; }

    [JsonIgnore]
    public int ExerciseId { get; set; }

    [JsonIgnore]
    public PoolChallenge? Exercise { get; set; }

    internal static ExerciseEvent FromSubmission(ExerciseSubmission submission) =>
        new()
        {
            UserId = submission.UserId,
            ExerciseId = submission.ExerciseId,
            Type = EventType.FlagSubmit,
            Values =
            [
                submission.Status.ToString(),
                submission.Answer,
                submission.Exercise?.Title ?? string.Empty,
                submission.ExerciseId.ToString()
            ]
        };
}
