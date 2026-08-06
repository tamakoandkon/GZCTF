using System.Text.Json.Serialization;

namespace GZCTF.Models.Data;

public class PoolChallenge : Challenge
{
    /// <summary>
    /// Difficulty of the challenge, used for tags, sorting, etc.
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Additional tags for the challenge
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    /// <summary>
    /// Admin-only note, never exposed to user-facing APIs
    /// </summary>
    [JsonIgnore]
    public string? Note { get; set; }

    /// <summary>
    /// Whether this challenge is enabled in the training range
    /// </summary>
    public bool RangeEnabled { get; set; }

    /// <summary>
    /// Fixed score of the challenge in the training range
    /// </summary>
    public int RangeScore { get; set; } = 500;

    #region Db Relationship

    /// <summary>
    /// Game challenges referencing this pool challenge
    /// </summary>
    public List<GameChallenge> ReferencingGames { get; set; } = [];

    /// <summary>
    /// Range instances of this challenge
    /// </summary>
    public List<ExerciseInstance> ExerciseInstances { get; set; } = [];

    /// <summary>
    /// Range submissions of this challenge
    /// </summary>
    public List<ExerciseSubmission> ExerciseSubmissions { get; set; } = [];

    #endregion
}
