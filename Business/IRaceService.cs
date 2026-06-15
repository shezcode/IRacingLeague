using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IRaceService
{
    Race Create(int leagueId, string track, string car, DateTime scheduledAt, int lapCount, decimal ambientTempC);
    IEnumerable<Race> GetByLeague(int leagueId);
    Race GetById(int id);
    void Update(Race race);
    void Delete(int id);
}
