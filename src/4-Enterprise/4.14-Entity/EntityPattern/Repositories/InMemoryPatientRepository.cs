namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly Dictionary<int, Patient> _store = new();

    public Patient? FindById(int id) =>
        _store.TryGetValue(id, out var p) ? p : null;

    public Patient? FindByHealthCard(string healthCardNumber) =>
        _store.Values.FirstOrDefault(p => p.HealthCardNumber == healthCardNumber);

    public IReadOnlyList<Patient> FindAll() =>
        _store.Values.ToList();

    public void Save(Patient patient) => _store[patient.Id] = patient;
}
