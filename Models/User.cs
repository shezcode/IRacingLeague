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

    /// <summary>
    /// Recompute account-wide stats from one race outcome (rich-model rule, the
    /// equivalent of BankAccount.MakeDeposit). Called by ResultService.ApplyResult.
    /// - IRating: position-based delta — a win gains most, tapering by position,
    ///   floored so a bad result can't crater the rating.
    /// - TotalWins: incremented on a win (Position == 1).
    /// - SafetyRating: incidents drag it down, clamped to [0, 4.99].
    /// </summary>
    public void ApplyRaceOutcome(int position, int incidentPoints)
    {
        int iRatingDelta = Math.Max(50 - (position - 1) * 10, -30);
        IRating += iRatingDelta;

        if (position == 1)
            TotalWins++;

        SafetyRating -= incidentPoints * 0.05m;
        if (SafetyRating < 0m) SafetyRating = 0m;
        if (SafetyRating > 4.99m) SafetyRating = 4.99m;
    }

    /// <summary>
    /// Inverse of ApplyRaceOutcome — used by ResultService.ApplyResult to back out
    /// a previously-applied outcome before applying an edited one.
    /// </summary>
    public void UndoRaceOutcome(int position, int incidentPoints)
    {
        int iRatingDelta = Math.Max(50 - (position - 1) * 10, -30);
        IRating -= iRatingDelta;

        if (position == 1)
            TotalWins--;

        SafetyRating += incidentPoints * 0.05m;
        if (SafetyRating < 0m) SafetyRating = 0m;
        if (SafetyRating > 4.99m) SafetyRating = 4.99m;
    }

    public override string ToString() =>
        $"[{UserId}] {UserName} ({Tag}) — {Role}, license {LicenseClass}, " +
        $"iR {IRating}, SR {SafetyRating:0.00}, wins {TotalWins}";
}
