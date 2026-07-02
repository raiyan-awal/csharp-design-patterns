namespace MediatorPattern;

public sealed class SecurityCamera : ISmartDevice
{
    public string Name { get; }
    public bool IsRecording { get; private set; }
    public string? RecordingSource { get; private set; }

    public SecurityCamera(string name) => Name = name;

    public void StartRecording(string trigger)
    {
        IsRecording = true;
        RecordingSource = trigger;
        Console.WriteLine($"  [{Name}] Recording started (trigger: {trigger}).");
    }

    public void StopRecording()
    {
        IsRecording = false;
        RecordingSource = null;
        Console.WriteLine($"  [{Name}] Recording stopped.");
    }
}
