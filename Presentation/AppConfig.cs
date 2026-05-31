namespace IRacingLeague.Presentation;

/// <summary>
/// Startup configuration derived from the environment. Built once in Program.cs
/// from the APP_ENV variable and injected where needed (menu header, data path).
/// </summary>
public class AppConfig
{
    public string Environment { get; }
    public string DataDirectory { get; }

    public AppConfig(string environment, string dataDirectory)
    {
        Environment = environment;
        DataDirectory = dataDirectory;
    }
}
