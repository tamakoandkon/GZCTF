using GZCTF.Models.Internal;
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

        instance ??= new ExerciseInstance { ExerciseId = exerciseId, UserId = user.Id, IsLoaded = false };

        try
        {
            // dynamic flag dispatch
            if (challenge.Type == ChallengeType.DynamicContainer)
                instance.FlagContext = new()
                {
                    PoolChallenge = challenge,
                    Flag = challenge.GenerateDynamicFlagForUser(user.Id),
                    IsOccupied = true
                };

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

        instance.SolveTimeUtc = DateTimeOffset.UtcNow;
        await SaveAsync(token);

        await transaction.CommitAsync(token);
    }
}
