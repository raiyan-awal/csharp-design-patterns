namespace MediatorPattern;

public static class Events
{
    public const string MotionDetected  = "motion.detected";
    public const string MotionCleared   = "motion.cleared";
    public const string TemperatureHigh = "temperature.high";
    public const string TemperatureLow  = "temperature.low";
    public const string TemperatureOk   = "temperature.ok";
    public const string AlarmTriggered  = "alarm.triggered";
    public const string AlarmCleared    = "alarm.cleared";
    public const string DoorOpened      = "door.opened";
    public const string DoorClosed      = "door.closed";
}
