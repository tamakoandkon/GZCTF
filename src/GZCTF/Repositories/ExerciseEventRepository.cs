using GZCTF.Hubs;
using GZCTF.Hubs.Clients;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ExerciseEventRepository(
    IHubContext<MonitorHub, IMonitorClient> hub,
    AppDbContext context) : RepositoryBase(context), IExerciseEventRepository
{
    public async Task<ExerciseEvent> AddEvent(ExerciseEvent exerciseEvent, CancellationToken token = default)
    {
        await Context.AddAsync(exerciseEvent, token);
        await SaveAsync(token);

        exerciseEvent = await Context.ExerciseEvents.SingleAsync(s => s.Id == exerciseEvent.Id, token);

        await hub.Clients.Group("Exercise").ReceivedExerciseEvent(exerciseEvent);

        return exerciseEvent;
    }

    public Task<ExerciseEvent[]> GetEvents(bool hideContainer = false, int count = 50, int skip = 0,
        CancellationToken token = default)
    {
        var data = Context.ExerciseEvents.AsNoTracking();

        if (hideContainer)
            data = data.Where(e => e.Type != EventType.ContainerStart && e.Type != EventType.ContainerDestroy);

        return data.OrderByDescending(e => e.PublishTimeUtc).Skip(skip).Take(count).ToArrayAsync(token);
    }
}
