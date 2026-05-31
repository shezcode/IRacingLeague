using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class InMemoryResultRepository : IResultRepository
{
    private readonly List<Result> _results = new();
    private int _nextId = 1;

    public void Add(Result result)
    {
        result.ResultId = _nextId++;
        _results.Add(result);
    }

    public Result? Get(int id) => _results.FirstOrDefault(r => r.ResultId == id);

    public IEnumerable<Result> GetAll() => _results.ToList();

    public void Update(Result result)
    {
        var index = _results.FindIndex(r => r.ResultId == result.ResultId);
        if (index >= 0)
            _results[index] = result;
    }

    public void Delete(int id) => _results.RemoveAll(r => r.ResultId == id);

    public void SaveChanges() { }
}
