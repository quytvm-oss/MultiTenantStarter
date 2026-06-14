using Microsoft.Extensions.Diagnostics.HealthChecks;

using RabbitMQ.Client;

namespace Web.Health;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConnectionFactory _connectionFactory;

    public RabbitMqHealthCheck(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            
            await channel.QueueDeclareAsync("health_check", durable: false, 
                exclusive: true, autoDelete: true, cancellationToken: cancellationToken);
            
            return HealthCheckResult.Healthy("RabbitMQ is accessible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is not accessible.", ex);
        }
    }
}