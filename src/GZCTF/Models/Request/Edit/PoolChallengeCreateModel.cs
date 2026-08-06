using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Pool challenge creation information (Edit)
/// </summary>
public class PoolChallengeCreateModel
{
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
}
