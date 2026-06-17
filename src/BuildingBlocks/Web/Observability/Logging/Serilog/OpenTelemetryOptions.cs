namespace Web.Observability.Logging.Serilog;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetryOptions";

    public bool Enabled { get; set; } = true;

    public TracingOptions Tracing { get; set; } = new();

    public MetricsOptions Metrics { get; set; } = new();

    public ExporterOptions Exporter { get; set; } = new();

    public JobsOptions Jobs { get; set; } = new();

    public MediatorOptions Mediator { get; set; } = new();

    public HttpOptions Http { get; set; } = new();

    public DataOptions Data { get; set; } = new();
}

public sealed class TracingOptions
{
    public bool Enabled { get; set; } = true;
}

public sealed class MetricsOptions
{
    public bool Enabled { get; set; } = true;

    public List<string> MeterNames { get; set; } = [];
}

public sealed class ExporterOptions
{
    public OtlpExporterOptions Otlp { get; set; } = new();
}

public sealed class OtlpExporterOptions
{
    public bool Enabled { get; set; }

    public string? Endpoint { get; set; }

    public string Protocol { get; set; } = "grpc";
}

public sealed class JobsOptions
{
    public bool Enabled { get; set; } = true;
}

public sealed class MediatorOptions
{
    public bool Enabled { get; set; } = true;
}

public sealed class HttpOptions
{
    public HistogramOptions Histograms { get; set; } = new();
}

public sealed class HistogramOptions
{
    public bool Enabled { get; set; } = true;
}

public sealed class DataOptions
{
    public bool FilterEfStatements { get; set; } = true;

    public bool FilterRedisCommands { get; set; } = true;
}