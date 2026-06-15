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

    public IEnumerable<StandingEntry> GetStandings(int leagueId)
    {
        // Incident points and race counts live on Result, so accumulate them per
        // registration to drive the tiebreak and race count. Precompute once to
        // avoid an inner scan per driver.
        var resultsByRegistration = _results.GetAll()
            .GroupBy(r => r.RegistrationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return _registrations.GetAll()
            .Where(r => r.LeagueId == leagueId)
            .OrderByDescending(r => r.Points)
            .ThenBy(r => resultsByRegistration.TryGetValue(r.RegistrationId, out var results)
                ? results.Sum(res => res.IncidentPoints) : 0)
            .Select(r => new StandingEntry(r,
                resultsByRegistration.TryGetValue(r.RegistrationId, out var results) ? results.Count : 0))
            .ToList();
    }
}
