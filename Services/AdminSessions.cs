namespace SignageApp.Services;

public sealed class AdminSessions : BackgroundService
{
    private const long TimeoutMilliseconds = 20_000;
    private readonly object gate = new();
    private readonly Dictionary<Guid, long> lastSeen = new();

    public bool IsActive
    {
        get
        {
            lock (gate)
            {
                RemoveExpired();
                return lastSeen.Count > 0;
            }
        }
    }

    public void Ping(Guid sessionId)
    {
        lock (gate)
        {
            RemoveExpired();
            lastSeen[sessionId] = Environment.TickCount64;
        }
    }

    public void Clear(Guid sessionId)
    {
        lock (gate)
        {
            RemoveExpired();
            lastSeen.Remove(sessionId);
        }
    }

    private void RemoveExpired()
    {
        var now = Environment.TickCount64;
        foreach (var id in lastSeen.Where(entry => now - entry.Value >= TimeoutMilliseconds)
                     .Select(entry => entry.Key).ToArray())
            lastSeen.Remove(id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            lock (gate) RemoveExpired();
        }
    }
}
