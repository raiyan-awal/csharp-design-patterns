# 3.05 — Mediator Pattern

## Intent

Define an object (the **Mediator**) that encapsulates how a set of objects interact. Colleagues communicate only through the mediator — never directly with each other. This promotes loose coupling and centralises coordination logic.

---

## The Problem It Solves

When many objects need to collaborate, direct cross-references create a tangled web:

```
MotionSensor → SmartLight        (reference)
MotionSensor → SecurityCamera    (reference)
DoorLock     → SecurityCamera    (reference)
Thermostat   → SmartLight        (reference)
Alarm        → DoorLock          (reference)
Alarm        → SmartLight        (reference)
...
```

Every time you add a device you must update several existing classes. The system becomes fragile and hard to reason about.

**Mediator fixes this by making every device talk only to the hub:**

```
MotionSensor → Hub → SmartLight
                   → SecurityCamera

DoorLock     → Hub → SecurityCamera

Thermostat   → Hub → Thermostat (mode change)

Alarm        → Hub → SmartLight
                   → DoorLock
                   → SecurityCamera
```

Each device has **one dependency** — the hub. The hub holds all the coordination logic.

---

## Solution: Smart Home Hub

A smart home hub coordinates 5 device types. Devices publish events by calling `hub.Send()`; the hub decides which other devices to notify.

### Participants

| Role | Class | Description |
|------|-------|-------------|
| **Mediator interface** | `ISmartHomeHub` | `Register` + `Send` |
| **Concrete Mediator** | `SmartHomeHub` | Coordinates all devices; holds routing logic |
| **Colleague interface** | `ISmartDevice` | Common `Name` property |
| **Concrete Colleagues** | `MotionSensor`, `SmartLight`, `SecurityCamera`, `Thermostat`, `DoorLock` | Publish events through hub; react to routed events |

### Event routing table

| Event published | Hub notifies |
|-----------------|-------------|
| `motion.detected` | `SmartLight.TurnOn()` + `SecurityCamera.StartRecording()` |
| `motion.cleared` | `SmartLight.TurnOff()` + `SecurityCamera.StopRecording()` |
| `temperature.high` | `Thermostat.SetMode(Cool)` |
| `temperature.low` | `Thermostat.SetMode(Heat)` |
| `temperature.ok` | `Thermostat.SetMode(Off)` |
| `alarm.triggered` | `SmartLight.Flash()` + `SecurityCamera.StartRecording()` + `DoorLock.Lock()` |
| `alarm.cleared` | `SmartLight.TurnOff()` + `SecurityCamera.StopRecording()` + `DoorLock.Unlock()` |
| `door.opened` | `SecurityCamera.StartRecording()` |

---

## Structure

```
MediatorPattern/
├── ISmartHomeHub.cs          ← Mediator interface
├── ISmartDevice.cs           ← Colleague interface
├── SmartHomeHub.cs           ← Concrete Mediator (all routing logic here)
├── Events.cs                 ← String constants for event names
└── Devices/
    ├── MotionSensor.cs       ← Publishes motion.detected / motion.cleared
    ├── SmartLight.cs         ← Reacts: TurnOn / TurnOff / Flash
    ├── SecurityCamera.cs     ← Reacts: StartRecording / StopRecording
    ├── Thermostat.cs         ← Publishes temperature events; reacts with mode change
    └── DoorLock.cs           ← Publishes door.opened; reacts to alarm events
```

---

## Key Code

### Mediator interface

```csharp
public interface ISmartHomeHub
{
    void Register(ISmartDevice device);
    void Send(string eventType, string source, string? payload = null);
}
```

### Concrete Mediator — all coordination in one place

```csharp
public sealed class SmartHomeHub : ISmartHomeHub
{
    private readonly List<ISmartDevice> _devices = [];

    public void Register(ISmartDevice device) => _devices.Add(device);

    public void Send(string eventType, string source, string? payload = null)
    {
        switch (eventType)
        {
            case Events.MotionDetected:
                foreach (var d in Devices<SmartLight>())     d.TurnOn();
                foreach (var d in Devices<SecurityCamera>()) d.StartRecording(source);
                break;

            case Events.AlarmTriggered:
                foreach (var d in Devices<SmartLight>())     d.Flash();
                foreach (var d in Devices<SecurityCamera>()) d.StartRecording("alarm");
                foreach (var d in Devices<DoorLock>())       d.Lock();
                break;
            // ...
        }
    }

    private IEnumerable<T> Devices<T>() where T : class, ISmartDevice => _devices.OfType<T>();
}
```

### Colleague — knows only the hub, never other devices

```csharp
public sealed class MotionSensor : ISmartDevice
{
    private readonly ISmartHomeHub _hub;

    public MotionSensor(string name, ISmartHomeHub hub) { Name = name; _hub = hub; }
    public string Name { get; }

    public void DetectMotion() => _hub.Send(Events.MotionDetected, Name);
    public void ClearMotion()  => _hub.Send(Events.MotionCleared, Name);
}
```

---

## Demo Scenarios

```
DEMO 1 — Motion detected
  Hub routes: lights ON + camera starts recording

DEMO 2 — Motion cleared
  Hub routes: lights OFF + camera stops recording

DEMO 3 — Temperature changes (30.5°C → 22.0°C → 14.0°C)
  Hub routes: thermostat Cool → Off → Heat

DEMO 4 — Alarm triggered
  Hub routes: lights FLASH + camera records + door LOCKS

DEMO 5 — Alarm cleared
  Hub routes: lights OFF + camera stops + door UNLOCKS

DEMO 6 — Door interaction
  Locked door blocks open; unlocked door triggers camera

DEMO 7 — Full event log
  All hub-routed events printed in order
```

---

## When to Use

- A group of objects communicate in complex, well-defined but tangled ways
- Reusing objects is difficult because they reference too many others
- Behaviour distributed across classes should be customisable without subclassing

---

## When NOT to Use

- There are only 2–3 objects — direct references are simpler
- The coordination logic is trivial — the mediator just becomes a pass-through
- Each "device" has wildly different interaction requirements — consider multiple smaller mediators

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Single Responsibility** | Each device does one thing; all coordination is in the hub |
| **Open/Closed** | Add a new device without touching existing devices |
| **Reduced coupling** | Devices have one dependency (the hub) instead of N |
| **Centralised logic** | Coordination rules are visible and testable in one place |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **God object risk** | The mediator can grow into a bloated class that knows too much |
| **Single point of failure** | All coordination runs through one object |
| **Harder to distribute** | If the hub is slow, all devices wait |

---

## Mediator vs Observer

These patterns are often confused. The key distinction:

| | Mediator | Observer |
|---|---------|---------|
| **Direction** | Bidirectional — colleagues both publish and react via the mediator | Unidirectional — subject notifies observers |
| **Who holds logic** | The mediator holds coordination logic | The subject just fires an event; observers decide independently |
| **Coupling** | Colleagues know the mediator; mediator knows all colleagues | Observers don't know each other or the subject's intent |
| **Use when** | Many objects with complex interdependencies | One object should notify many without knowing who they are |

In practice: if the hub decides *"motion detected → turn on lights AND start recording"*, that's Mediator. If the hub just fires `"motion.detected"` and each subscriber independently decides what to do, that's Observer (or Publish-Subscribe).

---

## Related Patterns

- **Observer (3.07)** — similar decoupling goal; Mediator centralises logic, Observer distributes it
- **Facade (2.5)** — also simplifies communication to a subsystem, but is one-directional and doesn't mediate between components
- **Command (3.02)** — commands can be routed through a mediator to implement request queuing
- **Chain of Responsibility (3.01)** — also routes requests, but through a linear chain rather than a central hub

---

## Running the Demo

```bash
cd src/3-Behavioral/3.05-Mediator/MediatorPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.05-Mediator/MediatorPattern.Tests
dotnet test
```
