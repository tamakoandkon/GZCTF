using GZCTF.Models.Request.Exercise;

namespace GZCTF.Repositories.Interface;

public interface IExerciseInstanceRepository : IRepository
{
    /// <summary>
    /// 获取用户靶场题目实例，不存在时按需创建（动态 flag 分发）
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="exerciseId">题目Id</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ExerciseInstance?> GetInstance(UserInfo user, int exerciseId, CancellationToken token = default);

    /// <summary>
    /// 验证答案
    /// </summary>
    /// <param name="user">当前用户</param>
    /// <param name="instance">当前实例</param>
    /// <param name="answer">当前提交</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<AnswerResult> VerifyAnswer(UserInfo user, ExerciseInstance instance, string answer,
        CancellationToken token = default);

    /// <summary>
    /// 创建容器实例
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <param name="user">用户对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<TaskResult<Container>> CreateContainer(ExerciseInstance instance, UserInfo user,
        CancellationToken token = default);

    /// <summary>
    /// 销毁某个题库题的全部靶场容器
    /// </summary>
    public Task DestroyAllContainers(PoolChallenge challenge, CancellationToken token = default);

    /// <summary>
    /// 获取用户已解的题目 ID 集合
    /// </summary>
    public Task<HashSet<int>> GetSolvedIds(Guid userId, CancellationToken token = default);

    /// <summary>
    /// 每道题的已解人数（题目 ID → 人数）
    /// </summary>
    public Task<Dictionary<int, int>> GetAcceptedCounts(CancellationToken token = default);

    /// <summary>
    /// 全局个人排行榜（实时派生）
    /// </summary>
    public Task<ExerciseScoreboardModel> GetScoreboard(CancellationToken token = default);
}
