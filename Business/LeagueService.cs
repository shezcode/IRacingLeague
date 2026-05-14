using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class LeagueService : ILeagueService
{
    private readonly ILeagueRepository _repository;

    public LeagueService(ILeagueRepository repository)
    {
        _repository = repository;
    }

    public League Create(string name, string discipline, bool isPublic, int maxDrivers, decimal entryFee, int ownerUserId = 0)
    {
        var league = new League(name, discipline, isPublic, maxDrivers, entryFee, ownerUserId);
        _repository.Add(league);
        _repository.SaveChanges();
        return league;
    }

    public IEnumerable<League> GetAll() => _repository.GetAll();

    // Visibility split, the public zone only
    // ever sees IsPublic leagues; private leagues surface solely to their owner/members.
    public IEnumerable<League> GetPublic() =>
        _repository.GetAll().Where(l => l.IsPublic).ToList();

    public IEnumerable<League> GetOwnedBy(int ownerUserId) =>
        _repository.GetAll().Where(l => l.OwnerUserId == ownerUserId).ToList();

    // Single field search: leagues whose Name contains the term (case-insensitive).
    // Filtering happens here in the service layer, reusing the existing repository.
    public IEnumerable<League> SearchByName(string name) =>
        _repository.GetAll()
            .Where(l => l.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public League GetById(int id)
    {
        var league = _repository.Get(id);
        if (league == null)
            throw new KeyNotFoundException($"League with id {id} not found.");
        return league;
    }

    public void Update(League league)
    {
        if (_repository.Get(league.LeagueId) == null)
            throw new KeyNotFoundException($"League with id {league.LeagueId} not found.");
        _repository.Update(league);
        _repository.SaveChanges();
    }

    public void Delete(int id)
    {
        if (_repository.Get(id) == null)
            throw new KeyNotFoundException($"League with id {id} not found.");
        _repository.Delete(id);
        _repository.SaveChanges();
    }
}
