using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class JsonRaceRepository : JsonRepository<Race>, IRaceRepository
{
    public JsonRaceRepository(string dataDirectory) : base(dataDirectory, "races.json") { }

    protected override int GetId(Race item) => item.RaceId;
    protected override void SetId(Race item, int id) => item.RaceId = id;

    public void Add(Race race) => AddItem(race);
}
