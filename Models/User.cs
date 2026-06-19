namespace IRacingLeague.Models;

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;   // SHA256 hash
    public string Role { get; set; } = "Driver";            // Admin / Driver / Guest
    public string Tag { get; set; } = string.Empty;
    public int IRating { get; set; }                        // global, recomputed from results
    public decimal SafetyRating { get; set; }
    public string LicenseClass { get; set; } = "Rookie";    // Rookie / D / C / B / A
    public bool IsActive { get; set; }
    public int TotalWins { get; set; }                      // global, recomputed
    public DateTime JoinedAt { get; set; }

    // Parameterless ctor kept for JSON (de)serialization in a later step.
    public User() { }

    public User(string userName, string email, string password, string role, string tag, string licenseClass)
    {
        UserName = userName;
        Email = email;
        Password = password;
        Role = role;
        Tag = tag;
        LicenseClass = licenseClass;
        IRating = 1350;          // iRacing's default starting iRating
        SafetyRating = 2.50m;
        TotalWins = 0;
        IsActive = true;
        JoinedAt = DateTime.Now;
    }

    // Safety Rating tuning. SrSensitivity scales how strongly one race's
    // cleanliness moves SR; SrMaxSwing caps a single race so SR can't lurch (real
    // SR moves are gradual). Both are expressed in SR points.
    private const decimal SrSensitivity = 0.5m;
    private const decimal SrMaxSwing = 0.50m;

    /// <summary>
    /// Recompute account-wide stats from one race outcome (rich-model rule, the
    /// equivalent of BankAccount.MakeDeposit). Called by ResultService.ApplyResult.
    /// - IRating: position-based delta — a win gains most, tapering by position,
    ///   floored so a bad result can't crater the rating.
    /// - TotalWins: incremented on a win (Position == 1).
    /// - SafetyRating: Corners-Per-Incident — incidents are weighed against the
    ///   distance covered (laps as a corners proxy), so a clean race over distance
    ///   RAISES SR and a crash-strewn one lowers it, clamped to [0, 4.99].
    /// </summary>
    public void ApplyRaceOutcome(int position, int incidentPoints, int lapsCompleted)
    {
        int iRatingDelta = Math.Max(50 - (position - 1) * 10, -30);
        IRating += iRatingDelta;

        if (position == 1)
            TotalWins++;

        SafetyRating = ClampSafetyRating(SafetyRating + SafetyRatingDelta(incidentPoints, lapsCompleted));
    }

    /// <summary>
    /// Inverse of ApplyRaceOutcome — used by ResultService.ApplyResult to back out
    /// a previously-applied outcome before applying an edited one. Recomputes the
    /// same SR delta and subtracts it, so the round-trip is exact unless a clamp
    /// boundary was hit.
    /// </summary>
    public void UndoRaceOutcome(int position, int incidentPoints, int lapsCompleted)
    {
        int iRatingDelta = Math.Max(50 - (position - 1) * 10, -30);
        IRating -= iRatingDelta;

        if (position == 1)
            TotalWins--;

        SafetyRating = ClampSafetyRating(SafetyRating - SafetyRatingDelta(incidentPoints, lapsCompleted));
    }

    /// <summary>
    /// Per-race Safety Rating delta from Corners-Per-Incident. Laps stand in for
    /// corners: this race's incidents-per-lap is compared to a per-class break-even
    /// target — cleaner than target gives a positive delta (SR up), dirtier gives a
    /// negative one. A zero-lap result (e.g. a lap-0 DNF) carries no distance to
    /// judge, so SR is left untouched.
    /// </summary>
    private decimal SafetyRatingDelta(int incidentPoints, int lapsCompleted)
    {
        if (lapsCompleted <= 0) return 0m;

        decimal incidentsPerLap = (decimal)incidentPoints / lapsCompleted;
        decimal delta = (BreakEvenIncidentsPerLap(LicenseClass) - incidentsPerLap) * SrSensitivity;
        return Math.Clamp(delta, -SrMaxSwing, SrMaxSwing);
    }

    // Higher licenses demand cleaner racing, so their break-even incidents-per-lap
    // is stricter (a smaller allowance before SR starts falling).
    private static decimal BreakEvenIncidentsPerLap(string licenseClass) => licenseClass switch
    {
        "A" => 0.20m,
        "B" => 0.25m,
        "C" => 0.30m,
        "D" => 0.35m,
        _ => 0.40m,   // Rookie / unknown
    };

    private static decimal ClampSafetyRating(decimal value) => Math.Clamp(value, 0m, 4.99m);

    public override string ToString() =>
        $"[{UserId}] {UserName} ({Tag}) — {Role}, license {LicenseClass}, " +
        $"iR {IRating}, SR {SafetyRating:0.00}, wins {TotalWins}";
}
