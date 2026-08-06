using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// Add a pool challenge to a game as a linked game challenge (Edit)
/// </summary>
public class GameChallengeFromPoolModel
{
    /// <summary>
    /// Pool challenge ID
    /// </summary>
    [Required]
    public int PoolChallengeId { get; set; }

    /// <summary>
    /// Initial score
    /// </summary>
    public int? OriginalScore { get; set; }

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Range(0, 1)]
    public double? MinScoreRate { get; set; }

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    public double? Difficulty { get; set; }

    /// <summary>
    /// Is blood bonus disabled (enable by default)
    /// </summary>
    public bool? DisableBloodBonus { get; set; }

    /// <summary>
    /// Is traffic capture enabled (disabled by default)
    /// </summary>
    public bool? EnableTrafficCapture { get; set; }
}
