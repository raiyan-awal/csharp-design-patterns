namespace EntityPattern.Domain;

public sealed class Appointment : Entity<int>
{
    public int               PatientId   { get; }
    public int               DoctorId    { get; }
    public DateTime          ScheduledAt { get; }
    public string            Reason      { get; }
    public AppointmentStatus Status      { get; private set; }
    public string?           Notes       { get; private set; }

    public Appointment(int id, int patientId, int doctorId, DateTime scheduledAt, string reason)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        PatientId   = patientId;
        DoctorId    = doctorId;
        ScheduledAt = scheduledAt;
        Reason      = reason;
        Status      = AppointmentStatus.Scheduled;
    }

    public void Complete(string notes)
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException(
                $"Cannot complete an appointment that is already {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);
        Status = AppointmentStatus.Completed;
        Notes  = notes;
    }

    public void Cancel()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException(
                $"Cannot cancel an appointment that is already {Status}.");
        Status = AppointmentStatus.Cancelled;
    }
}
