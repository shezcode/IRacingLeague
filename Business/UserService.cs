using IRacingLeague.Data;
using IRacingLeague.Models;

namespace IRacingLeague.Business;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public User Create(string userName, string email, string password, string role, string tag, string licenseClass)
    {
        var user = new User(userName, email, password, role, tag, licenseClass);
        _repository.Add(user);
        _repository.SaveChanges();
        return user;
    }

    public IEnumerable<User> GetAll() => _repository.GetAll();

    // Single field search: drivers whose Tag contains the term (case-insensitive).
    public IEnumerable<User> SearchByTag(string tag) =>
        _repository.GetAll()
            .Where(u => u.Tag.Contains(tag, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public User GetById(int id)
    {
        var user = _repository.Get(id);
        if (user == null)
            throw new KeyNotFoundException($"User with id {id} not found.");
        return user;
    }

    public void Update(User user)
    {
        if (_repository.Get(user.UserId) == null)
            throw new KeyNotFoundException($"User with id {user.UserId} not found.");
        _repository.Update(user);
        _repository.SaveChanges();
    }

    public void Delete(int id)
    {
        if (_repository.Get(id) == null)
            throw new KeyNotFoundException($"User with id {id} not found.");
        _repository.Delete(id);
        _repository.SaveChanges();
    }
}
