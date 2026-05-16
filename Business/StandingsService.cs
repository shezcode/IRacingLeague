using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class StandingsService : IStandingsService
{
    private readonly IRegistrationRepository _registrations;
    private readonly IResultRepository _results;

    public StandingsService(IRegistrationRepository registrations, IResultRepository results)
    {
        _registrations = registrations;
        _results = results;
    }

    public IEnumerable<Registration> GetStandings(int leagueId)
    {
        // Incident points live on Result, so accumulate them per registration to
        // drive the tiebreak. Precompute once to avoid an inner scan per driver.
        var incidentsByRegistration = _results.GetAll()
            .GroupBy(r => r.RegistrationId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.IncidentPoints));

        return _registrations.GetAll()
            .Where(r => r.LeagueId == leagueId)
            .OrderByDescending(r => r.Points)
            .ThenBy(r => incidentsByRegistration.TryGetValue(r.RegistrationId, out int inc) ? inc : 0)
            .ToList();
    }
}
