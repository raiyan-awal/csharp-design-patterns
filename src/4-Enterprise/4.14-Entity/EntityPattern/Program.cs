using EntityPattern.Domain;
using EntityPattern.Repositories;

Console.WriteLine("=== Maple Street Medical Centre — Entity Pattern Demo ===\n");

var patients     = new InMemoryPatientRepository();
var doctors      = new InMemoryDoctorRepository();
var appointments = new InMemoryAppointmentRepository();

// ── Section 1: Identity vs Attributes ─────────────────────────────────────
Console.WriteLine("--- Identity vs Attributes ---");

var p1 = new Patient(1, "TREMA8901-AB", "Sophie Tremblay",
                     new DateOnly(1989, 3, 12), "42 Maple Street, Toronto, ON");
var p2 = new Patient(2, "TREMA8901-CD", "Sophie Tremblay",
                     new DateOnly(1989, 3, 12), "42 Maple Street, Toronto, ON");
var p3 = new Patient(1, "TREMA8901-AB", "Sophie Tremblay",
                     new DateOnly(1989, 3, 12), "42 Maple Street, Toronto, ON");

Console.WriteLine($"  Patient #{p1.Id}: {p1.FullName}");
Console.WriteLine($"  Patient #{p2.Id}: {p2.FullName}");
Console.WriteLine($"  p1 == p2 (same name, different ID):  {p1 == p2}");
Console.WriteLine($"  p1 == p3 (different object, same ID): {p1 == p3}");
Console.WriteLine($"  Patient(1).GetHashCode() == Patient(3).GetHashCode(): {p1.GetHashCode() == p3.GetHashCode()}");

var d1 = new Doctor(1, "ON-MD-88421", "Dr. Amara Okonkwo", "Family Medicine");
Console.WriteLine($"\n  Patient(Id=1) == Doctor(Id=1) (same ID, different type): {p1.Equals(d1)}");

Pause();

// ── Section 2: State Changes Preserve Identity ────────────────────────────
Console.WriteLine("--- State Changes Preserve Identity ---");

Console.WriteLine($"  Before update — Id: {p1.Id}, Name: {p1.FullName}");
p1.UpdateName("Sophie Bergeron-Tremblay");
p1.UpdateAddress("88 Queen St W, Toronto, ON");
Console.WriteLine($"  After  update — Id: {p1.Id}, Name: {p1.FullName}");
Console.WriteLine($"  Identity unchanged: p1.Id is still {p1.Id}");
Console.WriteLine($"  p1 == p3 (p3 still has old name, same Id): {p1 == p3}");

patients.Save(p1);
patients.Save(p2);
doctors.Save(d1);
doctors.Save(new Doctor(2, "ON-MD-77302", "Dr. Lena Kowalski", "Cardiology"));
doctors.Save(new Doctor(3, "ON-MD-99154", "Dr. James Osei", "Family Medicine"));

Pause();

// ── Section 3: Reference by ID ────────────────────────────────────────────
Console.WriteLine("--- Reference by ID ---");

Console.WriteLine("  Appointments reference Patient and Doctor by ID, not by object.");
Console.WriteLine("  This decouples the aggregate from its collaborators.\n");

var apt1 = new Appointment(1, patientId: 1, doctorId: 1,
                            DateTime.Today.AddDays(3).AddHours(9),
                            "Annual physical — blood pressure follow-up");
var apt2 = new Appointment(2, patientId: 2, doctorId: 3,
                            DateTime.Today.AddDays(5).AddHours(14),
                            "Persistent cough — possible respiratory infection");

appointments.Save(apt1);
appointments.Save(apt2);

foreach (var apt in new[] { apt1, apt2 })
{
    var patient = patients.FindById(apt.PatientId)!;
    var doctor  = doctors.FindById(apt.DoctorId)!;
    Console.WriteLine($"  Apt #{apt.Id} | {patient.FullName} → {doctor.FullName} ({doctor.Specialization})");
    Console.WriteLine($"           Reason: {apt.Reason}");
    Console.WriteLine($"           Status: {apt.Status}");
}

Pause();

// ── Section 4: Repository Round-Trip and Appointment Lifecycle ────────────
Console.WriteLine("--- Repository Round-Trip and Appointment Lifecycle ---");

apt1.Complete("BP: 122/78. Weight stable. Booked follow-up in 6 months.");
appointments.Save(apt1);

var retrieved = appointments.FindById(1)!;
Console.WriteLine($"  Apt #1 status after complete: {retrieved.Status}");
Console.WriteLine($"  Notes: {retrieved.Notes}");

apt2.Cancel();
appointments.Save(apt2);

Console.WriteLine($"\n  Apt #2 status after cancel: {appointments.FindById(2)!.Status}");

Console.WriteLine("\n  Dr. Okonkwo's appointments:");
foreach (var apt in appointments.FindByDoctor(1))
    Console.WriteLine($"    #{apt.Id} — {apt.Status}");

Console.WriteLine("\n  All Family Medicine doctors:");
foreach (var doc in doctors.FindBySpecialization("Family Medicine"))
    Console.WriteLine($"    #{doc.Id} — {doc.FullName}");

Console.WriteLine("\n=== Demo complete ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
