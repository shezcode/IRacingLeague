using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class JsonRegistrationRepository : JsonRepository<Registration>, IRegistrationRepository
{
    public JsonRegistrationRepository(string dataDirectory) : base(dataDirectory, "registrations.json") { }

    protected override int GetId(Registration item) => item.RegistrationId;
    protected override void SetId(Registration item, int id) => item.RegistrationId = id;

    public void Add(Registration registration) => AddItem(registration);
}
