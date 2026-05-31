namespace IRacingLeague.Models;

public class League
{
    public int LeagueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MaxDrivers { get; set; }
    public decimal EntryFee { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OwnerUserId { get; set; }

    // Parameterless ctor kept for JSON (de)serialization in a later step.
    public League() { }

    public League(string name, string discipline, bool isPublic, int maxDrivers, decimal entryFee, int ownerUserId)
    {
        Name = name;
        Discipline = discipline;
        IsPublic = isPublic;
        MaxDrivers = maxDrivers;
        EntryFee = entryFee;
        OwnerUserId = ownerUserId;
        CreatedAt = DateTime.Now;
    }

    public override string ToString() =>
        $"[{LeagueId}] {Name} ({Discipline}) — {(IsPublic ? "public" : "private")}, " +
        $"max {MaxDrivers}, fee {EntryFee:0.##}, owner #{OwnerUserId}";
}
