using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationRepository _registrations;
    private readonly ILeagueRepository _leagues;

    public RegistrationService(IRegistrationRepository registrations, ILeagueRepository leagues)
    {
        _registrations = registrations;
        _leagues = leagues;
    }

    public Registration Register(int userId, int leagueId, int carNumber, string teamName, decimal ballastKg)
    {
        var league = _leagues.Get(leagueId);
        if (league == null)
            throw new KeyNotFoundException($"League with id {leagueId} not found.");

        var members = _registrations.GetAll().Where(r => r.LeagueId == leagueId).ToList();

        if (members.Any(r => r.UserId == userId))
            throw new DuplicateRegistrationException(
                $"Driver #{userId} is already registered in league '{league.Name}'.");

        if (members.Count >= league.MaxDrivers)
            throw new LeagueFullException(
                $"League '{league.Name}' is full ({league.MaxDrivers} drivers).");

        var registration = new Registration(userId, leagueId, carNumber, teamName, ballastKg);
        _registrations.Add(registration);
        _registrations.SaveChanges();
        return registration;
    }

    public IEnumerable<Registration> GetByLeague(int leagueId) =>
        _registrations.GetAll().Where(r => r.LeagueId == leagueId).ToList();

    public IEnumerable<Registration> GetByUser(int userId) =>
        _registrations.GetAll().Where(r => r.UserId == userId).ToList();

    public Registration GetById(int id)
    {
        var registration = _registrations.Get(id);
        if (registration == null)
            throw new KeyNotFoundException($"Registration with id {id} not found.");
        return registration;
    }
}
