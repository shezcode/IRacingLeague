using IRacingLeague.Models;

namespace IRacingLeague.Business;

public interface IRegistrationService
{
    Registration Register(int userId, int leagueId, int carNumber, string teamName, decimal ballastKg);
    IEnumerable<Registration> GetByLeague(int leagueId);
    // Private zone: the leagues a given driver has joined (their own memberships).
    IEnumerable<Registration> GetByUser(int userId);
    Registration GetById(int id);
}
