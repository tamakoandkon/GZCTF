using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseCheatInfoRepository(AppDbContext context)
    : RepositoryBase(context),
        IExerciseCheatInfoRepository
{
    public async Task<ExerciseCheatInfo?> CheckCheat(ExerciseSubmission submission,
        CancellationToken token = default)
    {
        // Only dynamic flag instances carry a per-user FlagContext; static flags match
        // globally and never reach this check (mirrors the game-side behavior).
        var source = await Context.ExerciseInstances.AsNoTracking()
            .Include(i => i.User)
            .Where(i => i.ExerciseId == submission.ExerciseId
                && i.UserId != submission.UserId
                && i.FlagContext != null
                && i.FlagContext.Flag == submission.Answer)
            .FirstOrDefaultAsync(token);

        if (source is null)
            return null;

        var cheatInfo = new ExerciseCheatInfo
        {
            ExerciseId = submission.ExerciseId,
            SubmitUserId = submission.UserId,
            SourceUserId = source.UserId,
            SourceUser = source.User,
            ExerciseSubmissionId = submission.Id
        };

        await Context.ExerciseCheatInfo.AddAsync(cheatInfo, token);

        // submission is a tracked entity passed in from the controller
        submission.Status = AnswerResult.CheatDetected;
        await SaveAsync(token);

        return cheatInfo;
    }

    public Task<ExerciseCheatInfo[]> GetCheatInfos(CancellationToken token = default) =>
        Context.ExerciseCheatInfo.IgnoreAutoIncludes()
            .Include(i => i.SubmitUser)
            .Include(i => i.SourceUser)
            .Include(i => i.Exercise)
            .Include(i => i.Submission).ThenInclude(s => s.Exercise)
            .AsSplitQuery()
            .OrderByDescending(i => i.Submission.SubmitTimeUtc)
            .ToArrayAsync(token);
}
