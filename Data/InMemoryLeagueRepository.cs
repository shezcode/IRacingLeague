using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class InMemoryLeagueRepository : ILeagueRepository
{
    private readonly List<League> _leagues = new();
    private int _nextId = 1;

    public void Add(League league)
    {
        league.LeagueId = _nextId++;
        _leagues.Add(league);
    }

    public League? Get(int id) => _leagues.FirstOrDefault(l => l.LeagueId == id);

    public IEnumerable<League> GetAll() => _leagues.ToList();

    public void Update(League league)
    {
        var index = _leagues.FindIndex(l => l.LeagueId == league.LeagueId);
        if (index >= 0)
            _leagues[index] = league;
    }

    public void Delete(int id) => _leagues.RemoveAll(l => l.LeagueId == id);

    // Nothing to flush for an in-memory store; the method exists to honour the
    // interface so services stay identical when JSON persistence arrives.
    public void SaveChanges() { }
}
