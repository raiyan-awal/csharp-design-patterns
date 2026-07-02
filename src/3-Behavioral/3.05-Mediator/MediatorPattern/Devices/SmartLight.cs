namespace MediatorPattern;

public enum LightState { Off, On, Flashing }

public sealed class SmartLight : ISmartDevice
{
    public string Name { get; }
    public LightState State { get; private set; } = LightState.Off;
    public bool IsOn => State != LightState.Off;

    public SmartLight(string name) => Name = name;

    public void TurnOn()
    {
        State = LightState.On;
        Console.WriteLine($"  [{Name}] Turned ON.");
    }

    public void TurnOff()
    {
        State = LightState.Off;
        Console.WriteLine($"  [{Name}] Turned OFF.");
    }

    public void Flash()
    {
        State = LightState.Flashing;
        Console.WriteLine($"  [{Name}] FLASHING — alarm mode.");
    }
}
