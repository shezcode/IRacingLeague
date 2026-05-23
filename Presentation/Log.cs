namespace IRacingLeague.Presentation;

/// <summary>
/// Centralized error log. Appends timestamped lines to logs/errors.log so the
/// menu never has to know how errors are persisted. The logs/ directory is the
/// second Docker volume in the containerization step.
/// </summary>
public static class Log
{
    private const string LogDirectory = "logs";
    private static readonly string LogFile = Path.Combine(LogDirectory, "errors.log");
    private static readonly object Sync = new();

    public static void Error(string message)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}{Environment.NewLine}");
        }
    }

    public static void Error(Exception ex) => Error(ex.Message);
}
