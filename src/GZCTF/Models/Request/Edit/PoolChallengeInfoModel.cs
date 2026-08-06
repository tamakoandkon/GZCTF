using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Basic pool challenge information (Edit)
/// </summary>
public class PoolChallengeInfoModel
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    [Required(ErrorMessageResourceName = nameof(Resources.Program.Model_TitleRequired),
        ErrorMessageResourceType = typeof(Resources.Program))]
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Difficulty of the challenge
    /// </summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    /// <summary>
    /// Additional tags for the challenge
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Is the challenge enabled (globally)
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether this challenge is enabled in the training range
    /// </summary>
    public bool RangeEnabled { get; set; }

    /// <summary>
    /// Fixed score of this challenge in the training range
    /// </summary>
    public int RangeScore { get; set; } = 500;

    /// <summary>
    /// Number of games referencing this challenge
    /// </summary>
    public int ReferencedGamesCount { get; set; }

    internal static PoolChallengeInfoModel FromChallenge(PoolChallenge challenge) =>
        new()
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Category = challenge.Category,
            Type = challenge.Type,
            Difficulty = challenge.Difficulty,
            Tags = challenge.Tags,
            IsEnabled = challenge.IsEnabled,
            RangeEnabled = challenge.RangeEnabled,
            RangeScore = challenge.RangeScore,
            ReferencedGamesCount = challenge.ReferencingGames.Count
        };
}
