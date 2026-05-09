using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class InMemoryRaceRepository : IRaceRepository
{
    private readonly List<Race> _races = new();
    private int _nextId = 1;

    public void Add(Race race)
    {
        race.RaceId = _nextId++;
        _races.Add(race);
    }

    public Race? Get(int id) => _races.FirstOrDefault(r => r.RaceId == id);

    public IEnumerable<Race> GetAll() => _races.ToList();

    public void Update(Race race)
    {
        var index = _races.FindIndex(r => r.RaceId == race.RaceId);
        if (index >= 0)
            _races[index] = race;
    }

    public void Delete(int id) => _races.RemoveAll(r => r.RaceId == id);

    public void SaveChanges() { }
}
