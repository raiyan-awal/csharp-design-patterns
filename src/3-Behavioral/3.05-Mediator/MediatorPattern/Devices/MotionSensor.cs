namespace MediatorPattern;

public sealed class MotionSensor : ISmartDevice
{
    private readonly ISmartHomeHub _hub;

    public string Name { get; }
    public bool IsMotionActive { get; private set; }

    public MotionSensor(string name, ISmartHomeHub hub)
    {
        Name = name;
        _hub = hub;
    }

    public void DetectMotion()
    {
        IsMotionActive = true;
        Console.WriteLine($"  [{Name}] Motion detected!");
        _hub.Send(Events.MotionDetected, Name);
    }

    public void ClearMotion()
    {
        IsMotionActive = false;
        Console.WriteLine($"  [{Name}] Motion cleared.");
        _hub.Send(Events.MotionCleared, Name);
    }
}
