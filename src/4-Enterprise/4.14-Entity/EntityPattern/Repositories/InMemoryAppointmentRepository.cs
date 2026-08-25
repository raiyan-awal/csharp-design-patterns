namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly Dictionary<int, Appointment> _store = new();

    public Appointment? FindById(int id) =>
        _store.TryGetValue(id, out var a) ? a : null;

    public IReadOnlyList<Appointment> FindByPatient(int patientId) =>
        _store.Values.Where(a => a.PatientId == patientId).ToList();

    public IReadOnlyList<Appointment> FindByDoctor(int doctorId) =>
        _store.Values.Where(a => a.DoctorId == doctorId).ToList();

    public void Save(Appointment appointment) => _store[appointment.Id] = appointment;
}
