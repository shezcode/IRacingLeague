using IRacingLeague.Models;

namespace IRacingLeague.Data;

public interface IResultRepository
{
    void Add(Result result);
    Result? Get(int id);
    IEnumerable<Result> GetAll();
    void Update(Result result);
    void Delete(int id);
    void SaveChanges();
}
