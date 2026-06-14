using System.Linq.Expressions;

using Hangfire;

namespace Jobs.Services;

public class HangfireJobService : IJobService
{
    private readonly IBackgroundJobClient _client;
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireJobService(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _client = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    public bool Delete(string jobId)
    => _client.Delete(jobId);

    public bool Delete(string jobId, string fromState)
        => _client.Delete(jobId, fromState);

    public string Enqueue(Expression<Action> methodCall)
     => _client.Enqueue(methodCall);

    public string Enqueue(string queue, Expression<Func<Task>> methodCall)
        => _client.Enqueue(queue, methodCall);

    public string Enqueue(Expression<Func<Task>> methodCall)
        => _client.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall)
        => _client.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        => _client.Enqueue(methodCall);

    public bool Requeue(string jobId)
     => _client.Requeue(jobId);

    public bool Requeue(string jobId, string fromState)
        => _client.Requeue(jobId, fromState);

    public string Schedule(Expression<Action> methodCall, DateTime delay)
       => _client.Schedule(methodCall, delay);

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
        => _client.Schedule(methodCall, delay);

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
        => _client.Schedule(methodCall, enqueueAt);

    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
        => _client.Schedule(methodCall, enqueueAt);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
        => _client.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        => _client.Schedule(methodCall, delay);

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
        => _client.Schedule(methodCall, enqueueAt);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
        => _client.Schedule(methodCall, enqueueAt);
}