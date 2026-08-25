namespace EntityPattern.Repositories;

using EntityPattern.Domain;

public interface IPatientRepository
{
    Patient?               FindById(int id);
    Patient?               FindByHealthCard(string healthCardNumber);
    IReadOnlyList<Patient> FindAll();
    void                   Save(Patient patient);
}
