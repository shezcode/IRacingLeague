using IRacingLeague.Models;

namespace IRacingLeague.Data;

public interface IUserRepository
{
    void Add(User user);
    User? Get(int id);
    IEnumerable<User> GetAll();
    void Update(User user);
    void Delete(int id);
    void SaveChanges();
}
