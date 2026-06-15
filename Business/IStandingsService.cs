using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IStandingsService
{
    IEnumerable<StandingEntry> GetStandings(int leagueId);
}
