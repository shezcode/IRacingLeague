using IRacingLeague.Models;

namespace IRacingLeague.Data;

public interface ILeagueRepository
{
    void Add(League league);
    League? Get(int id);
    IEnumerable<League> GetAll();
    void Update(League league);
    void Delete(int id);
    void SaveChanges();
}
