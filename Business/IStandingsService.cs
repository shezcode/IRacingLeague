using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IStandingsService
{
    IEnumerable<Registration> GetStandings(int leagueId);
}
