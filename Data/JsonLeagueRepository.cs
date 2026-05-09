using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class JsonLeagueRepository : JsonRepository<League>, ILeagueRepository
{
    public JsonLeagueRepository(string dataDirectory) : base(dataDirectory, "leagues.json") { }

    protected override int GetId(League item) => item.LeagueId;
    protected override void SetId(League item, int id) => item.LeagueId = id;

    public void Add(League league) => AddItem(league);
}
