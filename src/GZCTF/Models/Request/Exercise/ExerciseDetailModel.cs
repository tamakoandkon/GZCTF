using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Exercise;

public class ExerciseDetailModel
{
    /// <summary>
    /// Exercise ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Exercise title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Exercise content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Exercise category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Exercise hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Fixed score of the exercise in the range
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Difficulty of the exercise, used for tags, sorting, etc.
    /// </summary>
    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Additional tags for the exercise
    /// </summary>
    public List<string>? Tags { get; set; } = [];

    /// <summary>
    /// Exercise type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Whether the current user has solved the exercise
    /// </summary>
    public bool IsSolved { get; set; }

    /// <summary>
    /// Number of submissions made by the current user
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Maximum number of submissions allowed (0 = no limit)
    /// </summary>
    public int SubmissionLimit { get; set; }

    /// <summary>
    /// Flag context
    /// </summary>
    public ClientFlagContext Context { get; set; } = null!;

    internal static ExerciseDetailModel FromInstance(ExerciseInstance instance) =>
        new()
        {
            Id = instance.ExerciseId,
            Content = instance.Exercise.Content,
            Hints = instance.Exercise.Hints,
            Score = instance.Exercise.RangeScore,
            Difficulty = instance.Exercise.Difficulty,
            Category = instance.Exercise.Category,
            Tags = instance.Exercise.Tags,
            Title = instance.Exercise.Title,
            Type = instance.Exercise.Type,
            IsSolved = instance.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0),
            SubmissionLimit = instance.Exercise.SubmissionLimit,
            Context = new()
            {
                InstanceEntry = instance.Container?.Entry,
                CloseTime = instance.Container?.ExpectStopAt,
                Url = instance.AttachmentUrl,
                FileSize = instance.Attachment?.FileSize
            }
        };
}
