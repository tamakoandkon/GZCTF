using GZCTF.Models;
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

    [HttpPost("report")]
    [RequestSizeLimit(512)]
    public async Task<IActionResult> Report([FromHeader(Name = "X-Ai-Probe-Token")] Guid token,
        [FromBody] ReportModel report, CancellationToken cancellationToken)
    {
        var modelId = report.ModelId?.Trim();
        if (token == Guid.Empty || string.IsNullOrWhiteSpace(modelId) || modelId.Length > 128 ||
            modelId.Any(char.IsControl))
            return BadRequest();

        var container = await context.Containers.IgnoreAutoIncludes()
            .Include(c => c.GameInstance).ThenInclude(i => i!.Challenge)
            .Include(c => c.GameInstance).ThenInclude(i => i!.Participation).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(c => c.AiDetectionToken == token, cancellationToken);

        var instance = container?.GameInstance;
        if (container is null || instance is null ||
            !instance.Challenge.EnableAiDetection ||
            instance.Challenge.EffectiveContent.Type != ChallengeType.DynamicContainer)
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
}
