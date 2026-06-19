using IRacingLeague.Business;
using IRacingLeague.Data;
using IRacingLeague.Presentation;
using Microsoft.Extensions.DependencyInjection;

// Read the environment up front (defaults to "local") and isolate persistence per environment
string env = Environment.GetEnvironmentVariable("APP_ENV") ?? "local";
string dataDir = Path.Combine("data", env);

var config = new AppConfig(env, dataDir);

// Send errors to logs/<env>/errors.log so each environment keeps its own log
Log.Configure(env);

var services = new ServiceCollection();

services.AddSingleton(config);
services.AddSingleton<ILeagueRepository>(_ => new JsonLeagueRepository(dataDir));
services.AddSingleton<IUserRepository>(_ => new JsonUserRepository(dataDir));
services.AddSingleton<IRegistrationRepository>(_ => new JsonRegistrationRepository(dataDir));
services.AddSingleton<IRaceRepository>(_ => new JsonRaceRepository(dataDir));
services.AddSingleton<IResultRepository>(_ => new JsonResultRepository(dataDir));

services.AddTransient<ILeagueService, LeagueService>();
services.AddTransient<IUserService, UserService>();
services.AddTransient<IRegistrationService, RegistrationService>();
services.AddTransient<IRaceService, RaceService>();
services.AddTransient<IResultService, ResultService>();
services.AddTransient<IStandingsService, StandingsService>();
services.AddTransient<MenuApp>();

using var provider = services.BuildServiceProvider();
provider.GetRequiredService<MenuApp>().Run();
