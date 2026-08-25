namespace EntityPattern.Domain;

public sealed class Doctor : Entity<int>
{
    public string LicenceNumber  { get; }
    public string FullName       { get; private set; }
    public string Specialization { get; private set; }

    public Doctor(int id, string licenceNumber, string fullName, string specialization)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licenceNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(specialization);
        LicenceNumber  = licenceNumber;
        FullName       = fullName;
        Specialization = specialization;
    }

    public void UpdateSpecialization(string specialization)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specialization);
        Specialization = specialization;
    }
}
