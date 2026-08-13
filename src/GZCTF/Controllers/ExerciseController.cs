using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Exercise;
using GZCTF.Models.Request.Game;
using GZCTF.Models.Request.Shared;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace GZCTF.Controllers;

/// <summary>
/// Exercise related APIs, available to all logged-in users without registration
/// </summary>
[RequireUser]
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ExerciseController(
    ILogger<ExerciseController> logger,
    UserManager<UserInfo> userManager,
    CacheHelper cacheHelper,
    IConfigService configService,
    IContainerRepository containerRepository,
    IExerciseInstanceRepository exerciseInstanceRepository,
    IExerciseSubmissionRepository exerciseSubmissionRepository,
    IExerciseEventRepository exerciseEventRepository,
    IExerciseCheatInfoRepository exerciseCheatInfoRepository,
    IPoolChallengeRepository poolChallengeRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    /// <summary>
    /// Get all range challenges grouped by category
    /// </summary>
    /// <remarks>
    /// Only challenges that are both globally enabled and enabled in the range are returned.
    /// The instance is lazily created when the user opens a challenge detail.
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved range challenges</response>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<ChallengeCategory, IEnumerable<ExerciseInfoModel>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallenges(CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var challenges = await poolChallengeRepository.GetRangeChallenges(token);

        if (challenges.Length == 0)
            return Ok(new Dictionary<ChallengeCategory, IEnumerable<ExerciseInfoModel>>());

        var solved = await exerciseInstanceRepository.GetSolvedIds(user.Id, token);
        var acceptedCounts = await exerciseInstanceRepository.GetAcceptedCounts(token);
        var submissionCounts = await exerciseSubmissionRepository.GetSubmissionCounts(token);

        var result = challenges
            .Select(c => new ExerciseInfoModel
            {
                Id = c.Id,
                Title = c.Title,
                Difficulty = c.Difficulty,
                Category = c.Category,
                Tags = c.Tags,
                Score = c.RangeScore,
                IsSolved = solved.Contains(c.Id),
                AcceptedCount = acceptedCounts.GetValueOrDefault(c.Id),
                SubmissionCount = submissionCounts.GetValueOrDefault(c.Id)
            })
            .GroupBy(c => c.Category)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        return Ok(result);
    }

    /// <summary>
    /// Get a range challenge detail, lazily creating the instance and dispatching dynamic flags
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved range challenge</response>
    /// <response code="404">Challenge not found or not enabled in the range</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExerciseDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var instance = await exerciseInstanceRepository.GetInstance(user, id, token);

        if (instance is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var attempts = await exerciseSubmissionRepository.CountSubmissions(user.Id, id, token);

        var model = ExerciseDetailModel.FromInstance(instance);
        model.Attempts = attempts;

        return Ok(model);
    }

    /// <summary>
    /// Submit a flag in the range, the result is returned synchronously
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="model">Flag submission</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully submitted flag</response>
    /// <response code="404">Challenge not found or not enabled in the range</response>
    /// <response code="400">Invalid operation</response>
    [HttpPost("{id:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Submit))]
    [ProducesResponseType(typeof(ExerciseSubmitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromBody] FlagSubmitModel model,
        CancellationToken token)
    {
        var answer = configService.DecryptApiData(model.Flag);
        if (string.IsNullOrWhiteSpace(answer))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_FlagRequired)]));

        if (answer.Length > Limits.MaxFlagLength)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_FlagTooLong)]));

        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var instance = await exerciseInstanceRepository.GetInstance(user, id, token);

        if (instance is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var currentAttempts = await exerciseSubmissionRepository.CountSubmissions(user.Id, id, token);
        var submissionLimit = instance.Exercise.SubmissionLimit;

        if (submissionLimit > 0 && currentAttempts >= submissionLimit)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Challenge_SubmissionLimitExceeded)]));

        if (instance.SolveTimeUtc > DateTimeOffset.FromUnixTimeSeconds(0))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Challenge_AlreadySolved)]));

        var status = await exerciseInstanceRepository.VerifyAnswer(user, instance, answer, token);

        var submission = await exerciseSubmissionRepository.AddSubmission(new()
        {
            UserId = user.Id,
            ExerciseId = id,
            Answer = answer,
            Status = status,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        }, token);

        // FlagSubmit event records the original judging result; a cheat detection
        // upgrade below is pushed separately as a CheatDetected event.
        await exerciseEventRepository.AddEvent(ExerciseEvent.FromSubmission(submission), token);

        if (status == AnswerResult.WrongAnswer)
        {
            var cheat = await exerciseCheatInfoRepository.CheckCheat(submission, token);
            if (cheat is not null)
            {
                status = AnswerResult.CheatDetected;

                await exerciseEventRepository.AddEvent(new()
                {
                    Type = EventType.CheatDetected,
                    UserId = user.Id,
                    ExerciseId = id,
                    Values =
                    [
                        submission.ChallengeName,
                        user.UserName ?? string.Empty,
                        cheat.SourceUser.UserName ?? string.Empty
                    ]
                }, token);
            }
        }

        if (status == AnswerResult.Accepted)
            await cacheHelper.FlushExerciseScoreboardCache(token);

        return Ok(new ExerciseSubmitResult
        {
            Status = status,
            IsSolved = status == AnswerResult.Accepted,
            Score = instance.Exercise.RangeScore
        });
    }

    /// <summary>
    /// Create a container for a range challenge
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully created range challenge container</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container creation not allowed for challenge</response>
    /// <response code="429">Too many requests</response>
    [HttpPost("{id:int}/Container")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var instance = await exerciseInstanceRepository.GetInstance(user, id, token);

        if (instance is null || !instance.Exercise.IsEnabled || !instance.Exercise.RangeEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Exercise.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.IsContainerOperationTooFrequent)
            return RequestResponse.Result(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests);

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerAlreadyCreated)]));

            await containerRepository.DestroyContainer(instance.Container, token);
        }

        return await exerciseInstanceRepository.CreateContainer(instance, user, token) switch
        {
            null or (TaskStatus.Failed, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationFailed)])),
            (TaskStatus.Denied, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNumberLimitExceeded),
                    containerPolicy.Value.MaxExerciseContainerCountPerUser])),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Extend the lifetime of a range challenge container
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully extended range challenge container</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container not created or cannot be extended</response>
    [HttpPost("{id:int}/Container/Extend")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendContainerLifetime([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var instance = await exerciseInstanceRepository.GetInstance(user, id, token);

        if (instance is null || !instance.Exercise.IsEnabled || !instance.Exercise.RangeEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Exercise.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.Container.ExpectStopAt - DateTimeOffset.UtcNow >
            TimeSpan.FromMinutes(containerPolicy.Value.RenewalWindow))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerExtensionNotAvailable)]));

        await containerRepository.ExtendLifetime(instance.Container,
            TimeSpan.FromMinutes(containerPolicy.Value.ExtensionDuration), token);

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// Delete a range challenge container
    /// </summary>
    /// <param name="id">Exercise ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted range challenge container</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container not created</response>
    /// <response code="429">Too many requests</response>
    [HttpDelete("{id:int}/Container")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var instance = await exerciseInstanceRepository.GetInstance(user, id, token);

        if (instance is null || !instance.Exercise.IsEnabled || !instance.Exercise.RangeEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Exercise.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.IsContainerOperationTooFrequent)
            return RequestResponse.Result(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests);

        if (!await containerRepository.DestroyContainer(instance.Container, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerDeletionFailed)]));

        await exerciseEventRepository.AddEvent(new()
        {
            Type = EventType.ContainerDestroy,
            UserId = user.Id,
            ExerciseId = id,
            Values = [id.ToString(), instance.Exercise.Title]
        }, token);

        instance.LastContainerOperation = DateTimeOffset.UtcNow;
        await exerciseInstanceRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// Get the global personal scoreboard of the range
    /// </summary>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved scoreboard</response>
    [HttpGet("Scoreboard")]
    [ProducesResponseType(typeof(ExerciseScoreboardModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScoreboard(CancellationToken token)
    {
        var scoreboard = await cacheHelper.GetOrCreateAsync(logger, CacheKey.ExerciseScoreboard,
            options =>
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return exerciseInstanceRepository.GetScoreboard(token);
            });

        return Ok(scoreboard);
    }

    /// <summary>
    /// Get all range events, requires Monitor permission
    /// </summary>
    /// <remarks>
    /// The training range is always open, so there is no start-time window check
    /// (unlike the game monitor endpoints).
    /// </remarks>
    /// <param name="hideContainer">Hide container start/destroy events</param>
    /// <param name="count">Number of events to return, max 100</param>
    /// <param name="skip">Events to skip</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved range events</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [RequireMonitor]
    [HttpGet("Events")]
    [ProducesResponseType(typeof(ExerciseEvent[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Events([FromQuery] bool hideContainer = false,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default) =>
        Ok(await exerciseEventRepository.GetEvents(hideContainer, count, skip, token));

    /// <summary>
    /// Get all range submissions, requires Monitor permission
    /// </summary>
    /// <param name="type">Filter by answer result</param>
    /// <param name="count">Number of submissions to return, max 100</param>
    /// <param name="skip">Submissions to skip</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved range submissions</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [RequireMonitor]
    [HttpGet("Submissions")]
    [ProducesResponseType(typeof(ExerciseSubmission[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submissions([FromQuery] AnswerResult? type = null,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default) =>
        Ok(await exerciseSubmissionRepository.GetSubmissions(type, count, skip, token));

    /// <summary>
    /// Get range cheat information, requires Monitor permission
    /// </summary>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved cheat information</response>
    /// <response code="401">Unauthorized user</response>
    /// <response code="403">Forbidden</response>
    [RequireMonitor]
    [HttpGet("CheatInfo")]
    [ProducesResponseType(typeof(ExerciseCheatInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheatInfo(CancellationToken token = default) =>
        Ok((await exerciseCheatInfoRepository.GetCheatInfos(token)).Select(ExerciseCheatInfoModel.FromCheatInfo));
}
