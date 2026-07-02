using MediatorPattern;

public class MediatorPatternTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SmartHomeHub BuildHub(
        out SmartLight light,
        out SecurityCamera camera,
        out Thermostat thermostat,
        out DoorLock doorLock)
    {
        var hub = new SmartHomeHub();
        light      = new SmartLight("Light");
        camera     = new SecurityCamera("Camera");
        thermostat = new Thermostat("Thermostat", hub);
        doorLock   = new DoorLock("Door", hub);

        hub.Register(light);
        hub.Register(camera);
        hub.Register(thermostat);
        hub.Register(doorLock);
        return hub;
    }

    // ── Motion ────────────────────────────────────────────────────────────────

    [Fact]
    public void MotionDetected_TurnsOnAllLightsAndStartsCamera()
    {
        var hub = BuildHub(out var light, out var camera, out _, out _);

        hub.Send(Events.MotionDetected, "sensor");

        Assert.Equal(LightState.On, light.State);
        Assert.True(camera.IsRecording);
        Assert.Equal("sensor", camera.RecordingSource);
    }

    [Fact]
    public void MotionCleared_TurnsOffLightsAndStopsCamera()
    {
        var hub = BuildHub(out var light, out var camera, out _, out _);
        hub.Send(Events.MotionDetected, "sensor");

        hub.Send(Events.MotionCleared, "sensor");

        Assert.Equal(LightState.Off, light.State);
        Assert.False(camera.IsRecording);
        Assert.Null(camera.RecordingSource);
    }

    [Fact]
    public void MultipleSmartLights_AllTurnOnWhenMotionDetected()
    {
        var hub    = new SmartHomeHub();
        var light1 = new SmartLight("L1");
        var light2 = new SmartLight("L2");
        hub.Register(light1);
        hub.Register(light2);

        hub.Send(Events.MotionDetected, "sensor");

        Assert.Equal(LightState.On, light1.State);
        Assert.Equal(LightState.On, light2.State);
    }

    [Fact]
    public void MotionSensor_DetectMotion_PublishesEventThroughHub()
    {
        var hub    = new SmartHomeHub();
        var motion = new MotionSensor("Motion", hub);
        var light  = new SmartLight("Light");
        hub.Register(motion);
        hub.Register(light);

        motion.DetectMotion();

        Assert.Equal(LightState.On, light.State);
        Assert.True(motion.IsMotionActive);
    }

    [Fact]
    public void MotionSensor_ClearMotion_PublishesEventThroughHub()
    {
        var hub    = new SmartHomeHub();
        var motion = new MotionSensor("Motion", hub);
        var light  = new SmartLight("Light");
        hub.Register(motion);
        hub.Register(light);
        motion.DetectMotion();

        motion.ClearMotion();

        Assert.Equal(LightState.Off, light.State);
        Assert.False(motion.IsMotionActive);
    }

    // ── Temperature ───────────────────────────────────────────────────────────

    [Fact]
    public void TemperatureHigh_SetsThermostatToCool()
    {
        var hub = BuildHub(out _, out _, out var thermostat, out _);

        hub.Send(Events.TemperatureHigh, "sensor");

        Assert.Equal(ThermostatMode.Cool, thermostat.Mode);
    }

    [Fact]
    public void TemperatureLow_SetsThermostatToHeat()
    {
        var hub = BuildHub(out _, out _, out var thermostat, out _);

        hub.Send(Events.TemperatureLow, "sensor");

        Assert.Equal(ThermostatMode.Heat, thermostat.Mode);
    }

    [Fact]
    public void TemperatureOk_SetsThermostatToOff()
    {
        var hub = BuildHub(out _, out _, out var thermostat, out _);
        hub.Send(Events.TemperatureHigh, "sensor");

        hub.Send(Events.TemperatureOk, "sensor");

        Assert.Equal(ThermostatMode.Off, thermostat.Mode);
    }

    [Fact]
    public void Thermostat_ReportTemperature_High_PublishesThermostatHighEvent()
    {
        var hub        = new SmartHomeHub();
        var thermostat = new Thermostat("T", hub);
        hub.Register(thermostat);

        thermostat.ReportTemperature(30.0);

        Assert.Equal(ThermostatMode.Cool, thermostat.Mode);
    }

    [Fact]
    public void Thermostat_ReportTemperature_Low_PublishesThermostatLowEvent()
    {
        var hub        = new SmartHomeHub();
        var thermostat = new Thermostat("T", hub);
        hub.Register(thermostat);

        thermostat.ReportTemperature(15.0);

        Assert.Equal(ThermostatMode.Heat, thermostat.Mode);
    }

    [Fact]
    public void Thermostat_ReportTemperature_Ok_PublishesThermostatOkEvent()
    {
        var hub        = new SmartHomeHub();
        var thermostat = new Thermostat("T", hub);
        hub.Register(thermostat);
        thermostat.ReportTemperature(30.0);

        thermostat.ReportTemperature(22.0);

        Assert.Equal(ThermostatMode.Off, thermostat.Mode);
    }

    // ── Alarm ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AlarmTriggered_FlashesLightsStartsCameraAndLocksAllDoors()
    {
        var hub = BuildHub(out var light, out var camera, out _, out var doorLock);
        doorLock.Unlock();

        hub.Send(Events.AlarmTriggered, "SecuritySystem");

        Assert.Equal(LightState.Flashing, light.State);
        Assert.True(camera.IsRecording);
        Assert.Equal("alarm", camera.RecordingSource);
        Assert.True(doorLock.IsLocked);
    }

    [Fact]
    public void AlarmCleared_TurnsOffLightsStopsCameraAndUnlocksAllDoors()
    {
        var hub = BuildHub(out var light, out var camera, out _, out var doorLock);
        hub.Send(Events.AlarmTriggered, "SecuritySystem");

        hub.Send(Events.AlarmCleared, "SecuritySystem");

        Assert.Equal(LightState.Off, light.State);
        Assert.False(camera.IsRecording);
        Assert.False(doorLock.IsLocked);
    }

    // ── Door ─────────────────────────────────────────────────────────────────

    [Fact]
    public void DoorOpened_StartsCamera()
    {
        var hub    = new SmartHomeHub();
        var camera = new SecurityCamera("Cam");
        hub.Register(camera);

        hub.Send(Events.DoorOpened, "FrontDoor");

        Assert.True(camera.IsRecording);
        Assert.Equal("FrontDoor", camera.RecordingSource);
    }

    [Fact]
    public void DoorLock_Open_WhenLocked_DoesNotPublishEvent()
    {
        var hub      = new SmartHomeHub();
        var camera   = new SecurityCamera("Cam");
        var doorLock = new DoorLock("Door", hub);
        hub.Register(camera);
        hub.Register(doorLock);

        doorLock.Open(); // locked by default — should be blocked

        Assert.False(camera.IsRecording);
        Assert.Empty(hub.EventLog);
    }

    [Fact]
    public void DoorLock_Open_WhenUnlocked_PublishesDoorOpenedEvent()
    {
        var hub      = new SmartHomeHub();
        var camera   = new SecurityCamera("Cam");
        var doorLock = new DoorLock("Door", hub);
        hub.Register(camera);
        hub.Register(doorLock);
        doorLock.Unlock();

        doorLock.Open();

        Assert.True(camera.IsRecording);
    }

    // ── Event log ─────────────────────────────────────────────────────────────

    [Fact]
    public void EventLog_RecordsEveryPublishedEvent()
    {
        var hub = BuildHub(out _, out _, out _, out _);

        hub.Send(Events.MotionDetected, "sensor");
        hub.Send(Events.MotionCleared, "sensor");
        hub.Send(Events.TemperatureHigh, "thermostat", "30.0°C");

        Assert.Equal(3, hub.EventLog.Count);
    }

    [Fact]
    public void EventLog_ContainsSourceAndPayload()
    {
        var hub = new SmartHomeHub();

        hub.Send(Events.TemperatureHigh, "Thermostat", "31.5°C");

        Assert.Contains("temperature.high", hub.EventLog[0]);
        Assert.Contains("Thermostat", hub.EventLog[0]);
        Assert.Contains("31.5°C", hub.EventLog[0]);
    }
}
