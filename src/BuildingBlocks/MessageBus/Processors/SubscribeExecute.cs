using System.Diagnostics;
using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Exceptions;
using MessageBus.Model;
using MessageBus.Persistence;
using MessageBus.Subscribes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

internal sealed class SubscribeExecutor : ISubscribeExecutor
{
    private readonly ILogger<SubscribeExecutor> _logger;
    private readonly IOptions<MessageBusOptions> _options;
    private readonly IServiceProvider _provider;
    private readonly IDataStorage _dataStorage;
    private readonly SubscriptionMatcherCache _matcherCache;
    private readonly ISerializer _serializer;

    public SubscribeExecutor(
        ILogger<SubscribeExecutor> logger,
        IOptions<MessageBusOptions> options,
        IServiceProvider provider,
        SubscriptionMatcherCache matcherCache, ISerializer serializer)
    {
        _logger = logger;
        _options = options;
        _provider = provider;
        _dataStorage = provider.GetRequiredService<IDataStorage>();
        _matcherCache = matcherCache;
        _serializer = serializer;
    }

    public async Task<ResultResponse> ExecuteAsync(MessageContext message, ConsumerExecutorRegistration? registration = null, CancellationToken cancellationToken = default)
    {
        if (registration == null)
        {
            if (!_matcherCache.TryGetTopicExecutors(message.Name, message.Group, out var matches))
            {
                var error = $"Message (Name:{message.Name}, Group:{message.Group}) has no matching subscriber.";
                _logger.LogError(error);

                var ex = new ConsumerNotFoundException(error);
                await SetFailedStateAsync(message, ex);
                return ResultResponse.Fail(ex.Message);
            }

            registration = matches[0];
        }

        bool retry;
        ResultResponse result;

        do
        {
            var (shouldRetry, response) = await ExecuteWithoutRetryAsync(message, registration, cancellationToken)
                .ConfigureAwait(false);

            result = response;
            if (result.Succeeded) return result;
            retry = shouldRetry;
        } while (retry);

        return result;
    }

    private async Task<(bool shouldRetry, ResultResponse result)> ExecuteWithoutRetryAsync(MessageContext message, ConsumerExecutorRegistration registration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogDebug("Executing consumer {ConsumerType} for routing key '{RoutingKey}'.", registration.Descriptor.ConsumerType.Name, registration.Descriptor.RoutingKey);

            var sw = Stopwatch.StartNew();

            await InvokeConsumerAsync(message, registration, cancellationToken).ConfigureAwait(false);

            sw.Stop();

            await SetSuccessStateAsync(message).ConfigureAwait(false);

            _logger.LogDebug("Consumer {ConsumerType} executed in {ElapsedMs}ms.", registration.Descriptor.ConsumerType.Name, sw.Elapsed.TotalMilliseconds);

            return (false, ResultResponse.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer {ConsumerType} failed for message '{MessageName}' (DbId:{DbId}).", registration.Descriptor.ConsumerType.Name, message.Name, message.DbId);

            return (await SetFailedStateAsync(message, ex).ConfigureAwait(false), ResultResponse.Fail(ex.Message));
        }
    }

    private async Task InvokeConsumerAsync(
        MessageContext message,
        ConsumerExecutorRegistration registration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = message.Origin?.Value ?? throw new InvalidOperationException($"Message origin value is null for DbId:{message.DbId}.");

        using var scope = _provider.CreateScope();
        await registration.Executor.Execute(value, scope.ServiceProvider);
    }

    private Task SetSuccessStateAsync(MessageContext message)
    {
        message.ExpiresAt = DateTime.UtcNow.AddSeconds(_options.Value.SucceedMessageExpiredAfter);
        message.StatusName = StatusName.Succeeded;
        return _dataStorage.ChangeReceiveStateAsync(message, StatusName.Succeeded);
    }

    private async Task<bool> SetFailedStateAsync(MessageContext message, Exception ex)
    {
        if (ex is ConsumerNotFoundException)
            message.Retries = _options.Value.FailedRetryCount;

        var needRetry = UpdateMessageForRetry(message);

        message.ExpiresAt = message.Added.AddSeconds(_options.Value.FailedMessageExpiredAfter);
        message.StatusName = StatusName.Failed;

        await _dataStorage.ChangeReceiveStateAsync(message, StatusName.Failed).ConfigureAwait(false);

        return needRetry;
    }

    private bool UpdateMessageForRetry(MessageContext message)
    {
        var retries = ++message.Retries;
        var retryCount = Math.Min(_options.Value.FailedRetryCount, 3);

        if (retries >= retryCount)
        {
            if (retries == _options.Value.FailedRetryCount)
            {
                try
                {
                    _options.Value.FailedThresholdCallback?.Invoke(new FailedInfo
                    {
                        ServiceProvider = _provider,
                        MessageType = MessageType.Subscribe,
                        Message = message.Origin
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FailedThresholdCallback threw an exception.");
                }
            }

            return false;
        }

        _logger.LogDebug("Retrying message DbId:{DbId}, attempt {Retries}.", message.DbId, retries);
        return true;
    }
}