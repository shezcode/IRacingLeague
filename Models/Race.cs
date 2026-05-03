namespace IRacingLeague.Models;

public class Race
{
    public int RaceId { get; set; }
    public int LeagueId { get; set; }   // FK -> League
    public string Track { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int LapCount { get; set; }
    public bool IsCompleted { get; set; }
    public decimal AmbientTempC { get; set; }
    public int Round { get; set; }

    // Parameterless ctor kept for JSON (de)serialization in a later step.
    public Race() { }

    public Race(int leagueId, string track, string car, DateTime scheduledAt, int lapCount, decimal ambientTempC, int round)
    {
        LeagueId = leagueId;
        Track = track;
        Car = car;
        ScheduledAt = scheduledAt;
        LapCount = lapCount;
        AmbientTempC = ambientTempC;
        Round = round;
        IsCompleted = false;
    }

    public override string ToString() =>
        $"[{RaceId}] R{Round} {Track} ({Car}) — {ScheduledAt:yyyy-MM-dd HH:mm}, " +
        $"{LapCount} laps, {AmbientTempC:0.#}C{(IsCompleted ? " (completed)" : "")}";
}
