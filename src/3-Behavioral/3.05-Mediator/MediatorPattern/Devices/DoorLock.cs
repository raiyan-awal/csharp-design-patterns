namespace MediatorPattern;

public sealed class DoorLock : ISmartDevice
{
    private readonly ISmartHomeHub _hub;

    public string Name { get; }
    public bool IsLocked { get; private set; } = true;

    public DoorLock(string name, ISmartHomeHub hub)
    {
        Name = name;
        _hub = hub;
    }

    public void Lock()
    {
        IsLocked = true;
        Console.WriteLine($"  [{Name}] LOCKED.");
    }

    public void Unlock()
    {
        IsLocked = false;
        Console.WriteLine($"  [{Name}] Unlocked.");
    }

    public void Open()
    {
        if (IsLocked)
        {
            Console.WriteLine($"  [{Name}] Cannot open — door is locked.");
            return;
        }
        Console.WriteLine($"  [{Name}] Opened.");
        _hub.Send(Events.DoorOpened, Name);
    }

    public void Close()
    {
        Console.WriteLine($"  [{Name}] Closed.");
        _hub.Send(Events.DoorClosed, Name);
    }
}
