namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public interface IDoctorRepository
{
    Doctor?               FindById(int id);
    IReadOnlyList<Doctor> FindBySpecialization(string specialization);
    void                  Save(Doctor doctor);
}
