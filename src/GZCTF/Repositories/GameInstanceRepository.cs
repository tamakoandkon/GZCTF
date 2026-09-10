using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Container.Manager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Repositories;

public class GameInstanceRepository(
    AppDbContext context,
    IContainerManager service,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    ILogger<GameInstanceRepository> logger,
    IStringLocalizer<Program> localizer) : RepositoryBase(context), IGameInstanceRepository
{
    public async Task<GameInstance?> GetInstance(Participation part, int challengeId, CancellationToken token = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        var instance = await Context.GameInstances
            .Include(i => i.FlagContext)
            .Include(i => i.Challenge)
            .ThenInclude(c => c.PoolChallenge)
            .Where(e => e.ChallengeId == challengeId && e.Participation == part)
            .SingleOrDefaultAsync(token);

        if (instance is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_NoInstance), part.Id, challengeId],
                TaskStatus.NotFound,
                LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        var challenge = instance.Challenge;

        if (!challenge.IsEnabled)
        {
            await transaction.RollbackAsync(token);
            return null;
        }

        if (instance.IsLoaded)
        {
            await transaction.RollbackAsync(token);
            return instance;
        }

        try
        {
            var content = challenge.EffectiveContent;

            switch (content.Type)
            {
                // dynamic flag dispatch
                case ChallengeType.DynamicContainer:
                    instance.FlagContext = new()
                    {
                        Challenge = challenge,
                        Flag = content.GenerateDynamicFlag(part),
                        IsOccupied = true
                    };
                    break;
                case ChallengeType.DynamicAttachment:
                    // Atomically claim one free flag: read a candidate row, then occupy it
                    // with a conditional UPDATE. Concurrent dispatchers that pick the same
                    // row lose the UPDATE (0 rows affected) and retry on the next candidate,
                    // instead of both binding the same flag. The previous list-then-mark
                    // TOCTOU handed one flag to two teams under concurrency.
                    var claimedFlagId = await ClaimFreeFlagAsync(
                        challenge.IsLinked ? challenge.PoolChallengeId : null,
                        challenge.Id, token);

                    if (claimedFlagId is null)
                    {
                        logger.SystemLog(
                            StaticLocalizer[nameof(Resources.Program.InstanceRepository_DynamicFlagsNotEnough),
                                content.Title,
                                challenge.Id], TaskStatus.Failed,
                            LogLevel.Warning);
                        return null;
                    }

                    instance.FlagId = claimedFlagId.Value;
                    break;
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
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_GetInstanceFailed), part.Team.Name,
                    challenge.EffectiveContent.Title,
                    challenge.Id],
                TaskStatus.Failed, LogLevel.Warning);
            await transaction.RollbackAsync(token);
            return null;
        }

        return instance;
    }

    /// <summary>
    /// Atomically claim one unoccupied dynamic-attachment flag. Reads a candidate
    /// (linked challenges draw from the pool challenge's flag set) and occupies it
    /// with a conditional UPDATE; on a lost race it retries with the next candidate.
    /// Returns null when the pool has no free flag left after the attempts.
    /// </summary>
    private async Task<int?> ClaimFreeFlagAsync(int? poolChallengeId, int challengeId,
        CancellationToken token = default)
    {
        const int maxClaimAttempts = 5;

        for (var attempt = 0; attempt < maxClaimAttempts; attempt++)
        {
            int? candidateId = poolChallengeId is { } poolId
                ? await Context.FlagContexts.AsNoTracking()
                    .Where(e => e.PoolChallengeId == poolId && !e.IsOccupied)
                    .OrderBy(e => e.Id).Select(e => (int?)e.Id).FirstOrDefaultAsync(token)
                : await Context.FlagContexts.AsNoTracking()
                    .Where(e => e.ChallengeId == challengeId && !e.IsOccupied)
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

    public Task<GameInstance?> GetInstanceForSubmission(Participation team, int challengeId,
        CancellationToken token = default)
        => Context.GameInstances.IgnoreAutoIncludes()
            .Include(i => i.Challenge)
            .ThenInclude(c => c.PoolChallenge)
            .Where(i => i.ParticipationId == team.Id && i.ChallengeId == challengeId)
            .SingleOrDefaultAsync(token);

    public async Task<TaskResult<Container>> CreateContainer(GameInstance gameInstance, Team team, UserInfo user,
        Game game, CancellationToken token = default)
    {
        var content = gameInstance.Challenge.EffectiveContent;

        if (string.IsNullOrEmpty(content.ContainerImage) ||
            content.ExposePort is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    content.Title],
                TaskStatus.Denied, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // containerLimit == 0 means unlimited
        if (game.ContainerCountLimit > 0)
        {
            if (containerPolicy.Value.AutoDestroyOnLimitReached)
            {
                var running = await Context.GameInstances
                    .Where(i => i.Participation == gameInstance.Participation && i.Container != null)
                    .OrderBy(i => i.Container!.StartedAt).ToListAsync(token);

                var first = running.FirstOrDefault();
                if (running.Count >= game.ContainerCountLimit && first is not null)
                {
                    logger.Log(
                        StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerAutoDestroy),
                            team.Name, first.Challenge.Title,
                            first.Container!.LogId],
                        user, TaskStatus.Success);
                    await containerRepository.DestroyContainer(running.First().Container!, token);
                }
            }
            else
            {
                var count = await Context.GameInstances.CountAsync(
                    i => i.Participation == gameInstance.Participation &&
                         i.Container != null, token);

                if (count >= game.ContainerCountLimit)
                    return new TaskResult<Container>(TaskStatus.Denied);
            }
        }

        if (gameInstance.Container is not null)
            return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);

        await Context.Entry(gameInstance).Reference(e => e.FlagContext).LoadAsync(token);

        var challenge = gameInstance.Challenge;
        var container = await service.CreateContainerAsync(new ContainerConfig
        {
            TeamId = team.Id.ToString(),
            UserId = user.Id,
            ChallengeId = gameInstance.ChallengeId,
            GameId = challenge.GameId,
            Flag = gameInstance.FlagContext?.Flag, // static challenge has no specific flag
            Image = content.ContainerImage,
            CPUCount = content.CPUCount ?? 1,
            MemoryLimit = content.MemoryLimit ?? 64,
            StorageLimit = content.StorageLimit ?? 256,
            NetworkMode = content.NetworkMode ?? NetworkMode.Open,
            EnableTrafficCapture = challenge.EnableTrafficCapture && game.IsActive,
            ExposedPort = content.ExposePort ??
                          throw new ArgumentException(
                              localizer[nameof(Resources.Program.InstanceRepository_InvalidPort)])
        }, token);

        if (container is null)
        {
            logger.SystemLog(
                StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreationFailed),
                    content.Title],
                TaskStatus.Failed, LogLevel.Warning);
            return new TaskResult<Container>(TaskStatus.Failed);
        }

        // update the ExpectStopAt with config
        container.ExpectStopAt = container.StartedAt.AddMinutes(containerPolicy.Value.DefaultLifetime);

        gameInstance.Container = container;
        gameInstance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerStart,
                GameId = gameInstance.Challenge.GameId,
                TeamId = gameInstance.Participation.TeamId,
                UserId = user.Id,
                Values = [gameInstance.Challenge.Id.ToString(), content.Title]
            }, token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.InstanceRepository_ContainerCreated), team.Name,
                content.Title,
                container.LogId], user,
            TaskStatus.Success);

        return new TaskResult<Container>(TaskStatus.Success, gameInstance.Container);
    }

    public async Task DestroyAllContainers(GameChallenge challenge, CancellationToken token = default)
    {
        foreach (var container in await Context.GameInstances
                     .Include(i => i.Container)
                     .Where(i => i.Challenge == challenge && i.ContainerId != null)
                     .Select(i => i.Container)
                     .ToArrayAsync(token))
        {
            if (container is null)
                continue;

            await containerRepository.DestroyContainer(container, token);
        }
    }

    public async Task<CheatCheckInfo> CheckCheat(Submission submission, CancellationToken token = default)
    {
        CheatCheckInfo checkInfo = new();

        var instance = await Context.GameInstances
            .Include(i => i.Participation)
            .ThenInclude(i => i.Team)
            .Include(i => i.FlagContext)
            .Where(i => i.ChallengeId == submission.ChallengeId &&
                        i.ParticipationId != submission.ParticipationId &&
                        i.FlagContext != null && i.FlagContext.Flag == submission.Answer)
            .FirstOrDefaultAsync(token);

        if (instance is null)
            return checkInfo;

        var updateSub = await Context.Submissions.Where(s => s.Id == submission.Id).SingleAsync(token);

        var cheatInfo = await cheatInfoRepository.CreateCheatInfo(updateSub, instance, token);

        checkInfo = CheatCheckInfo.FromCheatInfo(cheatInfo);

        updateSub.Status = AnswerResult.CheatDetected;

        await SaveAsync(token);

        return checkInfo;
    }

    public async Task<VerifyResult> VerifyAnswer(Submission submission, CancellationToken token = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(token);

        try
        {
            var instance = await Context.GameInstances.IgnoreAutoIncludes()
                .Include(i => i.FlagContext)
                .SingleOrDefaultAsync(i => i.ChallengeId == submission.ChallengeId &&
                                           i.ParticipationId == submission.ParticipationId, token);

            if (instance is null)
            {
                submission.Status = AnswerResult.NotFound;
                await transaction.RollbackAsync(token);
                return new(SubmissionType.Unaccepted, AnswerResult.NotFound);
            }

            var updateSub = await Context.Submissions
                .IgnoreAutoIncludes()
                .SingleAsync(s => s.Id == submission.Id, token);

            var challenge = await Context.GameChallenges
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Include(c => c.PoolChallenge)
                .SingleAsync(c => c.Id == submission.ChallengeId, token);

            var content = challenge.EffectiveContent;

            if (instance.FlagContext is null && content.Type.IsStatic())
            {
                // linked challenges store static flags against the pool challenge
                updateSub.Status = await Context.FlagContexts.AsNoTracking()
                    .AnyAsync(
                        f => f.Flag == submission.Answer &&
                             (f.ChallengeId == submission.ChallengeId ||
                              (challenge.PoolChallengeId != null &&
                               f.PoolChallengeId == challenge.PoolChallengeId)),
                        token)
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;
            }
            else
            {
                updateSub.Status = instance.FlagContext?.Flag == submission.Answer
                    ? AnswerResult.Accepted
                    : AnswerResult.WrongAnswer;
            }

            if (updateSub.Status != AnswerResult.Accepted)
            {
                await SaveAsync(token);
                await transaction.CommitAsync(token);
                return new(SubmissionType.Unaccepted, updateSub.Status);
            }

            // Acquire a PostgresSQL advisory lock to prevent race conditions:
            // This lock ensures that only one concurrent submission for the same participation/challenge pair
            // can proceed past this point, preventing duplicate FirstSolve entries if multiple submissions
            // are processed at the same time. Without this, two submissions could both pass the check and
            // insert duplicate records.
            await Context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0}, {1})",
                [updateSub.ParticipationId, updateSub.ChallengeId],
                cancellationToken: token);

            var alreadySolved = await Context.FirstSolves
                .AnyAsync(fs => fs.ParticipationId == submission.ParticipationId &&
                                fs.ChallengeId == submission.ChallengeId, token);

            if (alreadySolved)
            {
                await SaveAsync(token);
                await transaction.CommitAsync(token);
                return new(SubmissionType.Normal, updateSub.Status);
            }

            var participation = await Context.Participations
                .IgnoreAutoIncludes().AsNoTracking()
                .Include(p => p.Division)
                .ThenInclude(d => d!.ChallengeConfigs)
                .SingleAsync(p => p.Id == submission.ParticipationId, token);

            var time = await Context.Games.IgnoreAutoIncludes().AsNoTracking()
                .Where(g => g.Id == participation.GameId)
                .Select(g => new { g.StartTimeUtc, g.EndTimeUtc })
                .SingleAsync(token);

            // Check if submission is within game time window
            var withinGameWindow = updateSub.SubmitTimeUtc >= time.StartTimeUtc &&
                                   updateSub.SubmitTimeUtc < time.EndTimeUtc;

            // Check if submission is within challenge deadline (if deadline is set)
            var withinDeadline = !content.DeadlineUtc.HasValue ||
                                 updateSub.SubmitTimeUtc <= content.DeadlineUtc.Value;

            // Blood bonus is only awarded if submission is within both game window and deadline
            var hasBloodPermission = withinGameWindow && withinDeadline && !challenge.DisableBloodBonus &&
                                     HasPermission(participation.Division, GamePermission.GetBlood,
                                         submission.ChallengeId);

            var submissionType = SubmissionType.Normal;
            if (hasBloodPermission)
            {
                var bloodEligibleCount = await CountBloodEligibleSolves(submission.ChallengeId, time.StartTimeUtc,
                    time.EndTimeUtc, token);

                submissionType = bloodEligibleCount switch
                {
                    0 => SubmissionType.FirstBlood,
                    1 => SubmissionType.SecondBlood,
                    2 => SubmissionType.ThirdBlood,
                    _ => SubmissionType.Normal
                };
            }

            Context.FirstSolves.Add(new FirstSolve
            {
                ParticipationId = submission.ParticipationId,
                ChallengeId = submission.ChallengeId,
                SubmissionId = submission.Id
            });

            await SaveAsync(token);
            await transaction.CommitAsync(token);

            return new(submissionType, updateSub.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during answer verification for submission {SubmissionId}.",
                submission.Id);
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    private Task<int> CountBloodEligibleSolves(int challengeId, DateTimeOffset start, DateTimeOffset end,
        CancellationToken token)
    {
        // First, get blood-eligible participation IDs for the challenge
        var eligibleParticipationIds =
            from participation in Context.Participations.AsNoTracking()
            join config in Context.Set<DivisionChallengeConfig>().AsNoTracking()
                    .Where(c => c.ChallengeId == challengeId)
                on participation.DivisionId equals config.DivisionId into configJoin
            from cfg in configJoin.DefaultIfEmpty()
            join division in Context.Divisions.AsNoTracking()
                on participation.DivisionId equals division.Id into divisionJoin
            from div in divisionJoin.DefaultIfEmpty()
            where participation.Status == ParticipationStatus.Accepted
                  && (cfg != null
                      ? cfg.Permissions.HasFlag(GamePermission.GetBlood)
                      : div == null || div.DefaultPermissions.HasFlag(GamePermission.GetBlood))
            select participation.Id;

        // Now, count FirstSolves for the challenge with eligible participations and time window
        return (
            from fs in Context.FirstSolves.AsNoTracking()
            join submission in Context.Submissions.AsNoTracking() on fs.SubmissionId equals submission.Id
            where fs.ChallengeId == challengeId
                  && eligibleParticipationIds.Contains(fs.ParticipationId)
                  && submission.SubmitTimeUtc >= start
                  && submission.SubmitTimeUtc < end
            orderby submission.SubmitTimeUtc
            select fs.ParticipationId
        ).Take(4).CountAsync(token);
    }

    private static bool HasPermission(Division? division, GamePermission permission, int challengeId)
    {
        if (division is null)
            return true;

        var specific = division.ChallengeConfigs.FirstOrDefault(c => c.ChallengeId == challengeId);
        var permissions = specific?.Permissions ?? division.DefaultPermissions;
        return permissions.HasFlag(permission);
    }
}
