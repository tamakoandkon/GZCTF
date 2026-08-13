namespace GZCTF.Repositories.Interface;

public interface IExerciseSubmissionRepository : IRepository
{
    /// <summary>
    /// Add a range submission
    /// </summary>
    public Task<ExerciseSubmission> AddSubmission(ExerciseSubmission submission, CancellationToken token = default);

    /// <summary>
    /// Count submissions of a user for a range challenge
    /// </summary>
    public Task<int> CountSubmissions(Guid userId, int exerciseId, CancellationToken token = default);

    /// <summary>
    /// Total submission count per challenge (challenge ID → count)
    /// </summary>
    public Task<Dictionary<int, int>> GetSubmissionCounts(CancellationToken token = default);

    /// <summary>
    /// Get range submissions (for the monitor), newest first, with optional status filter
    /// </summary>
    public Task<ExerciseSubmission[]> GetSubmissions(AnswerResult? type = null, int count = 100, int skip = 0,
        CancellationToken token = default);
}
