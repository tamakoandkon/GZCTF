using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Record of cheating behavior in the training range.
/// Exercise is per-user (no teams), so both sides are users instead of participations.
/// </summary>
[Index(nameof(ExerciseId))]
[Index(nameof(ExerciseSubmissionId), IsUnique = true)]
public class ExerciseCheatInfo
{
    #region Db Relationship

    /// <summary>
    /// Pool challenge object
    /// </summary>
    public PoolChallenge Exercise { get; set; } = null!;

    /// <summary>
    /// Pool challenge object ID
    /// </summary>
    public int ExerciseId { get; set; }

    /// <summary>
    /// User who submitted the shared flag
    /// </summary>
    public UserInfo SubmitUser { get; set; } = null!;

    /// <summary>
    /// User ID who submitted the shared flag
    /// </summary>
    public Guid SubmitUserId { get; set; }

    /// <summary>
    /// User who owns the corresponding flag
    /// </summary>
    public UserInfo SourceUser { get; set; } = null!;

    /// <summary>
    /// User ID who owns the corresponding flag
    /// </summary>
    public Guid SourceUserId { get; set; }

    /// <summary>
    /// Submission corresponding to this cheating behavior
    /// </summary>
    public ExerciseSubmission Submission { get; set; } = null!;

    /// <summary>
    /// Submission ID corresponding to this cheating behavior
    /// </summary>
    public int ExerciseSubmissionId { get; set; }

    #endregion Db Relationship
}
