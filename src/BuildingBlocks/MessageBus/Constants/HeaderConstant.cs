namespace MessageBus.Constants;

public static class HeaderConstant
{
    public const string MessageId = "msg-id";
    public const string MessageName = "msg-name";
    public const string Group = "msg-group";
    public const string Type = "msg-type";
    public const string CorrelationId = "corr-id";
    public const string CorrelationSequence = "corr-seq";
    public const string SentTime = "senttime";
    public const string ExecutionInstanceId = "exec-instance-id";
    public const string DelayTime = "delaytime";
    public const string Exception = "exception";
    public const string TraceParent = "traceparent"; 
    public const string TenantId = "tenant-id";
    public const string Source = "svc-source";
}