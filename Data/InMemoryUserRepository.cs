using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public void Add(User user)
    {
        user.UserId = _nextId++;
        _users.Add(user);
    }

    public User? Get(int id) => _users.FirstOrDefault(u => u.UserId == id);

    public IEnumerable<User> GetAll() => _users.ToList();

    public void Update(User user)
    {
        var index = _users.FindIndex(u => u.UserId == user.UserId);
        if (index >= 0)
            _users[index] = user;
    }

    public void Delete(int id) => _users.RemoveAll(u => u.UserId == id);

    public void SaveChanges() { }
}
