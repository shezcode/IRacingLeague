using IRacingLeague.Models;

namespace IRacingLeague.Data;

public interface IRegistrationRepository
{
    void Add(Registration registration);
    Registration? Get(int id);
    IEnumerable<Registration> GetAll();
    void Update(Registration registration);
    void Delete(int id);
    void SaveChanges();
}
