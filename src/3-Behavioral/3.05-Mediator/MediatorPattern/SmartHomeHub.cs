namespace MediatorPattern;

// The Mediator. All coordination logic lives here — devices know nothing about each other.
public sealed class SmartHomeHub : ISmartHomeHub
{
    private readonly List<ISmartDevice> _devices = [];
    private readonly List<string> _eventLog = [];

    public IReadOnlyList<string> EventLog => _eventLog;

    public void Register(ISmartDevice device) => _devices.Add(device);

    public void Send(string eventType, string source, string? payload = null)
    {
        var entry = $"[{eventType}] from {source}" + (payload is not null ? $" | {payload}" : "");
        _eventLog.Add(entry);
        Console.WriteLine($"  [HUB] {entry}");

        switch (eventType)
        {
            case Events.MotionDetected:
                foreach (var d in Devices<SmartLight>())    d.TurnOn();
                foreach (var d in Devices<SecurityCamera>()) d.StartRecording(source);
                break;

            case Events.MotionCleared:
                foreach (var d in Devices<SmartLight>())    d.TurnOff();
                foreach (var d in Devices<SecurityCamera>()) d.StopRecording();
                break;

            case Events.TemperatureHigh:
                foreach (var d in Devices<Thermostat>()) d.SetMode(ThermostatMode.Cool);
                break;

            case Events.TemperatureLow:
                foreach (var d in Devices<Thermostat>()) d.SetMode(ThermostatMode.Heat);
                break;

            case Events.TemperatureOk:
                foreach (var d in Devices<Thermostat>()) d.SetMode(ThermostatMode.Off);
                break;

            case Events.AlarmTriggered:
                foreach (var d in Devices<SmartLight>())    d.Flash();
                foreach (var d in Devices<SecurityCamera>()) d.StartRecording("alarm");
                foreach (var d in Devices<DoorLock>())      d.Lock();
                break;

            case Events.AlarmCleared:
                foreach (var d in Devices<SmartLight>())    d.TurnOff();
                foreach (var d in Devices<SecurityCamera>()) d.StopRecording();
                foreach (var d in Devices<DoorLock>())      d.Unlock();
                break;

            case Events.DoorOpened:
                foreach (var d in Devices<SecurityCamera>()) d.StartRecording(source);
                break;
        }
    }

    private IEnumerable<T> Devices<T>() where T : class, ISmartDevice => _devices.OfType<T>();
}
