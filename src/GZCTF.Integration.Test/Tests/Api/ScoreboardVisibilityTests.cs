using System.Net;
using GZCTF.Integration.Test.Base;
using GZCTF.Repositories.Interface;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for scoreboard visibility before the game starts.
/// The scoreboard cache can be generated ahead of time (admin edits/flushes), so
/// the public endpoint must check the start time before serving the cached value.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ScoreboardVisibilityTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task FutureGame_ShouldNotServeCachedScoreboard()
    {
        // Arrange: a game that starts in the future, with a pre-warmed scoreboard cache
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = start.AddHours(2);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Future Scoreboard Game {TestDataSeeder.RandomName(6)}", start: start, end: end);

        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var gameEntity = await gameRepository.GetGameById(game.Id, CancellationToken.None);
            Assert.NotNull(gameEntity);

            // Generate the scoreboard cache the same way a game update/flush would
            await gameRepository.GetScoreboard(gameEntity!, CancellationToken.None);

            var cached = await gameRepository.TryGetScoreboard(game.Id, CancellationToken.None);
            Assert.NotNull(cached);
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartedGame_ShouldServeScoreboardFromCache()
    {
        // Arrange: a started game with a pre-warmed scoreboard cache
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Started Scoreboard Game {TestDataSeeder.RandomName(6)}");

        using (var scope = factory.Services.CreateScope())
        {
            var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var gameEntity = await gameRepository.GetGameById(game.Id, CancellationToken.None);
            Assert.NotNull(gameEntity);
            await gameRepository.GetScoreboard(gameEntity!, CancellationToken.None);
        }

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
