using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

/// <summary>
/// Receives observations from a challenge Pod's HTTP probe.
/// </summary>
[ApiController]
[Route("api/ai-detection")]
public class AiDetectionController(AppDbContext context) : ControllerBase
{
    public sealed class ReportModel
    {
        public string? ModelId { get; set; }
    }

    public sealed class MarkedFlagReportModel
    {
        public string? ModelId { get; set; }

        public string? Flag { get; set; }
    }

    [HttpPost("report")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> Report([FromHeader(Name = "X-Ai-Probe-Token")] Guid token,
        [FromBody] ReportModel report, CancellationToken cancellationToken)
    {
        var modelId = report.ModelId?.Trim();
        if (token == Guid.Empty || string.IsNullOrWhiteSpace(modelId) || modelId.Length > 128 ||
            modelId.Any(char.IsControl))
            return BadRequest();

        var container = await FindContainer(token, cancellationToken);
        var instance = container?.GameInstance;
        if (container is null || instance is null || !IsProbeEnabled(instance))
            return NotFound();

        // Keep one observation per identifier and cap the number of identifiers
        // a malicious client can cause the proxy to submit for an instance.
        if (await context.AiDetectionEvents.AnyAsync(e => e.ContainerId == container.Id &&
            e.ModelId == modelId, cancellationToken))
            return NoContent();

        if (await context.AiDetectionEvents.CountAsync(e => e.ContainerId == container.Id,
                cancellationToken) >= 20)
            return NoContent();

        context.AiDetectionEvents.Add(new AiDetectionEvent
        {
            GameId = instance.Challenge.GameId,
            ChallengeId = instance.ChallengeId,
            ParticipationId = instance.ParticipationId,
            TeamId = instance.Participation.TeamId,
            TeamName = instance.Participation.Team.Name,
            ChallengeTitle = instance.Challenge.Title,
            ContainerId = container.Id,
            ModelId = modelId
        });
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Records a decoy flag the probe handed to a request that reported an AI identifier.
    /// </summary>
    [HttpPost("marked-flag")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> ReportMarkedFlag([FromHeader(Name = "X-Ai-Probe-Token")] Guid token,
        [FromBody] MarkedFlagReportModel report, CancellationToken cancellationToken)
    {
        var modelId = report.ModelId?.Trim();
        var flag = report.Flag;

        if (token == Guid.Empty || string.IsNullOrWhiteSpace(modelId) || modelId.Length > 128 ||
            modelId.Any(char.IsControl))
            return BadRequest();

        if (string.IsNullOrWhiteSpace(flag) || flag.Length > Limits.MaxFlagLength ||
            flag.Any(char.IsControl))
            return BadRequest();

        var container = await FindContainer(token, cancellationToken);
        var instance = container?.GameInstance;
        if (container is null || instance is null || !IsProbeEnabled(instance))
            return NotFound();

        // Values are random, so an existing row means this exact issuance was already
        // recorded; re-reporting the same decoy must not create a second entry.
        if (await context.MarkedFlags.AnyAsync(f => f.Flag == flag, cancellationToken))
            return NoContent();

        if (await context.MarkedFlags.CountAsync(f => f.ContainerId == container.Id,
                cancellationToken) >= 20)
            return NoContent();

        context.MarkedFlags.Add(new MarkedFlag
        {
            GameId = instance.Challenge.GameId,
            ChallengeId = instance.ChallengeId,
            ParticipationId = instance.ParticipationId,
            TeamId = instance.Participation.TeamId,
            TeamName = instance.Participation.Team.Name,
            ChallengeTitle = instance.Challenge.Title,
            ContainerId = container.Id,
            ModelId = modelId,
            Flag = flag
        });
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<Container?> FindContainer(Guid token, CancellationToken cancellationToken) =>
        context.Containers.IgnoreAutoIncludes()
            .Include(c => c.GameInstance).ThenInclude(i => i!.Challenge)
            .Include(c => c.GameInstance).ThenInclude(i => i!.Participation).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(c => c.AiDetectionToken == token, cancellationToken);

    private static bool IsProbeEnabled(GameInstance instance) =>
        instance.Challenge.EnableAiDetection &&
        instance.Challenge.EffectiveContent.Type == ChallengeType.DynamicContainer;
}
