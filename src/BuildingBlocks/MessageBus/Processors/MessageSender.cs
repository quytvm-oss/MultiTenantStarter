using MessageBus.Constants;
using MessageBus.Contracts;
using MessageBus.Exceptions;
using MessageBus.Model;
using MessageBus.Persistence;
using MessageBus.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageBus.Processors;

internal class MessageSender : IMessageSender
{
    private readonly IDataStorage _dataStorage;
    private readonly ILogger _logger;
    private readonly IOptions<MessageBusOptions> _options;
    private readonly ISerializer _serializer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITransport _transport;
    
    public MessageSender(
        ILogger<MessageSender> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _options = serviceProvider.GetRequiredService<IOptions<MessageBusOptions>>();
        _dataStorage = serviceProvider.GetRequiredService<IDataStorage>();
        _serializer = serviceProvider.GetRequiredService<ISerializer>();
        _transport = serviceProvider.GetRequiredService<ITransport>();
    }
    
   public async Task<ResultResponse> SendAsync(MessageContext message)
    {
        bool retry;
        ResultResponse result;
        do
        {
            (retry, result) = await SendWithoutRetryAsync(message).ConfigureAwait(false);
            if (result.Equals(ResultResponse.Success)) return result;
        } while (retry);

        return result;
    }

    private async Task<(bool, ResultResponse)> SendWithoutRetryAsync(MessageContext message)
    {
        var transportMsg = await _serializer.SerializeAsync(message.Origin).ConfigureAwait(false);

        var result = await _transport.SendAsync(transportMsg).ConfigureAwait(false);

        if (result.Succeeded)
        {
            await SetSuccessfulState(message).ConfigureAwait(false);
            
            return (false, ResultResponse.Success);
        }

        var needRetry = await SetFailedState(message, result.Exception!).ConfigureAwait(false);

        return (needRetry, ResultResponse.Fail(result.Exception?.Message!));
    }

    private async Task SetSuccessfulState(MessageContext message)
    {
        message.ExpiresAt = DateTime.UtcNow.AddSeconds(_options.Value.SucceedMessageExpiredAfter);
        await _dataStorage.ChangePublishStateAsync(message, StatusName.Succeeded).ConfigureAwait(false);
    }

    private async Task<bool> SetFailedState(MessageContext message, Exception ex)
    {
        var needRetry = UpdateMessageForRetry(message);

        message.Origin.AddOrUpdateException(ex);
        message.ExpiresAt = message.Added.AddSeconds(_options.Value.FailedMessageExpiredAfter);

        await _dataStorage.ChangePublishStateAsync(message, StatusName.Failed).ConfigureAwait(false);

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
                        ServiceProvider = _serviceProvider,
                        MessageType = MessageType.Subscribe,
                        Message = message.Origin
                    });

                    _logger.LogWarning(
                        "Message '{DbId}' has reached the failed retry threshold of {RetryCount}.",
                        message.DbId, _options.Value.FailedRetryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FailedThresholdCallback threw an exception.");
                }
            }

            return false;
        }

        _logger.LogDebug(
            "Retrying message '{DbId}', attempt {Retries}/{MaxRetries}.",
            message.DbId, retries, retryCount);

        return true;
    }
}