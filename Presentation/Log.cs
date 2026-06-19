namespace IRacingLeague.Presentation;

public static class Log
{
  private static string _logDirectory = "logs";
  private static string LogFile => Path.Combine(_logDirectory, "errors.log");
  private static readonly object Sync = new();

  public static void Configure(string environment)
  {
    lock (Sync)
      _logDirectory = Path.Combine("logs", environment);
  }

  public static void Error(string message)
  {
    lock (Sync)
    {
      Directory.CreateDirectory(_logDirectory);
      File.AppendAllText(LogFile,
          $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}{Environment.NewLine}");
    }
  }

  public static void Error(Exception ex) => Error(ex.Message);
}
