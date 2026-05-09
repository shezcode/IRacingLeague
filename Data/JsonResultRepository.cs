using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class JsonResultRepository : JsonRepository<Result>, IResultRepository
{
    public JsonResultRepository(string dataDirectory) : base(dataDirectory, "results.json") { }

    protected override int GetId(Result item) => item.ResultId;
    protected override void SetId(Result item, int id) => item.ResultId = id;

    public void Add(Result result) => AddItem(result);
}
