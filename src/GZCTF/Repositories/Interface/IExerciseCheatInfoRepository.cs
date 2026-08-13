namespace GZCTF.Repositories.Interface;

public interface IExerciseCheatInfoRepository : IRepository
{
    /// <summary>
    /// Detect cheating: if another user's dynamic flag matches the submitted answer,
    /// create a cheat record and mark the submission as CheatDetected; otherwise null.
    /// </summary>
    public Task<ExerciseCheatInfo?> CheckCheat(ExerciseSubmission submission, CancellationToken token = default);

    /// <summary>
    /// Get all cheat records (newest first)
    /// </summary>
    public Task<ExerciseCheatInfo[]> GetCheatInfos(CancellationToken token = default);
}
