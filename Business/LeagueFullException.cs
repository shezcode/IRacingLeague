namespace IRacingLeague.Business;

public class LeagueFullException : Exception
{
    public LeagueFullException(string message) : base(message) { }
}
