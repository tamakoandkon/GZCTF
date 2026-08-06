using GZCTF.Models.Request.Edit;

namespace GZCTF.Repositories.Interface;

public interface IPoolChallengeRepository : IRepository
{
    /// <summary>
    /// Create a pool challenge
    /// </summary>
    public Task<PoolChallenge> CreatePoolChallenge(PoolChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// Get a single pool challenge
    /// </summary>
    public Task<PoolChallenge?> GetPoolChallenge(int id, CancellationToken token = default);

    /// <summary>
    /// Get all pool challenges
    /// </summary>
    public Task<PoolChallenge[]> GetPoolChallenges(CancellationToken token = default);

    /// <summary>
    /// Get all pool challenges enabled in the training range
    /// </summary>
    public Task<PoolChallenge[]> GetRangeChallenges(CancellationToken token = default);

    /// <summary>
    /// Update a pool challenge from the update model
    /// </summary>
    public Task UpdatePoolChallenge(PoolChallenge challenge, PoolChallengeUpdateModel model,
        CancellationToken token = default);

    /// <summary>
    /// Remove a pool challenge
    /// </summary>
    public Task RemovePoolChallenge(PoolChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// Load flags of a pool challenge
    /// </summary>
    public Task LoadFlags(PoolChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// Add flags to a pool challenge
    /// </summary>
    public Task AddFlags(PoolChallenge challenge, FlagCreateModel[] models, CancellationToken token = default);

    /// <summary>
    /// Remove a flag from a pool challenge
    /// </summary>
    public Task<TaskStatus> RemoveFlag(PoolChallenge challenge, int flagId, CancellationToken token = default);

    /// <summary>
    /// Update the attachment of a pool challenge
    /// </summary>
    public Task UpdateAttachment(PoolChallenge challenge, AttachmentCreateModel model,
        CancellationToken token = default);

    /// <summary>
    /// Get games that reference the pool challenge
    /// </summary>
    public Task<ReferencedGameInfo[]> GetReferencedGames(int poolId, CancellationToken token = default);

    /// <summary>
    /// Number of games referencing each pool challenge (pool ID → count)
    /// </summary>
    public Task<Dictionary<int, int>> GetReferencedGameCounts(CancellationToken token = default);
}
