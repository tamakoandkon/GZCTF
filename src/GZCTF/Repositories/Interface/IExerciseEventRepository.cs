namespace GZCTF.Repositories.Interface;

public interface IExerciseEventRepository : IRepository
{
    /// <summary>
    /// Add an exercise (training range) event and push it to the monitor group
    /// </summary>
    public Task<ExerciseEvent> AddEvent(ExerciseEvent exerciseEvent, CancellationToken token = default);

    /// <summary>
    /// Get exercise events with optional filter and pagination
    /// </summary>
    public Task<ExerciseEvent[]> GetEvents(bool hideContainer = false, int count = 50, int skip = 0,
        CancellationToken token = default);
}
