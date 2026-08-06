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
}
