using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Request.Game;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// A game that references a pool challenge
/// </summary>
/// <param name="GameId">Game ID</param>
/// <param name="GameTitle">Game title</param>
/// <param name="GameChallengeId">The linked game challenge ID</param>
public record ReferencedGameInfo(int GameId, string GameTitle, int GameChallengeId);

/// <summary>
/// Pool challenge detailed information (Edit)
/// </summary>
public class PoolChallengeEditDetailModel
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
    /// Challenge content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    [Required]
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge type
    /// </summary>
    [Required]
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string> Hints { get; set; } = [];

    /// <summary>
    /// Flag template, used to generate dynamic flags
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Is the challenge enabled (globally)
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Difficulty of the challenge
    /// </summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    /// <summary>
    /// Additional tags for the challenge
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Admin-only note
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Whether this challenge is enabled in the training range
    /// </summary>
    public bool RangeEnabled { get; set; }

    /// <summary>
    /// Fixed score of this challenge in the training range
    /// </summary>
    public int RangeScore { get; set; } = 500;

    /// <summary>
    /// Number of users who solved this challenge in the range
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Unified file name (only for dynamic attachments)
    /// </summary>
    public string? FileName { get; set; } = string.Empty;

    /// <summary>
    /// Challenge attachment
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// Test container
    /// </summary>
    public ContainerInfoModel? TestContainer { get; set; }

    /// <summary>
    /// Challenge Flag information
    /// </summary>
    [Required]
    public List<FlagInfoModel> Flags { get; set; } = [];

    /// <summary>
    /// Image name and tag
    /// </summary>
    [Required]
    public string? ContainerImage { get; set; } = string.Empty;

    /// <summary>
    /// Memory limit (MB)
    /// </summary>
    public int? MemoryLimit { get; set; } = 64;

    /// <summary>
    /// CPU limit (0.1 CPUs)
    /// </summary>
    public int? CPUCount { get; set; } = 1;

    /// <summary>
    /// Storage limit (MB)
    /// </summary>
    public int? StorageLimit { get; set; } = 256;

    /// <summary>
    /// Container exposed port
    /// </summary>
    public int? ExposePort { get; set; } = 80;

    /// <summary>
    /// Container network mode
    /// </summary>
    public NetworkMode? NetworkMode { get; set; }

    /// <summary>
    /// The deadline of the challenge, null means no deadline
    /// </summary>
    public DateTimeOffset? DeadlineUtc { get; set; }

    /// <summary>
    /// Maximum number of submissions allowed per user (0 = no limit)
    /// </summary>
    [Required]
    public int SubmissionLimit { get; set; }

    /// <summary>
    /// Games that reference this pool challenge
    /// </summary>
    public List<ReferencedGameInfo> ReferencedGames { get; set; } = [];

    internal static PoolChallengeEditDetailModel FromChallenge(PoolChallenge chal) =>
        new()
        {
            Id = chal.Id,
            Title = chal.Title,
            Content = chal.Content,
            Category = chal.Category,
            Type = chal.Type,
            FlagTemplate = chal.FlagTemplate,
            Hints = chal.Hints ?? [],
            IsEnabled = chal.IsEnabled,
            Difficulty = chal.Difficulty,
            Tags = chal.Tags,
            Note = chal.Note,
            RangeEnabled = chal.RangeEnabled,
            RangeScore = chal.RangeScore,
            ContainerImage = chal.ContainerImage,
            MemoryLimit = chal.MemoryLimit,
            CPUCount = chal.CPUCount,
            StorageLimit = chal.StorageLimit,
            ExposePort = chal.ExposePort,
            NetworkMode = chal.NetworkMode,
            FileName = chal.FileName,
            Attachment = chal.Attachment,
            SubmissionLimit = chal.SubmissionLimit,
            DeadlineUtc = chal.DeadlineUtc,
            AcceptedCount = 0, // This field should be set externally
            TestContainer = chal.TestContainer is null ? null : ContainerInfoModel.FromContainer(chal.TestContainer),
            Flags = chal.Flags.Select(FlagInfoModel.FromFlagContext).ToList(),
            ReferencedGames = chal.ReferencingGames
                .Select(gc => new ReferencedGameInfo(gc.GameId, gc.Game.Title, gc.Id))
                .ToList()
        };
}
