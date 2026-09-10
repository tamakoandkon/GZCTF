using GZCTF.Models.Internal;
using GZCTF.Models.Request.Exercise;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Manager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class ExerciseInstanceRepository(
    AppDbContext context,
    IContainerManager service,
    IContainerRepository containerRepository,
    IExerciseEventRepository exerciseEventRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    ILogger<ExerciseInstanceRepository> logger,
    IStringLocalizer<Program> localizer
) : RepositoryBase(context),
    IExerciseInstanceRepository
{
    public async Task<ExerciseInstance?> GetInstance(UserInfo user, int exerciseId,
        CancellationToken token = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        var instance = await Context.ExerciseInstances
            .Include(i => i.FlagContext)
            .Where(e => e.ExerciseId == exerciseId && e.UserId == user.Id)
            .SingleOrDefaultAsync(token);

        if (instance is not null && instance.IsLoaded)
        {
            await transaction.CommitAsync(token);
            return instance;
        }

        // the challenge must be enabled in the range for the user to access it
        var challenge = await Context.PoolChallenges
            .FirstOrDefaultAsync(c => c.Id == exerciseId, token);

        if (challenge is null || !challenge.IsEnabled || !challenge.RangeEnabled)
        {
            await transaction.CommitAsync(token);
            return null;
        }

        var isNewInstance = instance is null;
        instance ??= new ExerciseInstance { ExerciseId = exerciseId, UserId = user.Id, IsLoaded = false };

        // a brand-new instance must be tracked explicitly, otherwise SaveAsync does not
        // insert it (navigation fixup does not reliably mark it as Added here)
        if (isNewInstance)
            Context.ExerciseInstances.Add(instance);

        // newly created instances have no loaded Exercise navigation; bind the loaded
        // challenge so callers (detail model / verify / container ops) can read it
        instance.Exercise = challenge;

        try
        {
            // dynamic flag dispatch
            if (challenge.Type == ChallengeType.DynamicContainer)
            {
                instance.FlagContext = new()
                {
                    PoolChallenge = challenge,
                    Flag = challenge.GenerateDynamicFlagForUser(user.Id),
                    IsOccupied = true
                };
            }
            else if (challenge.Type == ChallengeType.DynamicAttachment)
            {
                // Atomically claim one free flag (conditional UPDATE + retry). The old
                // list-then-mark flow let concurrent dispatchers bind the same flag row,
                // handing an identical dynamic flag to two users/teams.
                var claimedFlagId = await ClaimFreeExerciseFlagAsync(exerciseId, token);

                if (claimedFlagId is null)
                {
                    logger.SystemLog(
                        localizer[nameof(Resources.Program.InstanceRepository_DynamicFlagsNotEnough),
                            challenge.Title, challenge.Id],
                        TaskStatus.Failed, LogLevel.Warning);
                    await transaction.RollbackAsync(token);
                    return null;
                }

                instance.FlagId = claimedFlagId.Value;
            }

            // instance.FlagContext is null by default
            // static flag does not need to be dispatched

            instance.IsLoaded = true;
            await SaveAsync(token);
            await transaction.CommitAsync(token);
        }
        catch
        {
            logger.SystemLog(
                localizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), user.UserName!,
                    challenge.Title, challenge.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    /// <summary>
    /// Atomically claim one unoccupied dynamic-attachment flag of the pool challenge.
    /// Conditional UPDATE makes concurrent dispatchers retry instead of double-issuing.
    /// </summary>
    private async Task<int?> ClaimFreeExerciseFlagAsync(int poolChallengeId,
        CancellationToken token = default)
    {
        const int maxClaimAttempts = 5;

        for (var attempt = 0; attempt < maxClaimAttempts; attempt++)
        {
            var candidateId = await Context.FlagContexts.AsNoTracking()
                .Where(e => e.PoolChallengeId == poolChallengeId && !e.IsOccupied)
                .OrderBy(e => e.Id).Select(e => (int?)e.Id).FirstOrDefaultAsync(token);

            if (candidateId is null)
                return null;

            var affected = await Context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"FlagContexts\" SET \"IsOccupied\" = true WHERE \"Id\" = {candidateId.Value} AND NOT \"IsOccupied\"",
                token);

            if (affected > 0)
                return candidateId.Value;
        }

        return null;
    }

    public async Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user,
        CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(instance.Exercise.ContainerImage) || instance.Exercise.ExposePort is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    instance.Exercise.Title],
                TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        var containerLimit = containerPolicy.Value.MaxExerciseContainerCountPerUser;
        if (containerLimit > 0)
        {
            var running = await Context.ExerciseInstances
                .Where(i => i.User == user && i.Container != null)
                .OrderBy(i => i.Container!.StartedAt)
                .ToListAsync(token);

            var first = running.FirstOrDefault();
            if (running.Count >= containerLimit && first is not null)
            {
                logger.Log(
                    StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerAutoDestroy),
                        user.UserName!, first.Exercise.Title,
                        first.Container!.LogId],
                    user, TaskStatus.Success);
                await containerRepository.DestroyContainer(running.First().Container!, token);
            }
        }

        if (instance.Container is not null)
            return new TaskResult<Container>(TaskStatus.Success, instance.Container);

        await Context.Entry(instance).Reference(e => e.FlagContext).LoadAsync(token);

        var container = await service.CreateContainerAsync(new ContainerConfig
        {
            TeamId = "exercise",
            UserId = user.Id,
            ChallengeId = instance.ExerciseId,
            Flag = instance.FlagContext?.Flag, // static challenge has no specific flag
            Image = instance.Exercise.ContainerImage,
            CPUCount = instance.Exercise.CPUCount ?? 1,
            MemoryLimit = instance.Exercise.MemoryLimit ?? 64,
            StorageLimit = instance.Exercise.StorageLimit ?? 256,
            NetworkMode = instance.Exercise.NetworkMode ?? NetworkMode.Open,
            EnableTrafficCapture = false,
            ExposedPort = instance.Exercise.ExposePort.Value
        }, token);

        if (container is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    instance.Exercise.Title],
                TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        instance.Container = container;
        instance.LastContainerOperation = DateTimeOffset.UtcNow;

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreated), user.UserName!,
                instance.Exercise.Title,
                container.LogId], user,
            TaskStatus.Success);

        await SaveAsync(token);

        await exerciseEventRepository.AddEvent(new()
        {
            Type = EventType.ContainerStart,
            UserId = user.Id,
            ExerciseId = instance.ExerciseId,
            Values = [instance.ExerciseId.ToString(), instance.Exercise.Title]
        }, token);

        return new TaskResult<Container>(TaskStatus.Success, instance.Container);
    }

    public async Task<AnswerResult> VerifyAnswer(UserInfo user, ExerciseInstance instance, string answer,
        CancellationToken token = default)
    {
        if (instance.Exercise.Type == ChallengeType.DynamicContainer)
        {
            if (instance.FlagContext is null)
                return AnswerResult.NotFound;

            if (instance.FlagContext.Flag != answer)
                return AnswerResult.WrongAnswer;

            await MarkSolved(instance, token);
            return AnswerResult.Accepted;
        }

        if (await Context.FlagContexts.AsNoTracking()
                .AnyAsync(f => f.PoolChallengeId == instance.ExerciseId && f.Flag == answer, token))
        {
            await MarkSolved(instance, token);
            return AnswerResult.Accepted;
        }

        return AnswerResult.WrongAnswer;
    }

    internal async Task MarkSolved(ExerciseInstance instance, CancellationToken token = default)
    {
        if (instance.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0))
            return;

        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        // Atomically claim the solve with a conditional UPDATE (composite PK is
        // UserId + ExerciseId; SolveTimeUtc defaults to the Unix epoch). Two concurrent
        // correct submissions both passed the in-memory guards before; now only the
        // first one observes a row affected, so duplicate Accepted / double scoring
        // cannot happen.
        var now = DateTimeOffset.UtcNow;
        var epoch = DateTimeOffset.FromUnixTimeSeconds(0);
        var affected = await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ExerciseInstances\" SET \"SolveTimeUtc\" = {now} WHERE \"UserId\" = {instance.UserId} AND \"ExerciseId\" = {instance.ExerciseId} AND \"SolveTimeUtc\" <= {epoch}",
            token);

        if (affected > 0)
            instance.SolveTimeUtc = now;

        await transaction.CommitAsync(token);
    }

    public async Task DestroyAllContainers(PoolChallenge challenge, CancellationToken token = default)
    {
        foreach (var container in await Context.ExerciseInstances
                     .Include(i => i.Container)
                     .Where(i => i.Exercise == challenge && i.ContainerId != null)
                     .Select(i => i.Container)
                     .ToArrayAsync(token))
        {
            if (container is null)
                continue;

            await containerRepository.DestroyContainer(container, token);
        }
    }

    public async Task<HashSet<int>> GetSolvedIds(Guid userId, CancellationToken token = default) =>
        await Context.ExerciseInstances.AsNoTracking()
            .Where(i => i.UserId == userId && i.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0))
            .Select(i => i.ExerciseId)
            .ToHashSetAsync(token);

    public async Task<Dictionary<int, int>> GetAcceptedCounts(CancellationToken token = default) =>
        await Context.ExerciseInstances.AsNoTracking()
            .Where(i => i.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0))
            .GroupBy(i => i.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Count, token);

    public async Task<ExerciseScoreboardModel> GetScoreboard(CancellationToken token = default)
    {
        var solves = await (from inst in Context.ExerciseInstances.AsNoTracking()
                join chal in Context.PoolChallenges.AsNoTracking()
                    on inst.ExerciseId equals chal.Id
                join user in Context.Users.AsNoTracking()
                    on inst.UserId equals user.Id
                where inst.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0) &&
                      chal.RangeEnabled && chal.IsEnabled
                select new
                {
                    inst.UserId,
                    user.UserName,
                    user.AvatarHash,
                    chal.RangeScore,
                    inst.SolveTimeUtc
                })
            .ToListAsync(token);

        var items = solves
            .GroupBy(s => s.UserId)
            .Select(g => new ExerciseScoreboardItem
            {
                UserId = g.Key,
                UserName = g.First().UserName,
                Avatar = g.First().AvatarHash is null
                    ? null
                    : $"/assets/{g.First().AvatarHash}/avatar",
                Score = g.Sum(s => s.RangeScore),
                SolvedCount = g.Count(),
                LastSolveTime = g.Max(s => s.SolveTimeUtc)
            })
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.LastSolveTime)
            .ToList();

        for (var i = 0; i < items.Count; i++)
            items[i].Rank = i + 1;

        var challengeSolvedCount = await Context.ExerciseInstances.AsNoTracking()
            .Where(i => i.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0))
            .GroupBy(i => i.ExerciseId)
            .Select(g => new { ExerciseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExerciseId, x => x.Count, token);

        return new ExerciseScoreboardModel
        {
            Items = items,
            ChallengeSolvedCount = challengeSolvedCount
        };
    }
}
