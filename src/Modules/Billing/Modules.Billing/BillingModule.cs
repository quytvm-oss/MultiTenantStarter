using Asp.Versioning;

using Messaging.Rebus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Billing.Contracts.Authorization;
using Modules.Billing.Data;
using Modules.Billing.Features.v1.Invoices.GenerateInvoices;
using Modules.Billing.Features.v1.Invoices.GetInvoiceById;
using Modules.Billing.Features.v1.Invoices.GetInvoices;
using Modules.Billing.Features.v1.Invoices.GetMyInvoices;
using Modules.Billing.Features.v1.Invoices.IssueInvoice;
using Modules.Billing.Features.v1.Invoices.MarkInvoicePaid;
using Modules.Billing.Features.v1.Invoices.VoidInvoice;
using Modules.Billing.Features.v1.Plans.CreatePlan;
using Modules.Billing.Features.v1.Plans.GetPlans;
using Modules.Billing.Features.v1.Plans.UpdatePlan;
using Modules.Billing.Features.v1.Subscriptions.AssignSubscription;
using Modules.Billing.Features.v1.Subscriptions.GetSubscription;
using Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;
using Modules.Billing.Features.v1.Usage.GetUsageSnapshots;
using Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;
using Modules.Billing.Features.v1.Wallets.CreateTopupRequest;
using Modules.Billing.Features.v1.Wallets.GetMyTopupRequests;
using Modules.Billing.Features.v1.Wallets.GetMyWallet;
using Modules.Billing.Features.v1.Wallets.GetTopupRequests;
using Modules.Billing.Features.v1.Wallets.RejectTopupRequest;
using Modules.Billing.Messaging;
using Modules.Billing.Services;

using Persistence;

using Shared.Identity;

using Web.Modules;

namespace Modules.Billing;

public class BillingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder, bool IsWebHost = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(BillingPermissions.All);

        var services = builder.Services;

        services.AddCustomDbContext<BillingDbContext>();
        services.AddScoped<IDbInitializer, BillingDbInitializer>();
        services.AddScoped<IUsageReporter, UsageReporter>();
        services.AddScoped<IBillingService, BillingService>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<BillingDbContext>(
                name: "db:billing",
                failureStatus: HealthStatus.Unhealthy);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRebusSubscription, BillingSubscribe>());
        if (IsWebHost)
            services.AddBillingMessaging(builder.Configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/billing")
            .WithTags("Billing")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapGetPlansEndpoint();
        group.MapCreatePlanEndpoint();
        group.MapUpdatePlanEndpoint();

        group.MapAssignSubscriptionEndpoint();
        group.MapGetSubscriptionEndpoint();
        group.MapGetMySubscriptionEndpoint();

        group.MapGetUsageSnapshotsEndpoint();
        group.MapCaptureUsageSnapshotsEndpoint();

        group.MapGetMyWalletEndpoint();
        group.MapCreateTopupRequestEndpoint();
        group.MapGetMyTopupRequestsEndpoint();

        group.MapGetTopupRequestsEndpoint();
        group.MapApproveTopupRequestEndpoint();
        group.MapRejectTopupRequestEndpoint();

        group.MapGetInvoicesEndpoint();
        group.MapGetMyInvoicesEndpoint();
        group.MapGetInvoiceByIdEndpoint();
        group.MapIssueInvoiceEndpoint();
        group.MapMarkInvoicePaidEndpoint();
        group.MapVoidInvoiceEndpoint();
        group.MapGenerateInvoicesEndpoint();
    }
}