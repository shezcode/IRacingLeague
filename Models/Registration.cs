namespace IRacingLeague.Models;

public class Registration
{
    public int RegistrationId { get; set; }
    public int UserId { get; set; }      // FK -> User
    public int LeagueId { get; set; }    // FK -> League
    public int CarNumber { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int Points { get; set; }      // standings points in this league
    public decimal BallastKg { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedLeagueAt { get; set; }

    // Parameterless ctor kept for JSON (de)serialization in a later step.
    public Registration() { }

    public Registration(int userId, int leagueId, int carNumber, string teamName, decimal ballastKg)
    {
        UserId = userId;
        LeagueId = leagueId;
        CarNumber = carNumber;
        TeamName = teamName;
        BallastKg = ballastKg;
        Points = 0;
        IsActive = true;
        JoinedLeagueAt = DateTime.Now;
    }

    public void AddPoints(int points) => Points += points;

    public override string ToString() =>
        $"[{RegistrationId}] driver #{UserId} — car {CarNumber}, team {TeamName}, " +
        $"{Points} pts, ballast {BallastKg:0.##}kg{(IsActive ? "" : " (inactive)")}";
}
