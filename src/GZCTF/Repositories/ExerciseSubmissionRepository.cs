using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseSubmissionRepository(
    AppDbContext context,
    IHubContext<MonitorHub, IMonitorClient> hub) : RepositoryBase(context),
    IExerciseSubmissionRepository
{
    public async Task<ExerciseSubmission> AddSubmission(ExerciseSubmission submission,
        CancellationToken token = default)
    {
        await Context.ExerciseSubmissions.AddAsync(submission, token);
        await SaveAsync(token);

        // re-query with navigations, then push to the range monitor group
        submission = await Context.ExerciseSubmissions
            .Include(s => s.User)
            .Include(s => s.Exercise)
            .SingleAsync(s => s.Id == submission.Id, token);

        await hub.Clients.Group("Exercise").ReceivedExerciseSubmission(submission);

        return submission;
    }

    public Task<ExerciseSubmission[]> GetSubmissions(AnswerResult? type = null, int count = 100, int skip = 0,
        CancellationToken token = default)
    {
        IQueryable<ExerciseSubmission> subs = Context.ExerciseSubmissions.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Exercise);

        if (type is not null)
            subs = subs.Where(s => s.Status == type.Value);

        return subs.OrderByDescending(s => s.SubmitTimeUtc).Skip(skip).Take(count).ToArrayAsync(token);
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
