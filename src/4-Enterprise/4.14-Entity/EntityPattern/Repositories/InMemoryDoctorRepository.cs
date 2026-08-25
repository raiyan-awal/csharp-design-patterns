namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public sealed class InMemoryDoctorRepository : IDoctorRepository
{
    private readonly Dictionary<int, Doctor> _store = new();

    public Doctor? FindById(int id) =>
        _store.TryGetValue(id, out var d) ? d : null;

    public IReadOnlyList<Doctor> FindBySpecialization(string specialization) =>
        _store.Values
              .Where(d => d.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase))
              .ToList();

    public void Save(Doctor doctor) => _store[doctor.Id] = doctor;
}
