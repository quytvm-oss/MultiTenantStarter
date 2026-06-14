using Hangfire.Client;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace Jobs;

public class LogJobFilter : IClientFilter, IServerFilter, IElectStateFilter, IApplyStateFilter
{
    private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    public LogJobFilter()
    {
        
    }
    
    public void OnCreating(CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var job = context.Job;
        var jobName = job.Method.Name;
        
        Log.DebugFormat("Creating job for {0}.", jobName);
    }

    public void OnCreated(CreatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var job = context.Job;
        var jobName = job.Method.Name;
        var jobId = context.BackgroundJob?.Id ?? "<unknown>";
        var recurringJobId = context.Parameters.TryGetValue("RecurringJobId", out var recurringJobIdValue) ? 
            recurringJobIdValue :null;
        
        Log.DebugFormat("Job created: Id={0}, Name={1}, RecurringJobId={2}", 
            jobId, 
            jobName,
            recurringJobId ?? "<none>");
    }

    public void OnPerforming(PerformingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var backgroundJob = context.BackgroundJob;
        var job = backgroundJob.Job;
        var jobName = job.Method.Name;
        var recurringJobId = context.GetJobParameter<string?>("RecurringJobId") ?? "<none>";
        var args = FormatArguments(job);
        
        Log.DebugFormat(
            "Starting job: Id={0}, Name={1}, RecurringJobId={2}, Queue={3}, Args={4}",
            backgroundJob.Id,
            jobName,
            recurringJobId,
            backgroundJob.Job.Queue,
            args);
    }

    public void OnPerformed(PerformedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var backgroundJob = context.BackgroundJob;
        var job = backgroundJob.Job;
        var jobName =job.Method.Name;

        Log.DebugFormat(
            "Job completed: Id={0}, Name={1}, Succeeded={2}",
            backgroundJob.Id,
            jobName,
            context.Exception == null);
    }

    public void OnStateElection(ElectStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.CandidateState is FailedState failedState)
        {
            var job = context.BackgroundJob.Job;
            var jobName = job.Method.Name;
            Log.WarnFormat(
                "Job '{0}' failed. Name={1}, Reason={2}",
                context.BackgroundJob.Id,
                jobName,
                failedState.Exception);
        }
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(context);
        var job = context.BackgroundJob.Job;
        var jobName = job.Method.Name;
        Log.DebugFormat(
            "Job state changed: Id={0}, Name={1}, OldState={2}, NewState={3}",
            context.BackgroundJob.Id,
            jobName,
            context.OldStateName ?? "<none>",
            context.NewState?.Name ?? "<none>");
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(context);
        var job = context.BackgroundJob.Job;
        var jobName = job.Method.Name;
        Log.DebugFormat(
            "Job state unapplied: Id={0}, Name={1}, OldState={2}",
            context.BackgroundJob.Id,
            jobName,
            context.OldStateName ?? "<none>");
    }
    
    private static string FormatArguments(Hangfire.Common.Job job)
    {
        var args = job.Args;
        if (args == null || args.Count == 0)
            return "[]";

        var parameters = job.Method.GetParameters();
        
        try
        {
            var rendered = args.Select((arg, i) =>
            {
                var isSensitive = i < parameters.Length
                                  && parameters[i].IsDefined(typeof(SensitiveDataAttribute), inherit: false);

                if (isSensitive)
                    return "<redacted>";

                return arg?.ToString() ?? "null";
            });

            return "[" + string.Join(", ", rendered) + "]";
        }
        catch (Exception ex)
        {
            Log.DebugFormat("Failed to format job arguments: {0}", ex.Message);
            return "[<unavailable>]";
        }
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SensitiveDataAttribute : Attribute { }