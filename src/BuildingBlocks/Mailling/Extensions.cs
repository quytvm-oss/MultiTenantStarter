using Mailling.Smtp;

using Mailling.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mailling;

public static class Extensions
{
    public static IServiceCollection AddAppMailing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MailOptions>()
            .BindConfiguration(nameof(MailOptions))
            .ValidateOnStart();
        var mailOptions = configuration.GetSection(nameof(MailOptions)).Get<MailOptions>() ?? new MailOptions();

        if (mailOptions.Provider?.Trim().ToLowerInvariant() == "smtp")
        {
            services.AddTransient<IMailService>(sp => new SmtpMailService(sp.GetRequiredService<IOptions<MailOptions>>(),
                sp.GetRequiredService<ILogger<SmtpMailService>>()));
        }
        
        return services;
    }
}