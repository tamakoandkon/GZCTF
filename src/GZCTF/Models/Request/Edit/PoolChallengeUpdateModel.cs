using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Pool challenge update information (Edit)
/// </summary>
public class PoolChallengeUpdateModel
{
    /// <summary>
    /// Challenge title
    /// </summary>
    [MinLength(1, ErrorMessageResourceName = nameof(Resources.Program.Model_TitleTooShort),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? Title { get; set; }

    /// <summary>
    /// Challenge content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Flag template, used to generate dynamic flags
    /// </summary>
    [MaxLength(Limits.MaxFlagTemplateLength, ErrorMessageResourceName = nameof(Resources.Program.Model_FlagTooLong),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public string? FlagTemplate { get; set; }

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory? Category { get; set; }

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Is the challenge enabled (globally, in the pool)
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Unified file name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// The deadline of the challenge, null means no deadline
    /// </summary>
    public DateTimeOffset? DeadlineUtc { get; set; }

    /// <summary>
    /// Maximum number of flag submissions allowed per user (0 = no limit)
    /// </summary>
    [Range(0, 10000, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? SubmissionLimit { get; set; }

    /// <summary>
    /// Container image name and tag
    /// </summary>
    public string? ContainerImage { get; set; }

    /// <summary>
    /// Memory limit (MB)
    /// </summary>
    [Range(32, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? MemoryLimit { get; set; }

    /// <summary>
    /// CPU limit (0.1 CPUs)
    /// </summary>
    [Range(1, 1024, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? CPUCount { get; set; }

    /// <summary>
    /// Storage limit (MB)
    /// </summary>
    [Range(0, 1048576, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? StorageLimit { get; set; }

    /// <summary>
    /// Container exposed port
    /// </summary>
    public int? ExposePort { get; set; }

    /// <summary>
    /// Container network mode
    /// </summary>
    public NetworkMode? NetworkMode { get; set; }

    /// <summary>
    /// Difficulty of the challenge
    /// </summary>
    public Difficulty? Difficulty { get; set; }

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
    public bool? RangeEnabled { get; set; }

    /// <summary>
    /// Fixed score of this challenge in the training range
    /// </summary>
    [Range(0, 100000, ErrorMessageResourceName = nameof(Resources.Program.Model_OutOfRange),
        ErrorMessageResourceType = typeof(Resources.Program))]
    public int? RangeScore { get; set; }

    /// <summary>
    /// Check if the Flag template is valid
    /// </summary>
    /// <returns></returns>
    internal bool IsValidFlagTemplate() =>
        !string.IsNullOrWhiteSpace(FlagTemplate) && new DynamicFlagGenerator(FlagTemplate).IsValid();
}
