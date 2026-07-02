namespace MediatorPattern;

public enum ThermostatMode { Off, Cool, Heat }

public sealed class Thermostat : ISmartDevice
{
    private readonly ISmartHomeHub _hub;

    public string Name { get; }
    public ThermostatMode Mode { get; private set; } = ThermostatMode.Off;

    public Thermostat(string name, ISmartHomeHub hub)
    {
        Name = name;
        _hub = hub;
    }

    public void SetMode(ThermostatMode mode)
    {
        Mode = mode;
        Console.WriteLine($"  [{Name}] Mode set to {mode}.");
    }

    public void ReportTemperature(double celsius)
    {
        Console.WriteLine($"  [{Name}] Temperature reading: {celsius:F1}°C");
        var eventType = celsius > 26.0 ? Events.TemperatureHigh
                      : celsius < 18.0 ? Events.TemperatureLow
                                       : Events.TemperatureOk;
        _hub.Send(eventType, Name, $"{celsius:F1}°C");
    }
}
