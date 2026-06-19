using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IResultService
{
    Result ApplyResult(Registration registration, Race race, Result result);
    Result GetById(int id);
    IEnumerable<Result> GetByRace(int raceId);
    // Deleting a result reverts the standings points and driver stats it applied.
    void Delete(int resultId);
    // Cascade helpers: drop every result tied to a race or a registration, each
    // reverting its effects. Used when a race, league, or driver is removed.
    void DeleteByRace(int raceId);
    void DeleteByRegistration(int registrationId);
}
