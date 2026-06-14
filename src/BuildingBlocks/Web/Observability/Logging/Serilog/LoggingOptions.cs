using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace Web.Observability.Logging.Serilog;

public class LoggingOptions
{
    public static string SectionName = "Logging"; 
    
    public Dictionary<string, LogLevel> LogLevel { get; set; } = new();
    public FileOptions File { get; set; }
    public SeqOptions Seq { get; set; }

}

public class FileOptions
{
    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "logs/log-.txt";

    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
        
    public int? FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    public bool RollOnFileSizeLimit { get; set; } = true;

    public int? RetainedFileCountLimit { get; set; } = 7; // giữ 7 file

    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
}

public class SeqOptions
{
    public bool Enabled { get; set; }
    public string ServerUrl { get; set; }
}