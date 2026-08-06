using GZCTF.Models.Request.Edit;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class PoolChallengeRepository(AppDbContext context, IBlobRepository blobRepository)
    : RepositoryBase(context),
        IPoolChallengeRepository
{
    public async Task<PoolChallenge> CreatePoolChallenge(PoolChallenge challenge, CancellationToken token = default)
    {
        await Context.AddAsync(challenge, token);
        await SaveAsync(token);
        return challenge;
    }

    public Task<PoolChallenge?> GetPoolChallenge(int id, CancellationToken token = default) =>
        Context.PoolChallenges.FirstOrDefaultAsync(c => c.Id == id, token);

    public Task<PoolChallenge[]> GetPoolChallenges(CancellationToken token = default) =>
        Context.PoolChallenges.OrderBy(c => c.Id).ToArrayAsync(token);

    public Task<PoolChallenge[]> GetRangeChallenges(CancellationToken token = default) =>
        Context.PoolChallenges.Where(c => c.RangeEnabled && c.IsEnabled)
            .OrderBy(c => c.Category).ThenBy(c => c.Title)
            .ToArrayAsync(token);

    public async Task UpdatePoolChallenge(PoolChallenge challenge, PoolChallengeUpdateModel model,
        CancellationToken token = default)
    {
        challenge.Title = model.Title ?? challenge.Title;
        challenge.Content = model.Content ?? challenge.Content;
        challenge.Category = model.Category ?? challenge.Category;
        challenge.Hints = model.Hints ?? challenge.Hints;
        challenge.CPUCount = model.CPUCount ?? challenge.CPUCount;
        challenge.MemoryLimit = model.MemoryLimit ?? challenge.MemoryLimit;
        challenge.StorageLimit = model.StorageLimit ?? challenge.StorageLimit;
        challenge.ContainerImage = model.ContainerImage?.Trim() ?? challenge.ContainerImage;
        challenge.ExposePort = model.ExposePort ?? challenge.ExposePort;
        challenge.NetworkMode = model.NetworkMode ?? challenge.NetworkMode;
        challenge.FileName = model.FileName ?? challenge.FileName;
        challenge.SubmissionLimit = model.SubmissionLimit ?? challenge.SubmissionLimit;
        challenge.Difficulty = model.Difficulty ?? challenge.Difficulty;
        challenge.Tags = model.Tags ?? challenge.Tags;
        challenge.Note = model.Note ?? challenge.Note;
        challenge.RangeEnabled = model.RangeEnabled ?? challenge.RangeEnabled;
        challenge.RangeScore = model.RangeScore ?? challenge.RangeScore;

        // isEnabled should be updated alone
        challenge.IsEnabled = model.IsEnabled ?? challenge.IsEnabled;

        // only set DeadlineUtc to null when pass DateTimeOffset.MinValue (but not null)
        if (model.DeadlineUtc is { } time)
            challenge.DeadlineUtc = time.ToUnixTimeSeconds() == 0 ? null : time;

        // only set FlagTemplate to null when pass an empty string (but not null)
        if (model.FlagTemplate is { } template)
            challenge.FlagTemplate = string.IsNullOrWhiteSpace(template) ? null : template;

        await SaveAsync(token);
    }

    public async Task RemovePoolChallenge(PoolChallenge challenge, CancellationToken token = default)
    {
        await blobRepository.DeleteAttachment(challenge.Attachment, token);

        await LoadFlags(challenge, token);

        // only dynamic attachment challenge's flag contexts have attachment
        if (challenge.Type == ChallengeType.DynamicAttachment)
            foreach (var flag in challenge.Flags)
                await blobRepository.DeleteAttachment(flag.Attachment, token);

        Context.RemoveRange(challenge.Flags);
        Context.Remove(challenge);
        await SaveAsync(token);
    }

    public Task LoadFlags(PoolChallenge challenge, CancellationToken token = default) =>
        Context.Entry(challenge).Collection(c => c.Flags).LoadAsync(token);

    public async Task AddFlags(PoolChallenge challenge, FlagCreateModel[] models, CancellationToken token = default)
    {
        foreach (var model in models)
        {
            var attachment = model.ToAttachment(await blobRepository.GetBlobByHash(model.FileHash, token));

            challenge.Flags.Add(new() { Flag = model.Flag, PoolChallenge = challenge, Attachment = attachment });
        }

        await SaveAsync(token);
    }

    public async Task<TaskStatus> RemoveFlag(PoolChallenge challenge, int flagId, CancellationToken token = default)
    {
        var flag = await Context.FlagContexts
            .FirstOrDefaultAsync(f => f.PoolChallenge == challenge && f.Id == flagId, token);

        if (flag is null)
            return TaskStatus.NotFound;

        await blobRepository.DeleteAttachment(flag.Attachment, token);

        Context.Remove(flag);

        await SaveAsync(token);

        // If there are no more flags, disable the challenge
        if (!await Context.FlagContexts.AnyAsync(f => f.PoolChallenge == challenge, token))
        {
            challenge.IsEnabled = false;
            await SaveAsync(token);
        }

        return TaskStatus.Success;
    }

    public async Task UpdateAttachment(PoolChallenge challenge, AttachmentCreateModel model,
        CancellationToken token = default)
    {
        var attachment = model.ToAttachment(await blobRepository.GetBlobByHash(model.FileHash, token));

        await blobRepository.DeleteAttachment(challenge.Attachment, token);

        if (attachment is not null)
            await Context.AddAsync(attachment, token);

        challenge.Attachment = attachment;

        await SaveAsync(token);
    }

    public async Task<ReferencedGameInfo[]> GetReferencedGames(int poolId, CancellationToken token = default) =>
        await Context.GameChallenges
            .Where(c => c.PoolChallengeId == poolId)
            .Select(c => new ReferencedGameInfo(c.GameId, c.Game.Title, c.Id))
            .ToArrayAsync(token);
}
