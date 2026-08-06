using MemoryPack;

namespace GZCTF.Models.Request.Exercise;

/// <summary>
/// Exercise scoreboard
/// </summary>
[MemoryPackable]
public partial class ExerciseScoreboardModel
{
    /// <summary>
    /// Ranked items
    /// </summary>
    public List<ExerciseScoreboardItem> Items { get; set; } = [];

    /// <summary>
    /// Number of solvers per challenge (challenge ID → count)
    /// </summary>
    public Dictionary<int, int> ChallengeSolvedCount { get; set; } = [];
}

/// <summary>
/// A single scoreboard entry
/// </summary>
[MemoryPackable]
public partial class ExerciseScoreboardItem
{
    /// <summary>
    /// Rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// User avatar URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Total score in the range
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Number of solved challenges
    /// </summary>
    public int SolvedCount { get; set; }

    /// <summary>
    /// Time of the last solve
    /// </summary>
    public DateTimeOffset LastSolveTime { get; set; }
}

/// <summary>
/// Result of submitting a flag in the range
/// </summary>
public class ExerciseSubmitResult
{
    /// <summary>
    /// Answer result
    /// </summary>
    public AnswerResult Status { get; set; }

    /// <summary>
    /// Whether the challenge is now solved by the user
    /// </summary>
    public bool IsSolved { get; set; }

    /// <summary>
    /// Fixed score of the challenge
    /// </summary>
    public int Score { get; set; }
}
