using System.Diagnostics.CodeAnalysis;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;

namespace GZCTF.Hubs;

[ExcludeFromCodeCoverage]
public class MonitorHub : Hub<IMonitorClient>
{
    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();

        if (context is null)
        {
            Context.Abort();
            return;
        }

        // Training range scope: the range is global (no per-game id), join a fixed group.
        // Auth: Monitor role or valid API token, same as the game scope.
        if (context.Request.Query.ContainsKey("exercise"))
        {
            if (!await ContextHelper.HasMonitor(context) && !await ContextHelper.HasValidToken(context))
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
            await Groups.AddToGroupAsync(Context.ConnectionId, "Exercise");
            return;
        }

        if (!context.Request.Query.TryGetValue("game", out var gameId)
            || !int.TryParse(gameId, out var gId)
            || (!await ContextHelper.HasMonitor(context) && !await ContextHelper.HasValidToken(context)))
        {
            Context.Abort();
            return;
        }

        var gameRepository = context.RequestServices.GetRequiredService<IGameRepository>();
        if (!await gameRepository.HasGameAsync(gId))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Game_{gId}");
    }
}
