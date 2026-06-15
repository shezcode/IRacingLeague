using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class RaceService : IRaceService
{
    private readonly IRaceRepository _races;
    private readonly ILeagueRepository _leagues;

    public RaceService(IRaceRepository races, ILeagueRepository leagues)
    {
        _races = races;
        _leagues = leagues;
    }

    public Race Create(int leagueId, string track, string car, DateTime scheduledAt, int lapCount, decimal ambientTempC)
    {
        if (_leagues.Get(leagueId) == null)
            throw new KeyNotFoundException($"League with id {leagueId} not found.");

        var race = new Race(leagueId, track, car, scheduledAt, lapCount, ambientTempC);
        _races.Add(race);
        RenumberRounds(leagueId);
        _races.SaveChanges();
        return race;
    }

    public IEnumerable<Race> GetByLeague(int leagueId) =>
        _races.GetAll().Where(r => r.LeagueId == leagueId).OrderBy(r => r.Round).ToList();

    public Race GetById(int id)
    {
        var race = _races.Get(id);
        if (race == null)
            throw new KeyNotFoundException($"Race with id {id} not found.");
        return race;
    }

    public void Update(Race race)
    {
        if (_races.Get(race.RaceId) == null)
            throw new KeyNotFoundException($"Race with id {race.RaceId} not found.");
        _races.Update(race);
        _races.SaveChanges();
    }

    public void Delete(int id)
    {
        var race = _races.Get(id);
        if (race == null)
            throw new KeyNotFoundException($"Race with id {id} not found.");
        _races.Delete(id);
        RenumberRounds(race.LeagueId);
        _races.SaveChanges();
    }

    // Rounds are derived from chronological order rather than entered manually,
    // so every add/delete re-walks the league's races and reassigns 1, 2, 3...
    // Ties on identical ScheduledAt break by RaceId for a stable order.
    private void RenumberRounds(int leagueId)
    {
        var races = _races.GetAll()
            .Where(r => r.LeagueId == leagueId)
            .OrderBy(r => r.ScheduledAt)
            .ThenBy(r => r.RaceId)
            .ToList();

        for (int i = 0; i < races.Count; i++)
        {
            int round = i + 1;
            if (races[i].Round != round)
            {
                races[i].Round = round;
                _races.Update(races[i]);
            }
        }
    }
}
