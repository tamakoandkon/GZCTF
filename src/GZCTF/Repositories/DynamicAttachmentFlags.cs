using GZCTF.Models.Request.Edit;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

internal static class DynamicAttachmentFlags
{
    // All allocation and insertion for a shared pool must take the same row lock
    // before reading instance or Flag state. The transaction holds it until commit.
    public static Task LockAsync(AppDbContext context, int challengeId, bool isPool, CancellationToken token) =>
        isPool
            ? context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"PoolChallenges\" WHERE \"Id\" = {challengeId} FOR NO KEY UPDATE", token)
            : context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"GameChallenges\" WHERE \"Id\" = {challengeId} FOR NO KEY UPDATE", token);

    private static IQueryable<FlagContext> GetFlags(AppDbContext context, int challengeId, bool isPool) =>
        isPool
            ? context.FlagContexts.Where(flag => flag.PoolChallengeId == challengeId)
            : context.FlagContexts.Where(flag => flag.ChallengeId == challengeId);

    public static async Task<FlagContext?> ClaimAsync(
        AppDbContext context, int challengeId, bool isPool, CancellationToken token)
    {
        var flags = GetFlags(context, challengeId, isPool);

        // Older pools can contain duplicate text. Keep existing assignments and
        // never select another copy of text that has already been issued.
        var flag = await flags
            .Where(candidate => !candidate.IsOccupied &&
                !flags.Any(issued => issued.IsOccupied && issued.Flag == candidate.Flag))
            .OrderBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(token);

        if (flag is not null)
            flag.IsOccupied = true;

        return flag;
    }

    public static async Task RequireUniqueAsync(
        AppDbContext context, int challengeId, bool isPool, FlagCreateModel[] models, CancellationToken token)
    {
        var answers = models.Select(model => model.Flag).Distinct(StringComparer.Ordinal).ToArray();
        if (answers.Length != models.Length ||
            await GetFlags(context, challengeId, isPool).AnyAsync(flag => answers.Contains(flag.Flag), token))
            throw new DuplicateDynamicAttachmentFlagException();
    }
}

internal sealed class DuplicateDynamicAttachmentFlagException() :
    InvalidOperationException("Dynamic attachment Flags must be unique within the challenge or pool.");
