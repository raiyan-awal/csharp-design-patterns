namespace EntityPattern.Domain;

public sealed class Patient : Entity<int>
{
    public string  HealthCardNumber { get; }
    public string  FullName         { get; private set; }
    public DateOnly DateOfBirth     { get; }
    public string  Address          { get; private set; }

    public Patient(int id, string healthCardNumber, string fullName,
                   DateOnly dateOfBirth, string address)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(healthCardNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        HealthCardNumber = healthCardNumber;
        FullName         = fullName;
        DateOfBirth      = dateOfBirth;
        Address          = address;
    }

    public void UpdateName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        FullName = fullName;
    }

    public void UpdateAddress(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        Address = address;
    }
}
