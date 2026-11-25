using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace GZCTF.Controllers;

/// <summary>
/// Viewer (live scoreboard) single sign-on APIs
/// </summary>
/// <remarks>
/// The viewer service is deployed independently. A signed-in user is handed over to it
/// with a one-time ticket so that the viewer never asks for a password itself.
/// </remarks>
[ApiController]
[Route("api/viewer")]
[Produces(MediaTypeNames.Application.Json)]
public class ViewerController(
    IConfiguration configuration,
    IDistributedCache cache,
    ILogger<ViewerController> logger,
    UserManager<UserInfo> userManager,
    ITeamRepository teamRepository) : ControllerBase
{
    /// <summary>
    /// Authentication cookie issued by the identity scheme
    /// </summary>
    private const string AuthCookieName = "GZCTF_Token";

    private const string TicketCachePrefix = "viewer-sso-ticket:";
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(2);
    private static readonly DistributedCacheEntryOptions TicketCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TicketLifetime
    };

    private sealed record HandoffTicket(
        Guid UserId,
        string UserName,
        int TeamId,
        string TeamName,
        string AuthCookie,
        DateTimeOffset ExpiresAt);

    private sealed record SsoUrlModel(string Url);

    private sealed record SsoIdentityModel(
        Guid UserId,
        string UserName,
        int TeamId,
        string TeamName,
        string AuthCookie);

    private string? ViewerOrigin => configuration["ViewerSso:Origin"];

    private string? ExchangeSecret => configuration["ViewerSso:Secret"];

    /// <summary>
    /// Generate a one-time viewer handoff link
    /// </summary>
    /// <remarks>
    /// Requires a signed-in user with a team. The returned url carries a short-lived
    /// single-use ticket that the viewer exchanges on the server side.
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Ticket created</response>
    /// <response code="400">The user has not joined any team</response>
    /// <response code="401">Login required</response>
    /// <response code="503">Viewer integration is not configured</response>
    [HttpGet("Sso")]
    [RequireUser]
    [ProducesResponseType(typeof(SsoUrlModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateHandoff(CancellationToken token)
    {
        if (!Uri.TryCreate(ViewerOrigin, UriKind.Absolute, out var viewerOrigin))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "观赛系统地址尚未配置" });

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new { message = "登录状态已失效，请重新登录" });

        var teams = await teamRepository.GetUserTeams(user, token);
        var team = teams.FirstOrDefault(t => t.CaptainId == user.Id) ?? teams.FirstOrDefault();
        if (team is null)
            return BadRequest(new { code = "TEAM_REQUIRED", message = "请先加入战队后再进入观赛" });

        if (!Request.Cookies.TryGetValue(AuthCookieName, out var authCookie) ||
            string.IsNullOrWhiteSpace(authCookie))
            return Unauthorized(new { message = "登录状态已失效，请重新登录" });

        var code = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var ticket = new HandoffTicket(
            user.Id,
            user.UserName ?? string.Empty,
            team.Id,
            team.Name ?? string.Empty,
            authCookie,
            DateTimeOffset.UtcNow.Add(TicketLifetime));

        await cache.SetStringAsync(
            TicketCachePrefix + code,
            JsonSerializer.Serialize(ticket),
            TicketCacheOptions,
            token);

        var url = new Uri(viewerOrigin, $"sso?code={Uri.EscapeDataString(code)}");
        return Ok(new SsoUrlModel(url.ToString()));
    }

    /// <summary>
    /// Exchange a one-time ticket for the viewer identity
    /// </summary>
    /// <remarks>
    /// Server to server only: the viewer service authenticates with a shared secret.
    /// The ticket is consumed on first use.
    /// </remarks>
    /// <param name="code">One-time ticket code</param>
    /// <param name="token"></param>
    /// <response code="200">Identity released</response>
    /// <response code="401">Invalid secret</response>
    /// <response code="404">Ticket is invalid or expired</response>
    /// <response code="503">Viewer integration is not configured</response>
    [HttpGet("Exchange")]
    [ProducesResponseType(typeof(SsoIdentityModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exchange([FromQuery] string? code, CancellationToken token)
    {
        var secret = ExchangeSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "观赛对接密钥尚未配置" });

        var provided = Request.Headers["X-Viewer-Secret"].ToString();
        if (!FixedTimeEquals(provided, secret))
        {
            logger.LogWarning("Viewer SSO exchange rejected: invalid shared secret from {Address}",
                HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { message = "对接凭据无效" });
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length > 128)
            return NotFound(new { code = "TICKET_INVALID", message = "观赛凭据无效或已过期" });

        var cacheKey = TicketCachePrefix + code;
        var payload = await cache.GetStringAsync(cacheKey, token);
        if (payload is null)
            return NotFound(new { code = "TICKET_INVALID", message = "观赛凭据无效或已过期" });

        await cache.RemoveAsync(cacheKey, token);

        HandoffTicket? ticket;
        try
        {
            ticket = JsonSerializer.Deserialize<HandoffTicket>(payload);
        }
        catch (JsonException)
        {
            ticket = null;
        }

        if (ticket is null || ticket.ExpiresAt <= DateTimeOffset.UtcNow)
            return NotFound(new { code = "TICKET_INVALID", message = "观赛凭据无效或已过期" });

        return Ok(new SsoIdentityModel(
            ticket.UserId,
            ticket.UserName,
            ticket.TeamId,
            ticket.TeamName,
            ticket.AuthCookie));
    }

    private static bool FixedTimeEquals(string? left, string right)
    {
        if (left is null) return false;
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
