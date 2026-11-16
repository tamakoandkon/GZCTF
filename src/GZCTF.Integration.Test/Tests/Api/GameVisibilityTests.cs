using System.Net;
using System.Net.Http.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for hidden game visibility.
/// A hidden game is private to the administration and to users that already take
/// part in it; every other caller must receive 404 so that hidden game ids cannot
/// be probed (which would otherwise leak the game, its scoreboard and its notices).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class GameVisibilityTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task HiddenGame_ShouldBeInvisibleToAnonymous()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Hidden Game {TestDataSeeder.RandomName(6)}", hidden: true);
        using var client = factory.CreateClient();

        // Act & Assert
        var detailResponse = await client.GetAsync($"/api/Game/{game.Id}");
        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);

        var scoreboardResponse = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        Assert.Equal(HttpStatusCode.NotFound, scoreboardResponse.StatusCode);

        var noticesResponse = await client.GetAsync($"/api/Game/{game.Id}/Notices");
        Assert.Equal(HttpStatusCode.NotFound, noticesResponse.StatusCode);
    }

    [Fact]
    public async Task PublicGame_ShouldRemainVisibleToAnonymous()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Public Game {TestDataSeeder.RandomName(6)}");
        using var client = factory.CreateClient();

        // Act & Assert
        var detailResponse = await client.GetAsync($"/api/Game/{game.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var scoreboardResponse = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        Assert.Equal(HttpStatusCode.OK, scoreboardResponse.StatusCode);

        var noticesResponse = await client.GetAsync($"/api/Game/{game.Id}/Notices");
        Assert.Equal(HttpStatusCode.OK, noticesResponse.StatusCode);
    }

    [Fact]
    public async Task HiddenGame_ShouldRejectJoinAndCheckForRegularUser()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Hidden Join Game {TestDataSeeder.RandomName(6)}", hidden: true);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act
        var joinResponse = await client.PostAsJsonAsync($"/api/Game/{game.Id}",
            new GameJoinModel { TeamId = team.Id });
        var checkResponse = await client.GetAsync($"/api/Game/{game.Id}/Check");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, joinResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, checkResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var participationRepo = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var participation = await participationRepo.GetParticipation(user.Id, game.Id, CancellationToken.None);
        Assert.Null(participation);
    }

    [Fact]
    public async Task HiddenGame_ShouldBeVisibleToMonitor()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Hidden Monitor Game {TestDataSeeder.RandomName(6)}", hidden: true);

        var monitorPassword = "Monitor@Pass123";
        var monitor = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), monitorPassword, role: Role.Monitor);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = monitor.UserName, Password = monitorPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act & Assert
        var detailResponse = await client.GetAsync($"/api/Game/{game.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var scoreboardResponse = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        Assert.Equal(HttpStatusCode.OK, scoreboardResponse.StatusCode);

        var noticesResponse = await client.GetAsync($"/api/Game/{game.Id}/Notices");
        Assert.Equal(HttpStatusCode.OK, noticesResponse.StatusCode);
    }

    [Fact]
    public async Task HiddenGame_ShouldBeVisibleToParticipant()
    {
        // Arrange
        var game = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"Hidden Participant Game {TestDataSeeder.RandomName(6)}", hidden: true);

        var userPassword = "User@Pass123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id,
            $"Team {user.UserName}");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResponse.EnsureSuccessStatusCode();

        // Act & Assert
        var detailResponse = await client.GetAsync($"/api/Game/{game.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var scoreboardResponse = await client.GetAsync($"/api/Game/{game.Id}/Scoreboard");
        Assert.Equal(HttpStatusCode.OK, scoreboardResponse.StatusCode);
    }
}
