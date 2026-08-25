namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public interface IAppointmentRepository
{
    Appointment?               FindById(int id);
    IReadOnlyList<Appointment> FindByPatient(int patientId);
    IReadOnlyList<Appointment> FindByDoctor(int doctorId);
    void                       Save(Appointment appointment);
}
