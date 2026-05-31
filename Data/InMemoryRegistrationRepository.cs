using IRacingLeague.Models;

namespace IRacingLeague.Data;

public class InMemoryRegistrationRepository : IRegistrationRepository
{
    private readonly List<Registration> _registrations = new();
    private int _nextId = 1;

    public void Add(Registration registration)
    {
        registration.RegistrationId = _nextId++;
        _registrations.Add(registration);
    }

    public Registration? Get(int id) => _registrations.FirstOrDefault(r => r.RegistrationId == id);

    public IEnumerable<Registration> GetAll() => _registrations.ToList();

    public void Update(Registration registration)
    {
        var index = _registrations.FindIndex(r => r.RegistrationId == registration.RegistrationId);
        if (index >= 0)
            _registrations[index] = registration;
    }

    public void Delete(int id) => _registrations.RemoveAll(r => r.RegistrationId == id);

    public void SaveChanges() { }
}
