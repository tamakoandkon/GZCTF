using GZCTF.Models.Request.Admin;

namespace GZCTF.Models.Request.Exercise;

/// <summary>
/// Exercise cheat information (Monitor)
/// </summary>
public class ExerciseCheatInfoModel
{
    /// <summary>
    /// User who owns the flag
    /// </summary>
    public UserInfoModel OwnedUser { get; set; } = null!;

    /// <summary>
    /// User who submitted the shared flag
    /// </summary>
    public UserInfoModel SubmitUser { get; set; } = null!;

    /// <summary>
    /// The corresponding submission (serialized like the submission feed)
    /// </summary>
    public ExerciseSubmission Submission { get; set; } = null!;

    internal static ExerciseCheatInfoModel FromCheatInfo(ExerciseCheatInfo info) => new()
    {
        Submission = info.Submission,
        OwnedUser = UserInfoModel.FromUserInfo(info.SourceUser),
        SubmitUser = UserInfoModel.FromUserInfo(info.SubmitUser)
    };
}
