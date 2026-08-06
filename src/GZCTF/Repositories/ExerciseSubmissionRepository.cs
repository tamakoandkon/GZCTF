using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseSubmissionRepository(AppDbContext context)
    : RepositoryBase(context),
        IExerciseSubmissionRepository
{
    public async Task<ExerciseSubmission> AddSubmission(ExerciseSubmission submission,
        CancellationToken token = default)
    {
        await Context.ExerciseSubmissions.AddAsync(submission, token);
        await SaveAsync(token);
        return submission;
    }

    public Task<int> CountSubmissions(Guid userId, int exerciseId, CancellationToken token = default) =>
        Context.ExerciseSubmissions.CountAsync(
            s => s.UserId == userId && s.ExerciseId == exerciseId, token);

    public async Task<Dictionary<int, int>> GetSubmissionCounts(CancellationToken token = default) =>
        await Context.ExerciseSubmissions.AsNoTracking()
            .GroupBy(s => s.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Count, token);
}
