using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface ILeagueService
{
    League Create(string name, string discipline, bool isPublic, int maxDrivers, decimal entryFee, int ownerUserId = 0);
    IEnumerable<League> GetAll();
    // Public zone: only leagues flagged public are visible to everyone.
    IEnumerable<League> GetPublic();
    // Private zone: leagues owned by a given driver (the league owner).
    IEnumerable<League> GetOwnedBy(int ownerUserId);
    IEnumerable<League> SearchByName(string name);
    League GetById(int id);
    void Update(League league);
    void Delete(int id);
}
