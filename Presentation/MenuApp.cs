using IRacingLeague.Business;
using IRacingLeague.Models;
using Spectre.Console;

namespace IRacingLeague.Presentation;

public class MenuApp
{
  private readonly ILeagueService _leagueService;
  private readonly IUserService _userService;
  private readonly IRegistrationService _registrationService;
  private readonly IRaceService _raceService;
  private readonly IResultService _resultService;
  private readonly IStandingsService _standingsService;
  private readonly AppConfig _config;

  // A single "active driver", set after password verification in Login(), is the
  // logged in identity that unlocks the private zone and owns the leagues created
  // while it is selected.
  private User? _activeUser;

  public MenuApp(ILeagueService leagueService, IUserService userService,
                 IRegistrationService registrationService, IRaceService raceService,
                 IResultService resultService, IStandingsService standingsService,
                 AppConfig config)
  {
    _leagueService = leagueService;
    _userService = userService;
    _registrationService = registrationService;
    _raceService = raceService;
    _resultService = resultService;
    _standingsService = standingsService;
    _config = config;
  }

  private static void Safe(Action action)
  {
    try
    {
      action();
    }
    catch (Exception ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Something went wrong: {ex.Message}[/]");
      Log.Error(ex);
    }
  }

  // ================= top level =================

  public void Run()
  {
    while (true)
    {
      Header();

      var choice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title("Select a zone")
              .HighlightStyle(new Style(foreground: Color.Yellow))
              .AddChoices("Public zone", "Private zone", "Exit"));

      switch (choice)
      {
        case "Public zone": PublicZone(); break;
        case "Private zone": PrivateZone(); break;
        case "Exit":
          AnsiConsole.MarkupLine("[grey]Bye![/]");
          return;
      }
    }
  }

  private void Header()
  {
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[yellow]iRacing League Manager[/]").LeftJustified());
    AnsiConsole.MarkupLineInterpolated($"[grey]Environment:[/] {_config.Environment}   [grey]Data:[/] {_config.DataDirectory}");
    string session = _activeUser is null
        ? "[blue]public — not logged in[/]"
        : $"logged in as {Markup.Escape(_activeUser.ToString())}";
    AnsiConsole.MarkupLine($"[grey]Session:[/] {session}");
  }

  // ================= public zone =================
  // No active driver required. Only public information is reachable here.

  private void PublicZone()
  {
    while (true)
    {
      var choice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title("[aqua]Public zone[/] [grey](open to everyone)[/]")
              .HighlightStyle(new Style(foreground: Color.Aqua))
              .AddChoices("Browse public leagues", "Open a public league",
                          "Browse driver profiles", "Search", "Back"));

      switch (choice)
      {
        case "Browse public leagues": Safe(ListPublicLeagues); break;
        case "Open a public league": Safe(OpenPublicLeague); break;
        case "Browse driver profiles": Safe(BrowseDriverProfiles); break;
        case "Search": SearchMenu(); break;
        case "Back": return;
      }
    }
  }

  private void ListPublicLeagues()
  {
    var leagues = _leagueService.GetPublic().ToList();
    if (leagues.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No public leagues yet.[/]");
      return;
    }
    RenderLeagues(leagues);
  }

  private void OpenPublicLeague()
  {
    var leagues = _leagueService.GetPublic().ToList();
    if (leagues.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No public leagues to open.[/]");
      return;
    }
    RenderLeagues(leagues);

    int leagueId = AskInt("Public league id to open:");
    var league = _leagueService.GetById(leagueId);   // KeyNotFound -> Safe
    if (!league.IsPublic)
    {
      AnsiConsole.MarkupLine("[yellow]That league is private — only its owner and members can view it.[/]");
      return;
    }

    AnsiConsole.MarkupLineInterpolated($"[aqua]{league.Name}[/] — read-only public view");
    ShowRaces(league.LeagueId);
    ShowStandings(league.LeagueId);
    ShowMembers(league.LeagueId);
  }

  private void BrowseDriverProfiles()
  {
    var drivers = _userService.GetAll().ToList();
    if (drivers.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No drivers yet.[/]");
      return;
    }
    RenderDrivers(drivers);   // public profile lines — never the password
  }

  // ---------------- Search (public) ----------------

  private void SearchMenu()
  {
    while (true)
    {
      var choice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title("[magenta]Search[/]")
              .HighlightStyle(new Style(foreground: Color.Fuchsia))
              .AddChoices("Find public leagues by name", "Find drivers by tag", "Back"));

      switch (choice)
      {
        case "Find public leagues by name": Safe(SearchLeaguesByName); break;
        case "Find drivers by tag": Safe(SearchDriversByTag); break;
        case "Back": return;
      }
    }
  }

  private void SearchLeaguesByName()
  {
    string term = AskText("Name contains:");
    // Search is a public-zone feature: never surface private leagues here.
    var matches = _leagueService.SearchByName(term).Where(l => l.IsPublic).ToList();
    if (matches.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No public leagues match.[/]");
      return;
    }
    RenderLeagues(matches);
  }

  private void SearchDriversByTag()
  {
    string term = AskText("Tag contains:");
    var matches = _userService.SearchByTag(term).ToList();
    if (matches.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No drivers match.[/]");
      return;
    }
    RenderDrivers(matches);
  }

  // ================= private zone =================
  // Gated behind an active driver. Shows that driver's own world.

  private void PrivateZone()
  {
    if (_activeUser is null)
    {
      Login();
      if (_activeUser is null)   // cancelled or no account created
      {
        AnsiConsole.MarkupLine("[grey]You need to log in (select a driver) to enter the private zone.[/]");
        return;
      }
    }

    while (true)
    {
      AnsiConsole.MarkupLineInterpolated($"[green]Private zone[/] — [grey]driver[/] {_activeUser}");

      // Admins get an account-wide tool for removing driver profiles; everyone
      // else only ever manages their own world.
      var choices = new List<string> { "My leagues", "Create league", "Join a league", "My profile & stats" };
      if (IsAdmin) choices.Add("Delete a driver (admin)");
      choices.AddRange(new[] { "Switch driver", "Log out", "Back" });

      var choice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title("[green]Private zone[/]")
              .HighlightStyle(new Style(foreground: Color.Green))
              .AddChoices(choices));

      switch (choice)
      {
        case "My leagues": Safe(MyLeagues); break;
        case "Create league": Safe(CreateLeague); break;
        case "Join a league": Safe(JoinLeague); break;
        case "My profile & stats": Safe(MyProfile); break;
        case "Delete a driver (admin)": Safe(DeleteDriver); break;
        case "Switch driver": Login(); break;
        case "Log out":
          _activeUser = null;
          AnsiConsole.MarkupLine("[grey]Logged out.[/]");
          return;
        case "Back": return;
      }
    }
  }

  private void Login()
  {
    const string create = "+ Create new account";
    const string cancel = "Cancel";

    var drivers = _userService.GetAll().ToList();
    var byLabel = new Dictionary<string, User>();
    var prompt = new SelectionPrompt<string>()
        .Title("[blue]Log in[/] — select an active driver")
        .HighlightStyle(new Style(foreground: Color.Blue));

    foreach (var d in drivers)
    {
      // No square brackets: selection choices are rendered as markup, and
      // "[1]" would be parsed as an (unbalanced) markup tag.
      string label = $"#{d.UserId} {d.UserName} ({d.Tag})";
      byLabel[label] = d;
      prompt.AddChoice(label);
    }
    prompt.AddChoices(create, cancel);

    string choice = AnsiConsole.Prompt(prompt);
    if (choice == cancel) return;

    if (choice == create)
    {
      var created = CreateDriver();
      if (created != null) _activeUser = created;
      return;
    }

    var selected = byLabel[choice];
    string password = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());
    if (!_userService.VerifyPassword(selected, password))
    {
      AnsiConsole.MarkupLine("[red]Incorrect password.[/]");
      Log.Error($"Incorrect password attempt for {selected.UserName}");
      return;
    }

    _activeUser = selected;
    AnsiConsole.MarkupLineInterpolated($"[green]Logged in as[/] {_activeUser}");
  }

  private User? CreateDriver()
  {
    string userName = AskText("Driver name:");
    string email = AskText("Email:");
    string tag = AskText("Tag:");
    string licenseClass = AskOptional("License class (Rookie/D/C/B/A):", "Rookie");
    string role = AskOptional("Role (Admin/Driver/Guest):", "Driver");
    string password = AskText("Password:");

    var user = _userService.Create(userName, email, password, role, tag, licenseClass);
    AnsiConsole.MarkupLineInterpolated($"[green]Created driver[/] {user}");
    return user;
  }

  private void MyProfile()
  {
    RenderDrivers(new[] { _activeUser! });

    // The driver's own per-league memberships — private information tied to them.
    var regs = _registrationService.GetByUser(_activeUser!.UserId).ToList();
    if (regs.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]You have not joined any league yet.[/]");
      return;
    }

    var table = new Table().Border(TableBorder.Rounded).Title("[green]My memberships[/]");
    table.AddColumns("League", "Car", "Team", "Points", "Ballast");
    foreach (var r in regs)
      table.AddRow(Markup.Escape(LeagueName(r.LeagueId)), r.CarNumber.ToString(),
          Markup.Escape(r.TeamName), r.Points.ToString(), $"{r.BallastKg:0.##}");
    AnsiConsole.Write(table);
  }

  // Admin-only: remove a driver profile. Deleting a driver cascades their league
  // memberships and race results (handled in UserService); a driver who owns
  // leagues is blocked so those leagues aren't left without a valid owner.
  private void DeleteDriver()
  {
    if (!IsAdmin)
    {
      AnsiConsole.MarkupLine("[yellow]Only admins can delete driver profiles.[/]");
      return;
    }

    // You can't delete the profile you're logged in as.
    var drivers = _userService.GetAll().Where(u => u.UserId != _activeUser!.UserId).ToList();
    if (drivers.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No other drivers to delete.[/]");
      return;
    }
    RenderDrivers(drivers);

    const string back = "Back";
    var byLabel = new Dictionary<string, User>();
    var prompt = new SelectionPrompt<string>()
        .Title("[red]Delete driver[/] — select a profile to remove")
        .HighlightStyle(new Style(foreground: Color.Red));
    foreach (var d in drivers)
    {
      string label = $"#{d.UserId} {d.UserName} ({d.Tag}) — {d.Role}";
      byLabel[label] = d;
      prompt.AddChoice(label);
    }
    prompt.AddChoice(back);

    string choice = AnsiConsole.Prompt(prompt);
    if (choice == back) return;
    var driver = byLabel[choice];

    var owned = _leagueService.GetOwnedBy(driver.UserId).ToList();
    if (owned.Count > 0)
    {
      string names = string.Join(", ", owned.Select(l => l.Name));
      AnsiConsole.MarkupLineInterpolated(
          $"[yellow]Driver #{driver.UserId} owns {owned.Count} league(s): {names}. Delete or hand off those leagues first.[/]");
      return;
    }

    int regCount = _registrationService.GetByUser(driver.UserId).Count();
    string warn = regCount > 0
        ? $" This also removes {regCount} league membership(s) and all their race results."
        : string.Empty;
    if (!AnsiConsole.Confirm(
            $"Delete driver '{Markup.Escape(driver.UserName)}' (#{driver.UserId})?{warn} This cannot be undone.",
            defaultValue: false))
      return;

    _userService.Delete(driver.UserId);
    AnsiConsole.MarkupLineInterpolated($"[green]Deleted driver[/] #{driver.UserId}.");
  }

  private void CreateLeague()
  {
    string name = AskText("League name:");
    string discipline = AskText("Discipline (GT3/Oval/Formula...):");
    bool isPublic = AnsiConsole.Confirm("Public?");
    int maxDrivers = AskInt("Max drivers:");
    decimal entryFee = AskDecimal("Entry fee:");

    var league = _leagueService.Create(name, discipline, isPublic, maxDrivers, entryFee, _activeUser!.UserId);
    AnsiConsole.MarkupLineInterpolated($"[green]Created league[/] {league}");
  }

  private void JoinLeague()
  {
    // You can join any public league, plus your own (possibly private) leagues.
    var joinable = _leagueService.GetPublic()
        .Concat(_leagueService.GetOwnedBy(_activeUser!.UserId))
        .GroupBy(l => l.LeagueId).Select(g => g.First())
        .OrderBy(l => l.LeagueId).ToList();
    if (joinable.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No leagues available to join. Create one first.[/]");
      return;
    }
    RenderLeagues(joinable);

    int leagueId = AskInt("League id to join:");
    int carNumber = AskInt("Car number:");
    string teamName = AskText("Team name:");
    decimal ballast = AskDecimal("Ballast (kg):");

    try
    {
      var registration = _registrationService.Register(_activeUser!.UserId, leagueId, carNumber, teamName, ballast);
      AnsiConsole.MarkupLineInterpolated($"[green]Registered:[/] {registration}");
    }
    catch (DuplicateRegistrationException ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Cannot join: {ex.Message}[/]");
      Log.Error(ex);
    }
    catch (LeagueFullException ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Cannot join: {ex.Message}[/]");
      Log.Error(ex);
    }
    catch (KeyNotFoundException ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Cannot join: {ex.Message}[/]");
      Log.Error(ex);
    }
  }

  private void AddDriver(League league)
  {
    var registeredUserIds = _registrationService.GetByLeague(league.LeagueId)
        .Select(r => r.UserId).ToHashSet();
    var available = _userService.GetAll()
        .Where(u => !registeredUserIds.Contains(u.UserId)).ToList();
    if (available.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]Every driver is already registered in this league.[/]");
      return;
    }

    const string cancel = "Back";

    var byLabel = new Dictionary<string, User>();
    var prompt = new SelectionPrompt<string>()
        .Title("[green]Add driver[/] — select a driver to register")
        .HighlightStyle(new Style(foreground: Color.Green));
    foreach (var u in available)
    {
      string label = $"#{u.UserId} {u.UserName} ({u.Tag})";
      byLabel[label] = u;
      prompt.AddChoice(label);
    }
    prompt.AddChoice(cancel);

    string choice = AnsiConsole.Prompt(prompt);
    if (choice == cancel) return;
    var selected = byLabel[choice];

    int carNumber = AskInt("Car number:");
    string teamName = AskText("Team name:");
    decimal ballast = AskDecimal("Ballast (kg):");

    try
    {
      var registration = _registrationService.Register(selected.UserId, league.LeagueId, carNumber, teamName, ballast);
      AnsiConsole.MarkupLineInterpolated($"[green]Registered:[/] {registration}");
    }
    catch (DuplicateRegistrationException ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Cannot add driver: {ex.Message}[/]");
      Log.Error(ex);
    }
    catch (LeagueFullException ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Cannot add driver: {ex.Message}[/]");
      Log.Error(ex);
    }
  }

  private void MyLeagues()
  {
    var ownedIds = _leagueService.GetOwnedBy(_activeUser!.UserId).Select(l => l.LeagueId).ToHashSet();
    var joinedIds = _registrationService.GetByUser(_activeUser.UserId).Select(r => r.LeagueId).ToHashSet();
    // Admins manage the whole grid, so they can open any league — not just the
    // ones they own or have joined. Everyone else sees only their own world.
    var allIds = (IsAdmin
            ? _leagueService.GetAll().Select(l => l.LeagueId)
            : ownedIds.Union(joinedIds))
        .ToHashSet();
    if (allIds.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]You don't own or belong to any league yet. Create one or join from the menu.[/]");
      return;
    }

    var leagues = allIds.Select(id => _leagueService.GetById(id))
        .OrderBy(l => l.LeagueId).ToList();

    var table = new Table().Border(TableBorder.Rounded).Title("[green]My leagues[/]");
    table.AddColumns("Id", "Name", "Discipline", "Visibility", "My role");
    foreach (var l in leagues)
    {
      string role = ownedIds.Contains(l.LeagueId)
          ? (joinedIds.Contains(l.LeagueId) ? "Owner + Member" : "Owner")
          : joinedIds.Contains(l.LeagueId) ? "Member" : "Admin";
      table.AddRow(l.LeagueId.ToString(), Markup.Escape(l.Name), Markup.Escape(l.Discipline),
          l.IsPublic ? "public" : "[yellow]private[/]", role);
    }
    AnsiConsole.Write(table);

    int leagueId = AskInt("Open which league id?");
    if (!allIds.Contains(leagueId))
    {
      AnsiConsole.MarkupLine("[yellow]That league is not in your list (you can only open leagues you own or have joined).[/]");
      return;
    }
    LeagueDetail(_leagueService.GetById(leagueId));
  }

  private void LeagueDetail(League league)
  {
    while (true)
    {
      bool isOwner = league.OwnerUserId == _activeUser!.UserId;
      string role = isOwner
          ? "you are the owner"
          : IsAdmin
              ? $"owner is #{league.OwnerUserId} — admin access"
              : $"owner is #{league.OwnerUserId} — view only";
      var choice = AnsiConsole.Prompt(
          new SelectionPrompt<string>()
              .Title($"[green]{Markup.Escape(league.Name)}[/] [grey]({(league.IsPublic ? "public" : "private")}, {role})[/]")
              .HighlightStyle(new Style(foreground: Color.Green))
              .AddChoices("View races", "View standings", "View members", "Add driver",
                          "Schedule race", "Enter result", "Delete race", "Delete result",
                          "Edit league", "Delete league", "Back"));

      switch (choice)
      {
        case "View races": Safe(() => ShowRaces(league.LeagueId)); break;
        case "View standings": Safe(() => ShowStandings(league.LeagueId)); break;
        case "View members": Safe(() => ShowMembers(league.LeagueId)); break;
        case "Add driver": if (ManageGuard(league)) Safe(() => AddDriver(league)); break;
        case "Schedule race": if (ManageGuard(league)) Safe(() => ScheduleRaceFor(league)); break;
        case "Enter result": if (ManageGuard(league)) Safe(() => EnterResultFor(league)); break;
        case "Delete race": if (ManageGuard(league)) Safe(() => DeleteRaceFor(league)); break;
        case "Delete result": if (ManageGuard(league)) Safe(() => DeleteResultFor(league)); break;
        case "Edit league": if (ManageGuard(league)) Safe(() => EditLeague(league)); break;
        case "Delete league":
          if (ManageGuard(league) && Safe(() => DeleteLeague(league))) return;
          break;
        case "Back": return;
      }
    }
  }

  // Admins have league-wide management rights — they can manage any league, not
  // just the ones they own. Role is free text on the account, so match loosely.
  private bool IsAdmin =>
      string.Equals(_activeUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);

  // A league may be managed by its owner or by any admin. Renamed from the old
  // owner-only check so the gate reflects role-based authorization.
  private bool ManageGuard(League league)
  {
    if (_activeUser != null && (league.OwnerUserId == _activeUser.UserId || IsAdmin)) return true;
    AnsiConsole.MarkupLineInterpolated(
        $"[yellow]Only the league owner (#{league.OwnerUserId}) or an admin can manage this league.[/]");
    return false;
  }

  // ---- league management (owner or admin), acting on a league we already hold ----

  private void ScheduleRaceFor(League league)
  {
    // Leaving the first field blank is the way out of the whole entry chain.
    string? track = AskTextOrBack("Track (blank to go back):");
    if (track is null) return;
    string car = AskText("Car:");
    DateTime scheduledAt = AskDateTime("Scheduled at (yyyy-MM-dd HH:mm):");
    int lapCount = AskInt("Lap count:");
    decimal ambientTemp = AskDecimal("Ambient temp (C):");

    var race = _raceService.Create(league.LeagueId, track, car, scheduledAt, lapCount, ambientTemp);
    AnsiConsole.MarkupLineInterpolated($"[green]Scheduled race[/] {race}");
  }

  private void EnterResultFor(League league)
  {
    var races = _raceService.GetByLeague(league.LeagueId).ToList();
    if (races.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No races in this league. Schedule one first.[/]");
      return;
    }
    ShowRaces(league.LeagueId);

    const string back = "Back";

    // Pick from this league's own races/members: a "Back" choice is the way out,
    // and the closed lists guarantee the selection belongs to this league.
    var raceByLabel = new Dictionary<string, Race>();
    var racePrompt = new SelectionPrompt<string>()
        .Title("[green]Enter result[/] — select a race")
        .HighlightStyle(new Style(foreground: Color.Green));
    foreach (var r in races)
    {
      string label = $"#{r.RaceId} round {r.Round} — {Markup.Escape(r.Track)}";
      raceByLabel[label] = r;
      racePrompt.AddChoice(label);
    }
    racePrompt.AddChoice(back);

    string raceChoice = AnsiConsole.Prompt(racePrompt);
    if (raceChoice == back) return;
    Race race = raceByLabel[raceChoice];

    var members = _registrationService.GetByLeague(league.LeagueId).ToList();
    if (members.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No registered drivers in this league.[/]");
      return;
    }
    ViewMembersTable(league.LeagueId, members);

    var regByLabel = new Dictionary<string, Registration>();
    var regPrompt = new SelectionPrompt<string>()
        .Title("[green]Enter result[/] — select a driver")
        .HighlightStyle(new Style(foreground: Color.Green));
    foreach (var m in members)
    {
      string label = $"#{m.RegistrationId} {Markup.Escape(DriverName(m.UserId))} (car {m.CarNumber})";
      regByLabel[label] = m;
      regPrompt.AddChoice(label);
    }
    regPrompt.AddChoice(back);

    string regChoice = AnsiConsole.Prompt(regPrompt);
    if (regChoice == back) return;
    Registration registration = regByLabel[regChoice];

    int position = AskInt("Finishing position:");
    decimal fastestLap = AskDecimal("Fastest lap (seconds):");
    int points = AskInt("Points awarded:");
    int incidents = AskInt("Incident points:");
    int lapsCompleted = AskInt($"Laps completed (of {race.LapCount}):");
    if (lapsCompleted < 0) lapsCompleted = 0;
    if (lapsCompleted > race.LapCount) lapsCompleted = race.LapCount;
    bool dnf = AnsiConsole.Confirm("DNF?", defaultValue: false);
    string notes = AskOptional("Notes (optional):", string.Empty);

    var result = new Result(registration.RegistrationId, race.RaceId, position, fastestLap, points, incidents, lapsCompleted, dnf, notes);
    _resultService.ApplyResult(registration, race, result);
    AnsiConsole.MarkupLineInterpolated($"[green]Result recorded:[/] {result}");
    AnsiConsole.MarkupLineInterpolated($"  [grey]League standing now:[/] {registration}");
    AnsiConsole.MarkupLineInterpolated($"  [grey]Driver global stats now:[/] {_userService.GetById(registration.UserId)}");
  }

  private void DeleteRaceFor(League league)
  {
    var races = _raceService.GetByLeague(league.LeagueId).ToList();
    if (races.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No races to delete in this league.[/]");
      return;
    }
    ShowRaces(league.LeagueId);

    const string back = "Back";
    var byLabel = new Dictionary<string, Race>();
    var prompt = new SelectionPrompt<string>()
        .Title("[red]Delete race[/] — select a race to remove")
        .HighlightStyle(new Style(foreground: Color.Red));
    foreach (var r in races)
    {
      string label = $"#{r.RaceId} round {r.Round} — {Markup.Escape(r.Track)}";
      byLabel[label] = r;
      prompt.AddChoice(label);
    }
    prompt.AddChoice(back);

    string choice = AnsiConsole.Prompt(prompt);
    if (choice == back) return;
    var race = byLabel[choice];

    // Warn when results ride along: deleting the race reverts their points/stats too.
    int resultCount = _resultService.GetByRace(race.RaceId).Count();
    string warn = resultCount > 0
        ? $" This also deletes {resultCount} recorded result(s) and reverts the affected drivers' points and stats."
        : string.Empty;
    if (!AnsiConsole.Confirm(
            $"Delete race #{race.RaceId} (round {race.Round}, {Markup.Escape(race.Track)})?{warn} This cannot be undone.",
            defaultValue: false))
      return;

    _raceService.Delete(race.RaceId);
    AnsiConsole.MarkupLineInterpolated($"[green]Deleted race[/] #{race.RaceId}.");
  }

  private void DeleteResultFor(League league)
  {
    var races = _raceService.GetByLeague(league.LeagueId).ToList();
    var raceById = races.ToDictionary(r => r.RaceId);
    var results = races.SelectMany(r => _resultService.GetByRace(r.RaceId)).ToList();
    if (results.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No results recorded in this league yet.[/]");
      return;
    }

    const string back = "Back";
    var byLabel = new Dictionary<string, Result>();
    var prompt = new SelectionPrompt<string>()
        .Title("[red]Delete result[/] — select a result to remove")
        .HighlightStyle(new Style(foreground: Color.Red));
    foreach (var res in results.OrderBy(r => raceById[r.RaceId].Round))
    {
      var race = raceById[res.RaceId];
      string driver = DriverName(_registrationService.GetById(res.RegistrationId).UserId);
      string label = $"#{res.ResultId} round {race.Round} {Markup.Escape(race.Track)} — " +
                     $"{Markup.Escape(driver)}: P{res.Position}, {res.Points} pts";
      byLabel[label] = res;
      prompt.AddChoice(label);
    }
    prompt.AddChoice(back);

    string choice = AnsiConsole.Prompt(prompt);
    if (choice == back) return;
    var result = byLabel[choice];

    if (!AnsiConsole.Confirm(
            $"Delete result #{result.ResultId}? This reverts the driver's {result.Points} standings points " +
            "and this race's effect on their global stats. This cannot be undone.",
            defaultValue: false))
      return;

    _resultService.Delete(result.ResultId);
    AnsiConsole.MarkupLineInterpolated($"[green]Deleted result[/] #{result.ResultId}.");
  }

  private void EditLeague(League league)
  {
    league.Name = AskTextDefault("League name:", league.Name);
    league.Discipline = AskTextDefault("Discipline:", league.Discipline);
    league.IsPublic = AnsiConsole.Confirm("Public?", defaultValue: league.IsPublic);
    league.MaxDrivers = AskIntDefault("Max drivers:", league.MaxDrivers);
    league.EntryFee = AskDecimalDefault("Entry fee:", league.EntryFee);

    _leagueService.Update(league);
    AnsiConsole.MarkupLineInterpolated($"[green]Updated league[/] {league}");
  }

  private bool DeleteLeague(League league)
  {
    if (!AnsiConsole.Confirm($"Delete league '{Markup.Escape(league.Name)}'? This cannot be undone.", defaultValue: false))
      return false;
    _leagueService.Delete(league.LeagueId);
    AnsiConsole.MarkupLineInterpolated($"[green]Deleted league[/] #{league.LeagueId}.");
    return true;
  }

  // ---------------- shared views ----------------

  private void ShowRaces(int leagueId) => RenderRaces(leagueId);

  private void ShowStandings(int leagueId)
  {
    var standings = _standingsService.GetStandings(leagueId).OrderByDescending(s => s.Registration.Points).ToList();
    if (standings.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No registered drivers in this league.[/]");
      return;
    }

    var table = new Table().Border(TableBorder.Rounded).Title($"[yellow]Standings — league #{leagueId}[/]");
    table.AddColumns("Pos", "Driver", "Points", "Races");
    int pos = 1;
    foreach (var s in standings)
      table.AddRow((pos++).ToString(), Markup.Escape(DriverName(s.Registration.UserId)), s.Registration.Points.ToString(), s.RacesCompleted.ToString());
    AnsiConsole.Write(table);
  }

  private void ShowMembers(int leagueId)
  {
    var members = _registrationService.GetByLeague(leagueId).ToList();
    if (members.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No members in this league yet.[/]");
      return;
    }
    ViewMembersTable(leagueId, members);
  }

  // ---------------- shared rendering ----------------

  private static void RenderLeagues(IEnumerable<League> leagues)
  {
    var table = new Table().Border(TableBorder.Rounded);
    table.AddColumns("Id", "Name", "Discipline", "Public", "Max", "Fee", "Owner");
    foreach (var l in leagues)
      table.AddRow(l.LeagueId.ToString(), Markup.Escape(l.Name), Markup.Escape(l.Discipline),
          l.IsPublic ? "yes" : "no", l.MaxDrivers.ToString(), l.EntryFee.ToString("0.##"), $"#{l.OwnerUserId}");
    AnsiConsole.Write(table);
  }

  private string DriverName(int userId)
  {
    try { return _userService.GetById(userId).UserName; }
    catch (KeyNotFoundException) { return "unknown"; }
  }

  private string LeagueName(int leagueId)
  {
    try { return _leagueService.GetById(leagueId).Name; }
    catch (KeyNotFoundException) { return $"league #{leagueId}"; }
  }

  private static void RenderDrivers(IEnumerable<User> drivers)
  {
    var table = new Table().Border(TableBorder.Rounded);
    table.AddColumns("Id", "Name", "Tag", "Role", "License", "iRating", "SR", "Wins");
    foreach (var d in drivers)
      table.AddRow(d.UserId.ToString(), Markup.Escape(d.UserName), Markup.Escape(d.Tag),
          Markup.Escape(d.Role), Markup.Escape(d.LicenseClass), d.IRating.ToString(),
          d.SafetyRating.ToString("0.00"), d.TotalWins.ToString());
    AnsiConsole.Write(table);
  }

  private void RenderRaces(int leagueId)
  {
    var races = _raceService.GetByLeague(leagueId).ToList();   // ordered by Round
    if (races.Count == 0)
    {
      AnsiConsole.MarkupLine("[grey]No races scheduled for this league.[/]");
      return;
    }

    var table = new Table().Border(TableBorder.Rounded).Title($"[yellow]Races — league #{leagueId}[/]");
    table.AddColumns("Id", "Round", "Track", "Car", "ScheduledAt", "Laps", "TempC", "Completed");
    foreach (var r in races)
      table.AddRow(r.RaceId.ToString(), r.Round.ToString(), Markup.Escape(r.Track), Markup.Escape(r.Car),
          r.ScheduledAt.ToString("yyyy-MM-dd HH:mm"), r.LapCount.ToString(), $"{r.AmbientTempC:0.#}",
          r.IsCompleted ? "yes" : "no");
    AnsiConsole.Write(table);
  }

  private void ViewMembersTable(int leagueId, IEnumerable<Registration> members)
  {
    var table = new Table().Border(TableBorder.Rounded).Title($"[yellow]Registered drivers — league #{leagueId}[/]");
    table.AddColumns("RegId", "Driver", "Car", "Team", "Points", "Ballast");
    foreach (var m in members)
      table.AddRow(m.RegistrationId.ToString(), Markup.Escape(DriverName(m.UserId)),
          m.CarNumber.ToString(), Markup.Escape(m.TeamName), m.Points.ToString(), $"{m.BallastKg:0.##}");
    AnsiConsole.Write(table);
  }

  // ---------------- Spectre input helpers ----------------

  private static string AskText(string label) =>
      AnsiConsole.Prompt(new TextPrompt<string>(label)
          .Validate(s => string.IsNullOrWhiteSpace(s)
              ? ValidationResult.Error("[red]Value cannot be empty[/]")
              : ValidationResult.Success()));

  // Like AskText, but a blank entry means "go back" and yields null.
  private static string? AskTextOrBack(string label)
  {
    string value = AnsiConsole.Prompt(new TextPrompt<string>(label).AllowEmpty());
    return string.IsNullOrWhiteSpace(value) ? null : value;
  }

  private static string AskOptional(string label, string fallback) =>
      AnsiConsole.Prompt(new TextPrompt<string>(label)
          .AllowEmpty()
          .DefaultValue(fallback)
          .HideDefaultValue());

  private static string AskTextDefault(string label, string current) =>
      AnsiConsole.Prompt(new TextPrompt<string>(label).DefaultValue(current));

  private static int AskInt(string label) => AnsiConsole.Ask<int>(label);

  private static int AskIntDefault(string label, int current) =>
      AnsiConsole.Prompt(new TextPrompt<int>(label).DefaultValue(current));

  private static decimal AskDecimal(string label) => AnsiConsole.Ask<decimal>(label);

  private static decimal AskDecimalDefault(string label, decimal current) =>
      AnsiConsole.Prompt(new TextPrompt<decimal>(label).DefaultValue(current));

  private static DateTime AskDateTime(string label) => AnsiConsole.Ask<DateTime>(label);

  // Safe wrapper that reports whether the action ran without throwing — lets
  // Delete signal "league gone, leave the detail view".
  private static bool Safe(Func<bool> action)
  {
    try { return action(); }
    catch (Exception ex)
    {
      AnsiConsole.MarkupLineInterpolated($"[red]Something went wrong: {ex.Message}[/]");
      Log.Error(ex);
      return false;
    }
  }
}
