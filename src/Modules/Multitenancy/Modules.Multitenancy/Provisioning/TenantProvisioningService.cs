using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Hangfire;

using Jobs.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Data;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Provisioning;

public class TenantProvisioningService(
    TenantDbContext dbContext,
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IJobService jobService,
    IServiceScopeFactory scopeFactory,
    ILogger<TenantProvisioningService> logger)
    : ITenantProvisioningStarter, ITenantProvisioningReader, ITenantProvisioningStateWriter
{
    public async Task<TenantProvisioning> StartAsync(string tenantId, CancellationToken cancellationToken)
    {
        var tenant = await tenantStore.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new ArgumentException($"Tenant with id {tenantId} not found.", nameof(tenantId));

        var existing = await GetLastestAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && (existing.Status is TenantProvisioningStatus.Running or TenantProvisioningStatus.Pending))
        {
            throw new CustomException($"Provisioning already running for tenant {tenantId}.");
        }

        var correlationId = Guid.CreateVersion7().ToString();
        var provisioning = new TenantProvisioning(tenant.Id, correlationId);

        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Database));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Migrations));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.Seeding));
        provisioning.Steps.Add(new TenantProvisioningStep(provisioning.Id, TenantProvisioningStepName.CacheWarm));

        await dbContext.AddAsync(provisioning, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!TryEnsureJobStorage())
        {
            logger.LogWarning("Background job storage not available; running provisioning inline for tenant {TenantId}.", tenantId);
            provisioning.SetJobId("inline");
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await RunInlineProvisioningAsync(tenant.Id, correlationId, cancellationToken).ConfigureAwait(false);
            return provisioning;
        }

        var jobId = jobService.Enqueue<TenantProvisioningJob>(job => job.RunAsync(tenant.Id, correlationId, cancellationToken));
        provisioning.SetJobId(jobId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return provisioning;
    }

    public async Task<TenantProvisioning?> GetLastestAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<TenantProvisioning>().Include(x => x.Steps)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantProvisioningStatusDto> GetStatusAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await GetLastestAsync(tenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new ArgumentException($"Tenant with id {tenantId} not found.", nameof(tenantId));
        return ToDto(provisioning);
    }

    public async Task EnsureCanActivateAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await GetLastestAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (provisioning is null)
            return;
        if (provisioning.Status != TenantProvisioningStatus.Completed)
            throw new CustomException($"Tenant provisioning not completed for tenant {tenantId}.");
    }

    public async Task<string> RetryAsync(string tenantId, CancellationToken cancellationToken)
    {
        var provisioning = await StartAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return provisioning.CorrelationId;
    }

    public async Task<bool> MarkRunningAsync(string tenantId, string correlationId, TenantProvisioningStepName step,
        CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        var stepEntity = provisioning.Steps.First(s => s.Step == step);

        if (stepEntity.Status == TenantProvisioningStatus.Completed)
            return false;

        provisioning.MarkRunning(step.ToString());
        stepEntity.MarkRunning();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task MarkStepCompletedAsync(string tenantId, string correlationId, TenantProvisioningStepName step,
        CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        var stepEntity = provisioning.Steps.First(s => s.Step == step);

        if (stepEntity.Status == TenantProvisioningStatus.Completed)
            return;

        stepEntity.MarkCompleted();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(string tenantId, string correlationId, TenantProvisioningStepName step, string? error,
        CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
        provisioning.MarkFailed(step.ToString(), error!);

        var stepEntity = provisioning.Steps.First(s => s.Step == step);
        stepEntity.MarkFailed(error!);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkCompletedAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        var provisioning = await RequireAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);

        if (provisioning.Status == TenantProvisioningStatus.Completed)
        {
            return;
        }

        provisioning.MarkCompleted();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }


    #region private methods

    private async Task<TenantProvisioning> RequireAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<TenantProvisioning>()
                   .Include(x => x.Steps)
                   .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CorrelationId == correlationId, cancellationToken)
                   .ConfigureAwait(false) ??
               throw new ArgumentException($"Tenant provisioning not found for tenant {tenantId} and correlation id {correlationId}.");
    }

    private async Task RunInlineProvisioningAsync(string tenantId, string correlationId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TenantProvisioningJob>();
        await job.RunAsync(tenantId, correlationId, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryEnsureJobStorage()
    {
        try
        {
            _ = JobStorage.Current;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static TenantProvisioningStatusDto ToDto(TenantProvisioning provisioning)
    {
        var steps = provisioning.Steps.
            OrderBy(s => s.Step)
            .Select(s => new TenantProvisioningStepDto(
                s.Step.ToString(),
                s.Status.ToString(),
                s.StartedUtc,
                s.CompletedUtc,
                s.Error)).ToArray();

        return new TenantProvisioningStatusDto(
            provisioning.TenantId,
            provisioning.Status.ToString(),
            provisioning.CorrelationId,
            provisioning.CurrentStep,
            provisioning.Error,
            provisioning.CreatedUtc,
            provisioning.StartedUtc,
            provisioning.CompletedUtc,
            steps);
    }

    #endregion
}