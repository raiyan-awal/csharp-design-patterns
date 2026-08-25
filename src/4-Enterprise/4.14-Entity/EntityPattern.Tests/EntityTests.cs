using EntityPattern.Domain;
using EntityPattern.Repositories;

namespace EntityPattern.Tests;

// ── Entity equality ────────────────────────────────────────────────────────

public class EntityEqualityTests
{
    private static Patient MakePatient(int id, string name = "Test Patient") =>
        new(id, $"CARD-{id:D4}", name, new DateOnly(1990, 1, 1), "1 Test St, Toronto, ON");

    private static Doctor MakeDoctor(int id) =>
        new(id, $"ON-MD-{id:D4}", "Dr. Test", "General");

    [Fact]
    public void SameId_SameType_AreEqual()
    {
        var a = MakePatient(1);
        var b = MakePatient(1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentId_SameType_AreNotEqual()
    {
        var a = MakePatient(1);
        var b = MakePatient(2);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SameAttributesDifferentId_AreNotEqual()
    {
        var a = MakePatient(1, "Sophie Tremblay");
        var b = MakePatient(2, "Sophie Tremblay");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SameId_DifferentType_AreNotEqual()
    {
        // Patient(1) and Doctor(1) share an ID but are different entity types
        var patient = MakePatient(1);
        var doctor  = MakeDoctor(1);
        Assert.False(patient.Equals(doctor));
    }

    [Fact]
    public void EqualsOperator_SameId_ReturnsTrue()
    {
        var a = MakePatient(5);
        var b = MakePatient(5);
        Assert.True(a == b);
    }

    [Fact]
    public void NotEqualsOperator_DifferentId_ReturnsTrue()
    {
        var a = MakePatient(1);
        var b = MakePatient(2);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameId_SameType_AreEqual()
    {
        var a = MakePatient(7);
        var b = MakePatient(7);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameId_DifferentType_AreDifferent()
    {
        var patient = MakePatient(1);
        var doctor  = MakeDoctor(1);
        Assert.NotEqual(patient.GetHashCode(), doctor.GetHashCode());
    }

    [Fact]
    public void ReferenceEqual_ReturnsTrueWithoutIdCheck()
    {
        var a = MakePatient(1);
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void NullComparison_ReturnsFalse()
    {
        var a = MakePatient(1);
        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
    }
}

// ── Patient ───────────────────────────────────────────────────────────────

public class PatientTests
{
    private static Patient Make(int id = 1) =>
        new(id, "TREM-1234-AB", "Marie Tremblay",
            new DateOnly(1985, 6, 20), "55 King St W, Toronto, ON");

    [Fact]
    public void Construction_SetsAllProperties()
    {
        var p = Make();
        Assert.Equal(1, p.Id);
        Assert.Equal("TREM-1234-AB", p.HealthCardNumber);
        Assert.Equal("Marie Tremblay", p.FullName);
        Assert.Equal(new DateOnly(1985, 6, 20), p.DateOfBirth);
        Assert.Equal("55 King St W, Toronto, ON", p.Address);
    }

    [Fact]
    public void UpdateName_ChangesFullName()
    {
        var p = Make();
        p.UpdateName("Marie Bergeron-Tremblay");
        Assert.Equal("Marie Bergeron-Tremblay", p.FullName);
    }

    [Fact]
    public void UpdateAddress_ChangesAddress()
    {
        var p = Make();
        p.UpdateAddress("100 Queen St E, Toronto, ON");
        Assert.Equal("100 Queen St E, Toronto, ON", p.Address);
    }

    [Fact]
    public void UpdateName_PreservesIdentity()
    {
        var p = Make(42);
        p.UpdateName("New Name");
        Assert.Equal(42, p.Id);
    }

    [Fact]
    public void UpdateName_WhiteSpace_Throws()
    {
        var p = Make();
        Assert.Throws<ArgumentException>(() => p.UpdateName("   "));
    }

    [Fact]
    public void UpdateAddress_Empty_Throws()
    {
        var p = Make();
        Assert.Throws<ArgumentException>(() => p.UpdateAddress(""));
    }

    [Fact]
    public void Construction_EmptyHealthCard_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Patient(1, "", "Name", new DateOnly(2000, 1, 1), "Addr"));
    }
}

// ── Doctor ───────────────────────────────────────────────────────────────

public class DoctorTests
{
    private static Doctor Make(int id = 1, string specialization = "Family Medicine") =>
        new(id, "ON-MD-12345", "Dr. Amara Okonkwo", specialization);

    [Fact]
    public void Construction_SetsAllProperties()
    {
        var d = Make();
        Assert.Equal(1, d.Id);
        Assert.Equal("ON-MD-12345", d.LicenceNumber);
        Assert.Equal("Dr. Amara Okonkwo", d.FullName);
        Assert.Equal("Family Medicine", d.Specialization);
    }

    [Fact]
    public void UpdateSpecialization_ChangesSpecialization()
    {
        var d = Make();
        d.UpdateSpecialization("Cardiology");
        Assert.Equal("Cardiology", d.Specialization);
    }

    [Fact]
    public void UpdateSpecialization_PreservesIdentity()
    {
        var d = Make(99);
        d.UpdateSpecialization("Oncology");
        Assert.Equal(99, d.Id);
    }

    [Fact]
    public void UpdateSpecialization_Empty_Throws()
    {
        var d = Make();
        Assert.Throws<ArgumentException>(() => d.UpdateSpecialization(""));
    }
}

// ── Appointment ──────────────────────────────────────────────────────────

public class AppointmentTests
{
    private static Appointment Make(int id = 1, int patientId = 10, int doctorId = 20) =>
        new(id, patientId, doctorId, DateTime.Today.AddDays(7), "Annual checkup");

    [Fact]
    public void Construction_SetsPropertiesAndScheduledStatus()
    {
        var a = Make(1, 10, 20);
        Assert.Equal(1, a.Id);
        Assert.Equal(10, a.PatientId);
        Assert.Equal(20, a.DoctorId);
        Assert.Equal("Annual checkup", a.Reason);
        Assert.Equal(AppointmentStatus.Scheduled, a.Status);
        Assert.Null(a.Notes);
    }

    [Fact]
    public void Construction_StoresReferencesByIdNotByObject()
    {
        // PatientId and DoctorId are plain ints — no object references held
        var a = Make(1, patientId: 42, doctorId: 99);
        Assert.Equal(42, a.PatientId);
        Assert.Equal(99, a.DoctorId);
    }

    [Fact]
    public void Complete_SetsStatusAndNotes()
    {
        var a = Make();
        a.Complete("BP normal. Follow-up in 6 months.");
        Assert.Equal(AppointmentStatus.Completed, a.Status);
        Assert.Equal("BP normal. Follow-up in 6 months.", a.Notes);
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var a = Make();
        a.Cancel();
        Assert.Equal(AppointmentStatus.Cancelled, a.Status);
    }

    [Fact]
    public void Complete_AlreadyCompleted_Throws()
    {
        var a = Make();
        a.Complete("First notes.");
        Assert.Throws<InvalidOperationException>(() => a.Complete("Second notes."));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var a = Make();
        a.Cancel();
        Assert.Throws<InvalidOperationException>(() => a.Cancel());
    }

    [Fact]
    public void Complete_AfterCancel_Throws()
    {
        var a = Make();
        a.Cancel();
        Assert.Throws<InvalidOperationException>(() => a.Complete("Notes."));
    }

    [Fact]
    public void Cancel_AfterComplete_Throws()
    {
        var a = Make();
        a.Complete("Notes.");
        Assert.Throws<InvalidOperationException>(() => a.Cancel());
    }

    [Fact]
    public void Construction_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Appointment(1, 1, 1, DateTime.Today.AddDays(1), ""));
    }
}

// ── Repositories ─────────────────────────────────────────────────────────

public class RepositoryTests
{
    [Fact]
    public void PatientRepository_SaveAndFindById_ReturnsSameEntity()
    {
        var repo    = new InMemoryPatientRepository();
        var patient = new Patient(1, "CARD-0001", "Jean-Luc Picard",
                                  new DateOnly(1970, 7, 13), "1 Enterprise Way, Ottawa, ON");
        repo.Save(patient);
        var found = repo.FindById(1);
        Assert.NotNull(found);
        Assert.Equal(patient, found);
    }

    [Fact]
    public void PatientRepository_FindByHealthCard_ReturnsCorrectPatient()
    {
        var repo = new InMemoryPatientRepository();
        repo.Save(new Patient(1, "CARD-AAA1", "Alice",   new DateOnly(1990, 1, 1), "Addr"));
        repo.Save(new Patient(2, "CARD-BBB2", "Bob",     new DateOnly(1985, 5, 5), "Addr"));
        var found = repo.FindByHealthCard("CARD-BBB2");
        Assert.NotNull(found);
        Assert.Equal(2, found.Id);
        Assert.Equal("Bob", found.FullName);
    }

    [Fact]
    public void PatientRepository_FindById_NonExistent_ReturnsNull()
    {
        var repo = new InMemoryPatientRepository();
        Assert.Null(repo.FindById(99));
    }

    [Fact]
    public void DoctorRepository_FindBySpecialization_ReturnsMatchingDoctors()
    {
        var repo = new InMemoryDoctorRepository();
        repo.Save(new Doctor(1, "LIC-001", "Dr. A", "Family Medicine"));
        repo.Save(new Doctor(2, "LIC-002", "Dr. B", "Cardiology"));
        repo.Save(new Doctor(3, "LIC-003", "Dr. C", "Family Medicine"));
        var results = repo.FindBySpecialization("Family Medicine");
        Assert.Equal(2, results.Count);
        Assert.All(results, d => Assert.Equal("Family Medicine", d.Specialization));
    }

    [Fact]
    public void AppointmentRepository_FindByPatient_ReturnsCorrectAppointments()
    {
        var repo = new InMemoryAppointmentRepository();
        repo.Save(new Appointment(1, patientId: 10, doctorId: 20, DateTime.Today.AddDays(1), "Checkup"));
        repo.Save(new Appointment(2, patientId: 10, doctorId: 21, DateTime.Today.AddDays(2), "Follow-up"));
        repo.Save(new Appointment(3, patientId: 99, doctorId: 20, DateTime.Today.AddDays(3), "Other"));
        var results = repo.FindByPatient(10);
        Assert.Equal(2, results.Count);
        Assert.All(results, a => Assert.Equal(10, a.PatientId));
    }

    [Fact]
    public void AppointmentRepository_FindByDoctor_ExcludesOtherDoctors()
    {
        var repo = new InMemoryAppointmentRepository();
        repo.Save(new Appointment(1, 10, doctorId: 5, DateTime.Today.AddDays(1), "Checkup"));
        repo.Save(new Appointment(2, 11, doctorId: 6, DateTime.Today.AddDays(2), "Checkup"));
        var results = repo.FindByDoctor(5);
        Assert.Single(results);
        Assert.Equal(5, results[0].DoctorId);
    }

    [Fact]
    public void PatientRepository_Save_OverwritesExistingEntry()
    {
        var repo    = new InMemoryPatientRepository();
        var patient = new Patient(1, "CARD-0001", "Old Name", new DateOnly(1980, 1, 1), "Old Addr");
        repo.Save(patient);
        patient.UpdateName("New Name");
        repo.Save(patient);
        var found = repo.FindById(1);
        Assert.Equal("New Name", found!.FullName);
    }
}
