using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class JsonUserRepository : JsonRepository<User>, IUserRepository
{
    public JsonUserRepository(string dataDirectory) : base(dataDirectory, "users.json") { }

    protected override int GetId(User item) => item.UserId;
    protected override void SetId(User item, int id) => item.UserId = id;

    public void Add(User user) => AddItem(user);
}
