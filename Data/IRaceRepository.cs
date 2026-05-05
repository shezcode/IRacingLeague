using IRacingLeague.Models;

namespace IRacingLeague.Data;

public interface IRaceRepository
{
    void Add(Race race);
    Race? Get(int id);
    IEnumerable<Race> GetAll();
    void Update(Race race);
    void Delete(int id);
    void SaveChanges();
}
